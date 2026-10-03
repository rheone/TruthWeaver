namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 08: the K3-aware rule equivalence check and the diff's "preserves meaning" verdict.</summary>
public sealed class RuleEquivalenceTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary>De Morgan's law holds in Strong K3, so the two spellings are reported equivalent with no counter-example.</summary>
    [Fact]
    public void Compare_DeMorganPair_ReportsEquivalentWithoutCounterExample_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(
            Compile(compiler, "NOT (a AND b)"),
            Compile(compiler, "NOT a OR NOT b")
        );

        Assert.Equal(RuleEquivalenceOutcome.Equivalent, result.Outcome);
        Assert.Null(result.CounterExample);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// <c>a OR NOT a</c> is <c>Unknown</c> (not <c>True</c>) when <c>a</c> is <c>Unknown</c>, so it is not equivalent to
    /// <c>TRUE</c> in Strong K3 even though it is in two-valued logic; the counter-example is that assignment.
    /// </summary>
    [Fact]
    public void Compare_ExcludedMiddleVersusTrue_ReportsNotEquivalentWithUnknownCounterExample_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(Compile(compiler, "a OR NOT a"), Compile(compiler, "TRUE"));

        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, result.Outcome);
        Assert.NotNull(result.CounterExample);
        Assert.Equal(TruthValue.Unknown, result.CounterExample["a"]);
    }

    /// <summary>A term present in only one rule still appears in the counter-example (the union of both rules' terms).</summary>
    [Fact]
    public void Compare_RulesOverDifferentTerms_CounterExampleCoversTheUnionOfTerms_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(Compile(compiler, "a"), Compile(compiler, "a AND b"));

        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, result.Outcome);
        Assert.NotNull(result.CounterExample);
        Assert.Equal(["a", "b"], result.CounterExample.Keys.Order());
    }

    /// <summary>Term arguments are part of identity, so the counter-example key is the printed term with its arguments.</summary>
    [Fact]
    public void Compare_TermsDifferingOnlyByArgument_AreNotEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(
            Compile(compiler, "hasRole(role: \"Y\")"),
            Compile(compiler, "hasRole(role: \"Z\")")
        );

        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, result.Outcome);
        Assert.NotNull(result.CounterExample);
        Assert.Contains("hasRole(role: \"Y\")", result.CounterExample.Keys);
        Assert.Contains("hasRole(role: \"Z\")", result.CounterExample.Keys);
    }

    /// <summary>Beyond the term cap the check says so with a reason instead of guessing or throwing.</summary>
    [Fact]
    public void Compare_MoreDistinctTermsThanTheCap_ReportsUndecidedWithReason_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(
            Compile(compiler, "a AND b"),
            Compile(compiler, "b AND a"),
            new CompilerOptions(MaxAnalysisTerms: 1)
        );

        Assert.Equal(RuleEquivalenceOutcome.Undecided, result.Outcome);
        Assert.Null(result.CounterExample);
        Assert.Contains("2", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>The cap counts the union of both rules' distinct terms, and a union exactly at the cap is decided.</summary>
    [Fact]
    public void Compare_UnionOfTermsExactlyAtTheCap_IsDecided_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(
            Compile(compiler, "a AND b"),
            Compile(compiler, "b AND a"),
            new CompilerOptions(MaxAnalysisTerms: 2)
        );

        Assert.Equal(RuleEquivalenceOutcome.Equivalent, result.Outcome);
    }

    /// <summary>A null rule is a programming error and is rejected like the other public entry points.</summary>
    [Fact]
    public void Compare_NullRule_ThrowsArgumentNullException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a");

        Assert.Throws<ArgumentNullException>(() => RuleEquivalence.Compare(null!, rule));
        Assert.Throws<ArgumentNullException>(() => RuleEquivalence.Compare(rule, null!));
    }

    /// <summary>
    /// Over many generated rule pairs (a rule against itself, its canonical form, and an unrelated rule) the verdict is
    /// Equivalent exactly when the independent <see cref="K3Oracle"/> agrees on every {True, False, Unknown} assignment
    /// of a, b, c, and every counter-example is a real disagreement under the oracle.
    /// </summary>
    [Fact]
    public void Compare_GeneratedRulePairs_VerdictAndCounterExampleAgreeWithK3Oracle_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261003);
        List<string> failures = [];
        int equivalent = 0;
        int notEquivalent = 0;

        for (int i = 0; i < 300; i++)
        {
            GeneratedRule left = K3RuleGenerator.GenerateRule(random, depth: 3);
            GeneratedRule other = K3RuleGenerator.GenerateRule(random, depth: 3);
            CompiledRule<RuleTestContext>? leftRule = compiler.Compile(left.Text).CompiledRule;
            CompiledRule<RuleTestContext>? otherRule = compiler.Compile(other.Text).CompiledRule;
            if (leftRule is null || otherRule is null)
            {
                // Out-of-range thresholds (BRE0008) are authoring errors, not equivalence input.
                continue;
            }

            (CompiledRule<RuleTestContext> Rule, Func<IReadOnlyList<TruthValue>, TruthValue> Eval)[] candidates =
            [
                (leftRule, left.Eval),
                (leftRule.Canonicalize(), left.Eval),
                (otherRule, other.Eval),
            ];

            foreach ((CompiledRule<RuleTestContext> candidate, Func<IReadOnlyList<TruthValue>, TruthValue> eval) in candidates)
            {
                bool oracleEquivalent = K3Oracle.Assignments(3).All(a => left.Eval(a) == eval(a));
                RuleEquivalenceResult result = RuleEquivalence.Compare(leftRule, candidate);
                string label = $"{left.Text} vs {candidate}";

                if (oracleEquivalent != (result.Outcome == RuleEquivalenceOutcome.Equivalent))
                {
                    failures.Add($"{label}: oracle equivalent={oracleEquivalent}, got {result.Outcome}");
                    continue;
                }

                if (oracleEquivalent)
                {
                    equivalent++;
                    continue;
                }

                notEquivalent++;
                TruthValue[] assignment = [.. TermNames.Select(t => result.CounterExample!.GetValueOrDefault(t))];
                if (left.Eval(assignment) == eval(assignment))
                {
                    failures.Add($"{label}: counter-example [{string.Join(", ", assignment)}] does not distinguish the rules");
                }
            }
        }

        Assert.Empty(failures);

        // Guard against a vacuous pass: both verdicts must occur plenty of times.
        Assert.True(equivalent > 100, $"only {equivalent} equivalent pairs were checked");
        Assert.True(notEquivalent > 100, $"only {notEquivalent} non-equivalent pairs were checked");
    }

    /// <summary>A diff of structurally identical rules preserves meaning.</summary>
    [Fact]
    public void Compare_IdenticalRules_DiffPreservesMeaning_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a AND b"), Compile(compiler, "a AND b"));

        Assert.True(diff.PreservesMeaning);
    }

    /// <summary>A structural change that is a Strong K3 identity (De Morgan) is reported as a change that preserves meaning.</summary>
    [Fact]
    public void Compare_StructuralChangeThatIsAnIdentity_DiffPreservesMeaning_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "NOT (a AND b)"), Compile(compiler, "NOT a OR NOT b"));

        Assert.True(diff.HasChanges);
        Assert.True(diff.PreservesMeaning);
    }

    /// <summary>A structural change that alters the result is reported as one that does not preserve meaning.</summary>
    [Fact]
    public void Compare_StructuralChangeThatAltersTheResult_DiffDoesNotPreserveMeaning_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a AND b"), Compile(compiler, "a OR b"));

        Assert.True(diff.HasChanges);
        Assert.False(diff.PreservesMeaning);
    }

    /// <summary>
    /// When the rules have more distinct terms than the default analysis cap, the diff cannot tell, so it reports no
    /// verdict (<see langword="null"/>) rather than a guess.
    /// </summary>
    [Fact]
    public void Compare_ChangedRulesBeyondTheTermCap_DiffHasNoMeaningVerdict_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        int cap = new CompilerOptions().MaxAnalysisTerms;
        string wide = string.Join(" AND ", Enumerable.Range(0, cap + 1).Select(n => $"hasRole(role: \"R{n}\")"));
        string reversed = string.Join(" AND ", Enumerable.Range(0, cap + 1).Reverse().Select(n => $"hasRole(role: \"R{n}\")"));

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, wide), Compile(compiler, reversed));

        Assert.True(diff.HasChanges);
        Assert.Null(diff.PreservesMeaning);
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
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build(),
            new CompilerOptions(MaxDepth: 256, MaxNodeCount: 4096)
        );
    }
}
