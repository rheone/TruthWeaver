namespace TruthWeaver.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tests.TestSupport;

/// <summary>Feature backlog 13: rule implication and the looser/tighter relation between two rules.</summary>
public sealed class RuleImplicationTests
{
    private static readonly string[] TermNames = ["a", "b", "c"];

    /// <summary><c>a AND b</c> is <c>True</c> only where <c>a</c> is <c>True</c>, so the implication holds with no counter-example.</summary>
    [Fact]
    public void Implies_NarrowerRuleIntoWiderRule_HoldsWithoutCounterExample_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleImplicationResult result = RuleImplication.Implies(Compile(compiler, "a AND b"), Compile(compiler, "a"));

        Assert.Equal(RuleImplicationOutcome.Holds, result.Outcome);
        Assert.Null(result.CounterExample);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// <c>a</c> does not imply <c>a AND b</c>: the counter-example satisfies the first rule, not the second, and names
    /// every term of either rule.
    /// </summary>
    [Fact]
    public void Implies_WiderRuleIntoNarrowerRule_DoesNotHoldWithCounterExampleOverTheUnionOfTerms_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleImplicationResult result = RuleImplication.Implies(Compile(compiler, "a"), Compile(compiler, "a AND b"));

        Assert.Equal(RuleImplicationOutcome.DoesNotHold, result.Outcome);
        Assert.NotNull(result.CounterExample);
        Assert.Equal(["a", "b"], result.CounterExample.Keys.Order());
        Assert.Equal(TruthValue.True, result.CounterExample["a"]);
        Assert.NotEqual(TruthValue.True, result.CounterExample["b"]);
    }

    /// <summary>Beyond the term cap the implication check reports no verdict and says why.</summary>
    [Fact]
    public void Implies_MoreDistinctTermsThanTheCap_ReportsUndecidedWithReason_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleImplicationResult result = RuleImplication.Implies(Compile(compiler, "a AND b"), Compile(compiler, "a"), 1);

        Assert.Equal(RuleImplicationOutcome.Undecided, result.Outcome);
        Assert.Null(result.CounterExample);
        Assert.Contains("2", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>Two rules with the same satisfying contexts are reported equivalent with no examples.</summary>
    [Fact]
    public void Compare_DeMorganPair_ReportsEquivalentWithoutExamples_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(
            Compile(compiler, "NOT (a OR b)"),
            Compile(compiler, "NOT a AND NOT b")
        );

        Assert.Equal(RuleRelation.Equivalent, result.Relation);
        Assert.Null(result.SatisfiesOnlyBefore);
        Assert.Null(result.SatisfiesOnlyAfter);
        Assert.Null(result.Reason);
    }

    /// <summary>
    /// Adding a condition makes the rule tighter: the example is a context the old rule accepted and the new rule does
    /// not.
    /// </summary>
    [Fact]
    public void Compare_AddedConjunct_ReportsTighterWithAContextOnlyTheBeforeRuleAccepts_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(Compile(compiler, "a"), Compile(compiler, "a AND b"));

        Assert.Equal(RuleRelation.Tighter, result.Relation);
        Assert.NotNull(result.SatisfiesOnlyBefore);
        Assert.Equal(TruthValue.True, result.SatisfiesOnlyBefore["a"]);
        Assert.NotEqual(TruthValue.True, result.SatisfiesOnlyBefore["b"]);
        Assert.Null(result.SatisfiesOnlyAfter);
    }

    /// <summary>Adding an alternative makes the rule looser: the example is a context only the new rule accepts.</summary>
    [Fact]
    public void Compare_AddedDisjunct_ReportsLooserWithAContextOnlyTheAfterRuleAccepts_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(Compile(compiler, "a"), Compile(compiler, "a OR b"));

        Assert.Equal(RuleRelation.Looser, result.Relation);
        Assert.Null(result.SatisfiesOnlyBefore);
        Assert.NotNull(result.SatisfiesOnlyAfter);
        Assert.NotEqual(TruthValue.True, result.SatisfiesOnlyAfter["a"]);
        Assert.Equal(TruthValue.True, result.SatisfiesOnlyAfter["b"]);
    }

    /// <summary>Rules that share some contexts and each accept others overlap, and both one-sided examples are given.</summary>
    [Fact]
    public void Compare_RulesThatShareSomeContexts_ReportsOverlappingWithBothExamples_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(Compile(compiler, "a"), Compile(compiler, "b"));

        Assert.Equal(RuleRelation.Overlapping, result.Relation);
        Assert.NotNull(result.SatisfiesOnlyBefore);
        Assert.NotNull(result.SatisfiesOnlyAfter);
    }

    /// <summary>A rule and its negation are never both <c>True</c>, so they are disjoint.</summary>
    [Fact]
    public void Compare_RuleAndItsNegation_ReportsDisjointWithBothExamples_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(Compile(compiler, "a AND b"), Compile(compiler, "NOT (a AND b)"));

        Assert.Equal(RuleRelation.Disjoint, result.Relation);
        Assert.NotNull(result.SatisfiesOnlyBefore);
        Assert.NotNull(result.SatisfiesOnlyAfter);
    }

    /// <summary>
    /// The relation compares only the satisfying contexts: <c>a</c> and <c>IsTrue(a)</c> are <c>True</c> for the same
    /// contexts, so they are equivalent here although they differ in Strong K3 when <c>a</c> is <c>Unknown</c>.
    /// </summary>
    [Fact]
    public void Compare_SameSatisfyingContextsButDifferentUnknownHandling_ReportsEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = Compile(compiler, "a");
        CompiledRule<RuleTestContext> after = Compile(compiler, "IsTrue(a)");

        RuleRelationResult result = RuleImplication.Compare(before, after);

        Assert.Equal(RuleRelation.Equivalent, result.Relation);
        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, RuleEquivalence.Compare(before, after).Outcome);
    }

    /// <summary>Beyond the term cap the relation is <see cref="RuleRelation.Undecided"/> with a reason, not a guess.</summary>
    [Fact]
    public void Compare_MoreDistinctTermsThanTheCap_ReportsUndecidedWithReason_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(Compile(compiler, "a"), Compile(compiler, "a AND b"), 1);

        Assert.Equal(RuleRelation.Undecided, result.Relation);
        Assert.Null(result.SatisfiesOnlyBefore);
        Assert.Null(result.SatisfiesOnlyAfter);
        Assert.Contains("2", result.Reason, StringComparison.Ordinal);
    }

    /// <summary>A null rule on either side is a programming error, and the implication check rejects it.</summary>
    /// <param name="nullFirst">Whether the first rule (otherwise the second) is <see langword="null"/>.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Implies_NullRule_ThrowsArgumentNullException_Test(bool nullFirst)
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a");

        Assert.Throws<ArgumentNullException>(() =>
            nullFirst ? RuleImplication.Implies(null!, rule) : RuleImplication.Implies(rule, null!)
        );
    }

    /// <summary>A null rule on either side is a programming error, and the relation check rejects it.</summary>
    /// <param name="nullBefore">Whether the before rule (otherwise the after rule) is <see langword="null"/>.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compare_NullRule_ThrowsArgumentNullException_Test(bool nullBefore)
    {
        CompiledRule<RuleTestContext> rule = Compile(CreateCompiler(), "a");

        Assert.Throws<ArgumentNullException>(() =>
            nullBefore ? RuleImplication.Compare(null!, rule) : RuleImplication.Compare(rule, null!)
        );
    }

    /// <summary>
    /// Over many generated rule pairs (a rule against itself, a narrowing, a widening, its negation and an unrelated rule)
    /// the relation and the implication verdict are what the independent <see cref="K3Oracle"/> gives over every
    /// {True, False, Unknown} assignment of a, b, c, every example really satisfies one rule and not the other, and all
    /// five relations occur.
    /// </summary>
    [Fact]
    public void Compare_GeneratedRulePairs_RelationAndExamplesAgreeWithK3Oracle_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261011);
        List<string> failures = [];
        Dictionary<RuleRelation, int> seen = [];

        for (int i = 0; i < 300; i++)
        {
            GeneratedRule left = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            GeneratedRule other = K3RuleGenerator.GenerateRule(random, depth: 3, TermNames);
            CompiledRule<RuleTestContext>? leftRule = compiler.Compile(left.Text).CompiledRule;
            if (leftRule is null || compiler.Compile(other.Text).CompiledRule is null)
            {
                // Out-of-range thresholds (TRE0008) are authoring errors, not comparison input.
                continue;
            }

            GeneratedRule[] candidates =
            [
                left,
                other,
                new($"({left.Text}) AND ({other.Text})", v => K3Oracle.And([left.Eval(v), other.Eval(v)]), []),
                new($"({left.Text}) OR ({other.Text})", v => K3Oracle.Or([left.Eval(v), other.Eval(v)]), []),
                new($"NOT ({left.Text})", v => K3Oracle.Not(left.Eval(v)), []),
            ];

            foreach (GeneratedRule candidate in candidates)
            {
                CompiledRule<RuleTestContext> after = Compile(compiler, candidate.Text);
                string label = $"{left.Text} vs {candidate.Text}";
                RuleRelation expected = OracleRelation(left, candidate);
                RuleRelationResult result = RuleImplication.Compare(leftRule, after);
                seen[result.Relation] = seen.GetValueOrDefault(result.Relation) + 1;
                if (result.Relation != expected)
                {
                    failures.Add($"{label}: oracle {expected}, got {result.Relation}");
                    continue;
                }

                CheckExample(result.SatisfiesOnlyBefore, left, candidate, $"{label} (only before)", failures);
                CheckExample(result.SatisfiesOnlyAfter, candidate, left, $"{label} (only after)", failures);

                // "after implies before" holds exactly when no context satisfies only the after rule.
                bool oracleImplies = expected is RuleRelation.Equivalent or RuleRelation.Tighter;
                RuleImplicationResult implication = RuleImplication.Implies(after, leftRule);
                if ((implication.Outcome == RuleImplicationOutcome.Holds) != oracleImplies)
                {
                    failures.Add($"{label}: Implies(after, before) gave {implication.Outcome}, oracle {oracleImplies}");
                }

                CheckExample(implication.CounterExample, candidate, left, $"{label} (implication)", failures);
            }
        }

        Assert.Empty(failures);

        // Guard against a vacuous pass: every relation must occur.
        foreach (RuleRelation relation in Enum.GetValues<RuleRelation>().Where(r => r != RuleRelation.Undecided))
        {
            Assert.True(
                seen.GetValueOrDefault(relation) > 10,
                $"only {seen.GetValueOrDefault(relation)} {relation} pairs were checked"
            );
        }
    }

    /// <summary>A diff of structurally identical rules reports them equivalent.</summary>
    [Fact]
    public void RuleDiffCompare_IdenticalRules_ReportsEquivalentRelation_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a AND b"), Compile(compiler, "a AND b"));

        Assert.Equal(RuleRelation.Equivalent, diff.Relation);
    }

    /// <summary>A diff that adds a condition reports the new rule as tighter than the old one.</summary>
    [Fact]
    public void RuleDiffCompare_AddedConjunct_ReportsTighterRelation_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a"), Compile(compiler, "a AND b"));

        Assert.Equal(RuleRelation.Tighter, diff.Relation);
    }

    /// <summary>A diff of rules with more distinct terms than the term cap reports the relation as undecided.</summary>
    [Fact]
    public void RuleDiffCompare_RulesBeyondTheCap_ReportsUndecidedRelation_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a"), Compile(compiler, "a OR b"), 1);

        Assert.Equal(RuleRelation.Undecided, diff.Relation);
    }

    /// <summary>The term cap argument of a diff also applies to the relation, so a raised cap decides it.</summary>
    [Fact]
    public void RuleDiffCompare_CapRaisedToFitTheTerms_ReportsDecidedRelation_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleDiffResult diff = RuleDiff.Compare(Compile(compiler, "a"), Compile(compiler, "a OR b"), 2);

        Assert.Equal(RuleRelation.Looser, diff.Relation);
    }

    private static RuleRelation OracleRelation(GeneratedRule before, GeneratedRule after)
    {
        bool onlyBefore = false;
        bool onlyAfter = false;
        bool both = false;
        foreach (TruthValue[] assignment in K3Oracle.Assignments(TermNames.Length))
        {
            bool b = before.Eval(assignment) == TruthValue.True;
            bool a = after.Eval(assignment) == TruthValue.True;
            onlyBefore |= b && !a;
            onlyAfter |= a && !b;
            both |= a && b;
        }

        return (onlyBefore, onlyAfter, both) switch
        {
            (false, false, _) => RuleRelation.Equivalent,
            (true, false, _) => RuleRelation.Tighter,
            (false, true, _) => RuleRelation.Looser,
            (true, true, true) => RuleRelation.Overlapping,
            (true, true, false) => RuleRelation.Disjoint,
        };
    }

    private static void CheckExample(
        IReadOnlyDictionary<string, TruthValue>? example,
        GeneratedRule satisfied,
        GeneratedRule notSatisfied,
        string label,
        List<string> failures
    )
    {
        if (example is null)
        {
            return;
        }

        // The example must name every term of both rules; a missing key would otherwise read as False.
        string[] missing =
        [
            .. TermNames.Where(t =>
                !example.ContainsKey(t)
                && Regex.IsMatch(
                    $"{satisfied.Text} {notSatisfied.Text}",
                    $@"\b{t}\b",
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1)
                )
            ),
        ];
        if (missing.Length > 0)
        {
            failures.Add($"{label}: example does not name {string.Join(", ", missing)}");
            return;
        }

        TruthValue[] assignment = [.. TermNames.Select(t => example.GetValueOrDefault(t))];
        if (satisfied.Eval(assignment) != TruthValue.True || notSatisfied.Eval(assignment) == TruthValue.True)
        {
            failures.Add($"{label}: example [{string.Join(", ", assignment)}] is not a one-sided context");
        }
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
