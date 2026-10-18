namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Feature backlog 21c (ADR-0009 decision 5): redundancy and the pairwise sweep over a set of peer rules.</summary>
public sealed class RuleSetAnalysisTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary>
    /// In <c>[a, a AND b, c, c]</c> the last three rules are each redundant given the others, but the two <c>c</c> rules
    /// cover each other, so the greedy group keeps one of them (design note section 4).
    /// </summary>
    [Fact]
    public void Analyze_RulesThatCoverEachOther_ReportsEachRedundantRuleAndAGroupThatKeepsOneCopy_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext>[] rules = Compile(compiler, "a", "a AND b", "c", "c");

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(rules);

        Assert.Equal(RuleSetAnalysisOutcome.Decided, result.Outcome);
        Assert.Null(result.Reason);
        Assert.Equal([false, true, true, true], result.Rules.Select(rule => rule.IsRedundant));
        Assert.Equal([0, 1, 2, 3], result.Rules.Select(rule => rule.Index));
        Assert.Equal([1, 3], result.RemovableTogether);
    }

    /// <summary>A rule that is not redundant carries an example where it alone is <c>True</c>; a redundant rule carries none.</summary>
    [Fact]
    public void Analyze_RuleNotCoveredByTheOthers_ReportsAnExampleWhereOnlyItIsTrue_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(Compile(compiler, "a", "b", "a AND b"));

        RuleRedundancy first = result.Rules[0];
        Assert.False(first.IsRedundant);
        Assert.NotNull(first.Example);
        Assert.Equal(TruthValue.True, first.Example["a"]);
        Assert.NotEqual(TruthValue.True, first.Example["b"]);
        Assert.True(result.Rules[2].IsRedundant);
        Assert.Null(result.Rules[2].Example);
    }

    /// <summary>
    /// The sweep has one entry per unordered pair, in index order, and each entry has the same reachable pairs as
    /// <see cref="RuleOverlap.Compare{TContext}"/> for those two rules, with examples that name only their terms.
    /// </summary>
    [Fact]
    public void Analyze_ThreeRules_SweepMatchesPairwiseCompareForEveryPair_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext>[] rules = Compile(compiler, "a", "NOT a", "b AND c");

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(rules);

        Assert.Equal([(0, 1), (0, 2), (1, 2)], result.Pairs.Select(pair => (pair.First, pair.Second)));
        foreach (RuleSetPair pair in result.Pairs)
        {
            RuleOverlapResult expected = RuleOverlap.Compare(rules[pair.First], rules[pair.Second]);
            Assert.Equal(RuleOverlapOutcome.Decided, pair.Overlap.Outcome);
            Assert.Equal(
                expected.Reachable.Select(o => (o.First, o.Second)),
                pair.Overlap.Reachable.Select(o => (o.First, o.Second))
            );
        }

        RuleSetPair negation = Assert.IsType<RuleSetPair>(result.FindPair(1, 0));
        Assert.False(negation.Overlap.CanOverlap);
        Assert.All(negation.Overlap.Reachable, outcome => Assert.Equal(["a"], outcome.Example.Keys));
        Assert.Null(result.FindPair(1, 1));
    }

    /// <summary>
    /// Over generated sets of four rules (connectives and external operators), the redundancy of each rule, its
    /// example and the removable group agree with a brute force over every <c>{True, False, Unknown}</c> assignment of
    /// a, b, c by the independent <see cref="K3Oracle"/>.
    /// </summary>
    [Fact]
    public void Analyze_GeneratedRuleSets_RedundancyAndRemovableGroupAgreeWithK3Oracle_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261018);
        List<string> failures = [];
        int redundantSeen = 0;
        int setsChecked = 0;

        for (int i = 0; i < 200; i++)
        {
            GeneratedRule[] generated =
            [
                .. Enumerable.Range(0, 4).Select(_ => K3RuleGenerator.GenerateRule(random, depth: 2, TermNames)),
            ];
            CompiledRule<RuleTestContext>?[] compiled = [.. generated.Select(rule => compiler.Compile(rule.Text).CompiledRule)];
            if (compiled.Any(rule => rule is null))
            {
                // Out-of-range thresholds (TRE0008) are authoring errors, not analysis input.
                continue;
            }

            setsChecked++;
            string label = string.Join(" | ", generated.Select(rule => rule.Text));
            TruthValue[][] assignments = [.. K3Oracle.Assignments(TermNames.Length)];
            RuleSetAnalysisResult result = RuleSetAnalysis.Analyze([.. compiled.Select(rule => rule!)]);

            for (int r = 0; r < generated.Length; r++)
            {
                bool expectedRedundant = assignments.All(assignment =>
                    generated[r].Eval(assignment) != TruthValue.True
                    || generated.Where((_, other) => other != r).Any(other => other.Eval(assignment) == TruthValue.True)
                );
                RuleRedundancy reported = result.Rules[r];
                redundantSeen += reported.IsRedundant ? 1 : 0;
                if (reported.IsRedundant != expectedRedundant)
                {
                    failures.Add($"{label}: rule {r} redundant {reported.IsRedundant}, oracle {expectedRedundant}");
                }
                else if (reported.Example is not null)
                {
                    TruthValue[] example = [.. TermNames.Select(t => reported.Example.GetValueOrDefault(t))];
                    bool onlyThis =
                        generated[r].Eval(example) == TruthValue.True
                        && generated.Where((_, other) => other != r).All(other => other.Eval(example) != TruthValue.True);
                    if (!onlyThis)
                    {
                        failures.Add($"{label}: the example of rule {r} does not make it the only True rule");
                    }
                }
            }

            GeneratedRule[] kept = [.. generated.Where((_, index) => !result.RemovableTogether.Contains(index))];
            if (
                assignments.Any(a =>
                    generated.Any(g => g.Eval(a) == TruthValue.True) != kept.Any(g => g.Eval(a) == TruthValue.True)
                )
            )
            {
                failures.Add($"{label}: removing [{string.Join(", ", result.RemovableTogether)}] changes the satisfying set");
            }
        }

        Assert.Empty(failures);

        // Guard against a vacuous pass: enough sets ran, and both answers occurred.
        Assert.True(setsChecked > 100, $"only {setsChecked} sets were checked");
        Assert.InRange(redundantSeen, 1, (setsChecked * 4) - 1);
    }

    /// <summary>A set with more rules than the rule cap computes nothing and reports why, without throwing.</summary>
    [Fact]
    public void Analyze_MoreRulesThanTheCap_ReportsTooManyRulesWithEmptyLists_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(Compile(compiler, "a", "b", "c"), maxRules: 2);

        Assert.Equal(RuleSetAnalysisOutcome.TooManyRules, result.Outcome);
        Assert.Empty(result.Pairs);
        Assert.Empty(result.Rules);
        Assert.Empty(result.RemovableTogether);
        Assert.Contains("3 rules", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>The default rule cap is 50: a set of 51 rules is not analyzed.</summary>
    [Fact]
    public void Analyze_FiftyOneRulesWithTheDefaultCap_ReportsTooManyRules_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a")[0];

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(Enumerable.Repeat(rule, 51).ToArray());

        Assert.Equal(50, RuleSetAnalysis.DefaultMaxRules);
        Assert.Equal(RuleSetAnalysisOutcome.TooManyRules, result.Outcome);
    }

    /// <summary>
    /// When the set has more distinct terms than the cap, the set-level answers are undecided, but each pair that
    /// fits the cap on its own is still decided.
    /// </summary>
    [Fact]
    public void Analyze_SetOverTheTermCapButPairsUnderIt_ReportsUndecidedWithDecidedPairs_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(Compile(compiler, "a", "b", "c"), maxAnalysisTerms: 2);

        Assert.Equal(RuleSetAnalysisOutcome.Undecided, result.Outcome);
        Assert.Contains("3 distinct terms", result.Reason, StringComparison.Ordinal);
        Assert.Empty(result.Rules);
        Assert.Empty(result.RemovableTogether);
        Assert.Equal(3, result.Pairs.Count);
        Assert.All(result.Pairs, pair => Assert.Equal(RuleOverlapOutcome.Decided, pair.Overlap.Outcome));
    }

    /// <summary>
    /// When the shared BDD of the set would exceed the node budget, the set-level answers are undecided with a reason
    /// and no exception escapes; the pairs are still computed one at a time under their own budget.
    /// </summary>
    [Fact]
    public void AnalyzeCore_SharedBddOverTheNodeBudget_ReportsUndecidedWithDecidedPairs_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleSetAnalysisResult result = RuleSetAnalysis.AnalyzeCore(
            Compile(compiler, "a AND b", "b OR c"),
            maxAnalysisTerms: null,
            maxRules: null,
            maxBddNodes: 2
        );

        Assert.Equal(RuleSetAnalysisOutcome.Undecided, result.Outcome);
        Assert.Contains("budget of 2", result.Reason, StringComparison.Ordinal);
        Assert.Empty(result.Rules);
        RuleSetPair pair = Assert.Single(result.Pairs);
        Assert.Equal(RuleOverlapOutcome.Decided, pair.Overlap.Outcome);
    }

    /// <summary>An empty set is decided and has nothing to report.</summary>
    [Fact]
    public void Analyze_EmptySet_ReportsDecidedWithEmptyLists_Test()
    {
        RuleSetAnalysisResult result = RuleSetAnalysis.Analyze(Array.Empty<CompiledRule<RuleTestContext>>());

        Assert.Equal(RuleSetAnalysisOutcome.Decided, result.Outcome);
        Assert.Empty(result.Pairs);
        Assert.Empty(result.Rules);
        Assert.Empty(result.RemovableTogether);
    }

    /// <summary>The <see cref="RuleSet{TContext}"/> overload gives the same answers as the list overload.</summary>
    [Fact]
    public void Analyze_RuleSetOverload_GivesTheSameAnswersAsTheListOverload_Test()
    {
        CompiledRule<RuleTestContext>[] rules = Compile(CreateCompiler(), "a", "a AND b", "c", "c");

        RuleSetAnalysisResult fromSet = RuleSetAnalysis.Analyze(new RuleSet<RuleTestContext>(rules));
        RuleSetAnalysisResult fromList = RuleSetAnalysis.Analyze(rules);

        Assert.Equal(fromList.Outcome, fromSet.Outcome);
        Assert.Equal(fromList.RemovableTogether, fromSet.RemovableTogether);
        Assert.Equal(fromList.Rules.Select(r => r.IsRedundant), fromSet.Rules.Select(r => r.IsRedundant));
        Assert.Equal(fromList.Pairs.Count, fromSet.Pairs.Count);
    }

    /// <summary>A null list, a null rule in the list, or a rule cap below 1 is a programming error, and the analysis rejects it.</summary>
    [Fact]
    public void Analyze_InvalidArguments_Throw_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a")[0];

        Assert.Throws<ArgumentNullException>(() =>
            RuleSetAnalysis.Analyze((IReadOnlyList<CompiledRule<RuleTestContext>>)null!)
        );
        Assert.Throws<ArgumentNullException>(() => RuleSetAnalysis.Analyze([rule, null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleSetAnalysis.Analyze([rule], maxRules: 0));
    }

    /// <summary>A rule set keeps its own copy of the rules, in order, so a later change to the source list does not change it.</summary>
    [Fact]
    public void RuleSet_SourceListChangedAfterConstruction_KeepsTheOriginalRulesInOrder_Test()
    {
        CompiledRule<RuleTestContext>[] rules = Compile(CreateCompiler(), "a", "b");
        List<CompiledRule<RuleTestContext>> source = [.. rules];

        RuleSet<RuleTestContext> set = new(source);
        source.Clear();

        Assert.Equal(2, set.Count);
        Assert.Same(rules[0], set[0]);
        Assert.Equal(rules, set);
    }

    /// <summary>A rule set rejects a null source and a null rule.</summary>
    [Fact]
    public void RuleSet_NullSourceOrNullRule_ThrowsArgumentNullException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a")[0];

        Assert.Throws<ArgumentNullException>(() => new RuleSet<RuleTestContext>(null!));
        Assert.Throws<ArgumentNullException>(() => new RuleSet<RuleTestContext>([rule, null!]));
    }

    private static CompiledRule<RuleTestContext>[] Compile(RuleCompiler<RuleTestContext> compiler, params string[] texts)
    {
        return
        [
            .. texts.Select(text =>
                compiler.Compile(text).CompiledRule ?? throw new InvalidOperationException($"'{text}' did not compile.")
            ),
        ];
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        );
    }
}
