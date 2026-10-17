namespace TruthWeaver.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Feature backlog 21b-2 (ADR-0009): the pairwise joint-outcome check between two peer rules.</summary>
public sealed class RuleOverlapTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary>A rule and its negation never both hold, but both one-sided conflicts and the shared-Unknown pair are reachable.</summary>
    [Fact]
    public void Compare_RuleAndItsNegation_NeverOverlapsButBothConflictPairsAreReachable_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleOverlapResult result = RuleOverlap.Compare(Compile(compiler, "a"), Compile(compiler, "NOT a"));

        Assert.Equal(RuleOverlapOutcome.Decided, result.Outcome);
        Assert.False(result.CanOverlap);
        Assert.True(result.CanConflict);
        Assert.Null(result.Find(TruthValue.True, TruthValue.True));

        JointOutcome? trueFalse = result.Find(TruthValue.True, TruthValue.False);
        Assert.NotNull(trueFalse);
        Assert.Equal(TruthValue.True, trueFalse.Example["a"]);

        JointOutcome? falseTrue = result.Find(TruthValue.False, TruthValue.True);
        Assert.NotNull(falseTrue);
        Assert.Equal(TruthValue.False, falseTrue.Example["a"]);

        // Both sides depend only on "a", so the Unknown/Unknown pair has no alternative witness without an Unknown term.
        JointOutcome? unknownUnknown = result.Find(TruthValue.Unknown, TruthValue.Unknown);
        Assert.NotNull(unknownUnknown);
        Assert.Equal(TruthValue.Unknown, unknownUnknown.Example["a"]);
    }

    /// <summary>A rule compared with itself never disagrees: only the three diagonal pairs are reachable.</summary>
    [Fact]
    public void Compare_RuleAgainstItself_OverlapsAndNeverConflicts_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = Compile(compiler, "a AND b");

        RuleOverlapResult result = RuleOverlap.Compare(rule, rule);

        Assert.True(result.CanOverlap);
        Assert.False(result.CanConflict);
        Assert.Equal(3, result.Reachable.Count);
        Assert.All(result.Reachable, outcome => Assert.Equal(outcome.First, outcome.Second));
    }

    /// <summary>Two rules over independent terms overlap and conflict, with definite (no-<c>Unknown</c>) examples for the definite pairs.</summary>
    [Fact]
    public void Compare_RulesOverIndependentTerms_ReportsDefiniteExamplesWithNoUnknownTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleOverlapResult result = RuleOverlap.Compare(Compile(compiler, "a"), Compile(compiler, "b"));

        Assert.True(result.CanOverlap);
        Assert.True(result.CanConflict);
        foreach (
            JointOutcome definite in result.Reachable.Where(o =>
                o.First != TruthValue.Unknown && o.Second != TruthValue.Unknown
            )
        )
        {
            Assert.DoesNotContain(TruthValue.Unknown, definite.Example.Values);
        }
    }

    /// <summary>
    /// <c>IsUnknown(a)</c> can be <c>True</c> only when <c>a</c> is <c>Unknown</c>, so the <c>(True, True)</c> pair with
    /// <c>b</c> genuinely needs an <c>Unknown</c> term: no definite witness exists for it (ADR-0009 decision 4, design
    /// note section 2).
    /// </summary>
    [Fact]
    public void Compare_ExternalOperatorPair_ReportsUnknownTermWhenNoDefiniteWitnessExists_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleOverlapResult result = RuleOverlap.Compare(Compile(compiler, "b"), Compile(compiler, "IsUnknown(a)"));

        JointOutcome? bothTrue = result.Find(TruthValue.True, TruthValue.True);
        Assert.NotNull(bothTrue);
        Assert.Equal(TruthValue.True, bothTrue.Example["b"]);
        Assert.Equal(TruthValue.Unknown, bothTrue.Example["a"]);
    }

    /// <summary>Beyond the term cap the check reports no verdict, a reason, an empty table, and fail-closed convenience members.</summary>
    [Fact]
    public void Compare_MoreDistinctTermsThanTheCap_ReportsUndecidedWithFailClosedMembers_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleOverlapResult result = RuleOverlap.Compare(Compile(compiler, "a"), Compile(compiler, "b"), 1);

        Assert.Equal(RuleOverlapOutcome.Undecided, result.Outcome);
        Assert.Empty(result.Reachable);
        Assert.Contains("2", result.Reason, StringComparison.Ordinal);
        Assert.False(result.CanOverlap);
        Assert.False(result.CanConflict);
        Assert.Null(result.Find(TruthValue.True, TruthValue.True));
    }

    /// <summary>A null rule on either side is a programming error, and the check rejects it.</summary>
    /// <param name="nullFirst">Whether the first rule (otherwise the second) is <see langword="null"/>.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compare_NullRule_ThrowsArgumentNullException_Test(bool nullFirst)
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a");

        Assert.Throws<ArgumentNullException>(() =>
            nullFirst ? RuleOverlap.Compare(null!, rule) : RuleOverlap.Compare(rule, null!)
        );
    }

    /// <summary>
    /// Over many generated rule pairs (connectives and external operators) the reachable set of
    /// <see cref="RuleOverlap.Compare{TContext}"/> equals the brute-force set the independent <see cref="K3Oracle"/>
    /// produces over every <c>{True, False, Unknown}</c> assignment of a, b, c, and every reported example really
    /// produces its own pair.
    /// </summary>
    [Fact]
    public void Compare_GeneratedRulePairs_ReachableSetAndExamplesAgreeWithK3Oracle_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261016);
        List<string> failures = [];
        int totalReachablePairs = 0;
        Dictionary<(TruthValue, TruthValue), int> seen = [];

        for (int i = 0; i < 300; i++)
        {
            GeneratedRule left = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            GeneratedRule right = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            CompiledRule<RuleTestContext>? leftRule = compiler.Compile(left.Text).CompiledRule;
            CompiledRule<RuleTestContext>? rightRule = compiler.Compile(right.Text).CompiledRule;
            if (leftRule is null || rightRule is null)
            {
                // Out-of-range thresholds (TRE0008) are authoring errors, not comparison input.
                continue;
            }

            string label = $"{left.Text} vs {right.Text}";
            HashSet<(TruthValue First, TruthValue Second)> oracleReachable = [];
            foreach (TruthValue[] assignment in K3Oracle.Assignments(TermNames.Length))
            {
                oracleReachable.Add((left.Eval(assignment), right.Eval(assignment)));
            }

            RuleOverlapResult result = RuleOverlap.Compare(leftRule, rightRule);
            HashSet<(TruthValue First, TruthValue Second)> reported = [.. result.Reachable.Select(o => (o.First, o.Second))];
            if (!reported.SetEquals(oracleReachable))
            {
                failures.Add($"{label}: oracle {Describe(oracleReachable)}, got {Describe(reported)}");
                continue;
            }

            foreach (JointOutcome outcome in result.Reachable)
            {
                seen[(outcome.First, outcome.Second)] = seen.GetValueOrDefault((outcome.First, outcome.Second)) + 1;
                totalReachablePairs++;

                TruthValue[] assignment = [.. TermNames.Select(t => outcome.Example.GetValueOrDefault(t))];
                TruthValue actualFirst = left.Eval(assignment);
                TruthValue actualSecond = right.Eval(assignment);
                if (actualFirst != outcome.First || actualSecond != outcome.Second)
                {
                    failures.Add(
                        $"{label}: example [{string.Join(", ", assignment)}] gave ({actualFirst}, {actualSecond}), expected ({outcome.First}, {outcome.Second})"
                    );
                }
            }
        }

        Assert.Empty(failures);

        // Guard against a vacuous pass: every one of the nine cells must occur somewhere across the generated pairs.
        Assert.True(totalReachablePairs > 100, $"only {totalReachablePairs} reachable pairs were checked in total");
        foreach (TruthValue first in new[] { TruthValue.True, TruthValue.False, TruthValue.Unknown })
        {
            foreach (TruthValue second in new[] { TruthValue.True, TruthValue.False, TruthValue.Unknown })
            {
                Assert.True(seen.GetValueOrDefault((first, second)) > 0, $"the pair ({first}, {second}) never occurred");
            }
        }
    }

    /// <summary>
    /// For rules built only from connectives (no <c>COALESCE</c> or inspection), every definite-pair example (neither
    /// side <c>Unknown</c>) names no <c>Unknown</c> term: connectives are monotone in the information order (ADR-0005
    /// decision 18), so a definite result never genuinely needs an <c>Unknown</c> term (ADR-0009 decision 4, design
    /// note section 2).
    /// </summary>
    [Fact]
    public void Compare_ConnectiveOnlyGeneratedRulePairs_DefinitePairExamplesHaveNoUnknownTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261016);
        List<string> failures = [];
        int definitePairsChecked = 0;

        for (int i = 0; i < 400 && definitePairsChecked < 200; i++)
        {
            GeneratedRule left = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            GeneratedRule right = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            if (!IsConnectiveOnly(left.Text) || !IsConnectiveOnly(right.Text))
            {
                continue;
            }

            CompiledRule<RuleTestContext>? leftRule = compiler.Compile(left.Text).CompiledRule;
            CompiledRule<RuleTestContext>? rightRule = compiler.Compile(right.Text).CompiledRule;
            if (leftRule is null || rightRule is null)
            {
                continue;
            }

            RuleOverlapResult result = RuleOverlap.Compare(leftRule, rightRule);
            foreach (
                JointOutcome outcome in result.Reachable.Where(o =>
                    o.First != TruthValue.Unknown && o.Second != TruthValue.Unknown
                )
            )
            {
                definitePairsChecked++;
                if (outcome.Example.Values.Contains(TruthValue.Unknown))
                {
                    failures.Add(
                        $"{left.Text} vs {right.Text}: definite pair ({outcome.First}, {outcome.Second}) needed an Unknown term"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(definitePairsChecked > 50, $"only {definitePairsChecked} definite pairs were checked");
    }

    /// <summary>Whether <paramref name="ruleText"/> uses only Strong K3 connectives (no <c>COALESCE</c> or inspection).</summary>
    private static bool IsConnectiveOnly(string ruleText)
    {
        return !ruleText.Contains("COALESCE", StringComparison.Ordinal)
            && !Regex.IsMatch(ruleText, @"Is(True|False|Unknown|Known)\(", RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    private static string Describe(IEnumerable<(TruthValue First, TruthValue Second)> pairs)
    {
        return string.Join(", ", pairs.Select(p => $"({p.First}, {p.Second})"));
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string text)
    {
        return compiler.Compile(text).CompiledRule ?? throw new InvalidOperationException($"'{text}' did not compile.");
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
