namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Feature backlog 22b-5 (ADR-0010 decision 8): a <c>FORALL</c>/<c>EXISTS</c> quantifier as a single opaque atom in
/// the constant/contradiction analyzer, <see cref="RuleEquivalence"/> and <see cref="RuleImplication"/>; <c>ToNnf</c>'s
/// De Morgan dual over the quantifier kind; every other rewrite leaving the quantifier node itself untouched; and
/// <see cref="RuleCoverage"/> folding a quantifier's variable-size, one-child-per-element trace onto its one
/// <see cref="NodeCoverage"/> across a corpus whose decisions range over different element counts.
/// </summary>
public sealed class QuantifierAnalysisRewritesCoverageTests
{
    /// <summary>
    /// A rule that narrows a quantifier with <c>AND</c> implies the quantifier alone, exactly as any other
    /// <c>x AND y IMPLIES x</c> narrowing does: the quantifier is one opaque term of the implication check, not a
    /// construct it looks inside of.
    /// </summary>
    [Fact]
    public void Implies_QuantifierNarrowedByAnd_HoldsAsAnOpaqueTerm_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleImplicationResult result = RuleImplication.Implies(
            Compile(compiler, "FORALL(x IN items, elementTrue) AND a"),
            Compile(compiler, "FORALL(x IN items, elementTrue)")
        );

        Assert.Equal(RuleImplicationOutcome.Holds, result.Outcome);
        Assert.Null(result.CounterExample);
    }

    /// <summary>Adding a quantifier as an alternative with <c>OR</c> makes the rule looser, the quantifier again standing as one opaque term.</summary>
    [Fact]
    public void Compare_QuantifierAddedAsDisjunct_ReportsLooser_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleRelationResult result = RuleImplication.Compare(
            Compile(compiler, "FORALL(x IN items, elementTrue)"),
            Compile(compiler, "FORALL(x IN items, elementTrue) OR a")
        );

        Assert.Equal(RuleRelation.Looser, result.Relation);
    }

    /// <summary>A quantifier against itself, written identically, is equivalent: the same canonical text is the same opaque atom.</summary>
    [Fact]
    public void Compare_IdenticalQuantifierText_ReportsEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = Compile(compiler, "FORALL(x IN items, elementTrue)");

        RuleEquivalenceResult result = RuleEquivalence.Compare(rule, Compile(compiler, "FORALL(x IN items, elementTrue)"));

        Assert.Equal(RuleEquivalenceOutcome.Equivalent, result.Outcome);
    }

    /// <summary>
    /// Two quantifiers whose bodies are semantically equal but differently written (<c>elementTrue</c> versus a
    /// double negation of it) are two distinct opaque atoms, not recognized as the same term: the check is sound
    /// but intentionally incomplete across such rewordings (ADR-0010 decision 8).
    /// </summary>
    [Fact]
    public void Compare_QuantifiersWithEquivalentButDifferentlyWrittenBodies_ReportsNotEquivalent_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        RuleEquivalenceResult result = RuleEquivalence.Compare(
            Compile(compiler, "FORALL(x IN items, elementTrue)"),
            Compile(compiler, "FORALL(x IN items, NOT NOT elementTrue)")
        );

        Assert.Equal(RuleEquivalenceOutcome.NotEquivalent, result.Outcome);
        Assert.NotNull(result.CounterExample);
        Assert.Equal(2, result.CounterExample.Count);
    }

    /// <summary><c>ToNnf</c> pushes a negation through <c>FORALL</c> to its De Morgan dual, <c>EXISTS</c>, negating the body.</summary>
    [Fact]
    public void ToNnf_NegatedForAll_BecomesExistsWithNegatedBody_Test()
    {
        Assert.Equal("EXISTS(x IN items, NOT elementTrue)", Nnf("NOT FORALL(x IN items, elementTrue)").CanonicalText);
    }

    /// <summary><c>ToNnf</c> pushes a negation through <c>EXISTS</c> to its dual, <c>FORALL</c>, negating the body.</summary>
    [Fact]
    public void ToNnf_NegatedExists_BecomesForAllWithNegatedBody_Test()
    {
        Assert.Equal("FORALL(x IN items, NOT elementTrue)", Nnf("NOT EXISTS(x IN items, elementTrue)").CanonicalText);
    }

    /// <summary>
    /// Negating a quantifier whose own body is already negated flips the kind and removes the double negation inside
    /// the body in the same step, exactly as a double negation collapses outside a quantifier.
    /// </summary>
    [Fact]
    public void ToNnf_NegatedForAllWithNegatedBody_BecomesExistsWithDoubleNegationRemoved_Test()
    {
        Assert.Equal("EXISTS(x IN items, elementTrue)", Nnf("NOT FORALL(x IN items, NOT elementTrue)").CanonicalText);
    }

    /// <summary>De Morgan over an outer <c>AND</c> reaches a quantifier operand the same way it reaches any other operand, pushing the negation onto its dual.</summary>
    [Fact]
    public void ToNnf_NegatedAndWithQuantifierOperand_PushesNotThroughBothOperands_Test()
    {
        Assert.Equal(
            "NOT a OR EXISTS(x IN items, NOT elementTrue)",
            Nnf("NOT (a AND FORALL(x IN items, elementTrue))").CanonicalText
        );
    }

    /// <summary><c>Simplify</c> rewrites a quantifier's body in its own scope but leaves the quantifier node itself exactly where it is.</summary>
    [Fact]
    public void Simplify_QuantifierBodyWithIdentityConstant_SimplifiesTheBodyAndKeepsTheQuantifier_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = Compile(compiler, "FORALL(x IN items, elementTrue AND True)");

        CompilationResult<RuleTestContext> result = rule.Simplify();

        Assert.Equal("FORALL(x IN items, elementTrue)", result.CompiledRule!.CanonicalText);
    }

    /// <summary><c>ExpandToPrimitives</c> expands a derived operator above a quantifier but does not open up the quantifier itself.</summary>
    [Fact]
    public void ExpandToPrimitives_QuantifierUnderADerivedOperator_LeavesTheQuantifierIntact_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = Compile(compiler, "a XOR FORALL(x IN items, elementTrue)");

        CompilationResult<RuleTestContext> result = rule.ExpandToPrimitives();

        string text = result.CompiledRule!.CanonicalText;
        Assert.DoesNotContain("XOR", text, StringComparison.Ordinal);
        Assert.Contains("FORALL(x IN items, elementTrue)", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>ToCnf</c> distributes <c>OR</c> over <c>AND</c> treating the quantifier as a single, unsplit literal: it
    /// appears whole in every clause it reaches, and the rewritten rule stays equivalent to the original.
    /// </summary>
    [Fact]
    public void ToCnf_QuantifierDisjoinedWithAConjunction_KeepsTheQuantifierWholeInEachClause_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = Compile(compiler, "FORALL(x IN items, elementTrue) OR (a AND b)");

        CompilationResult<RuleTestContext> result = rule.ToCnf();

        CompiledRule<RuleTestContext> cnf = result.CompiledRule!;
        Assert.Equal(2, CountOccurrences(cnf.CanonicalText, "FORALL(x IN items, elementTrue)"));
        Assert.Equal(RuleEquivalenceOutcome.Equivalent, RuleEquivalence.Compare(rule, cnf).Outcome);
    }

    /// <summary>
    /// Coverage over a corpus whose decisions range over element counts of 0, 1, 2 and 3 — including a decision whose
    /// quantifier short-circuits with a trailing, not-evaluated element — does not throw despite every decision's
    /// trace carrying a different number of per-element children for the same compiled rule shape, and folds each
    /// element's contribution onto the quantifier's own term and its body's one <see cref="NodeCoverage"/>.
    /// </summary>
    [Fact]
    public async Task FromDecisions_QuantifierCorpusWithVaryingElementCounts_FoldsPerElementCoverageWithoutThrowing_Test()
    {
        RuleCompiler<QuantifierCorpusContext> compiler = CreateCorpusCompiler();
        CompilationResult<QuantifierCorpusContext> compiled = compiler.Compile("FORALL(x IN items, elementTrue)");
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
        CompiledRule<QuantifierCorpusContext> rule = compiled.CompiledRule!;

        Decision[] decisions = await EvaluateManyAsync(
            rule,
            new QuantifierCorpusContext { Items = [] },
            new QuantifierCorpusContext { Items = [TruthValue.True] },
            new QuantifierCorpusContext { Items = [TruthValue.True, TruthValue.False, TruthValue.True] },
            new QuantifierCorpusContext { Items = [TruthValue.True, TruthValue.Unknown] }
        );

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, decisions);

        Assert.Equal(4, coverage.DecisionCount);
        Assert.Equal((2, 1, 1), (coverage.TrueCount, coverage.FalseCount, coverage.UnknownCount));

        TermCoverage quantifierTerm = Assert.Single(coverage.Terms);
        Assert.Equal("FORALL(x IN items, elementTrue)", quantifierTerm.Term);
        (int EvaluatedCount, int TrueCount, int FalseCount, int UnknownCount, int FaultCount, int DecidedCount) actualTerm = (
            quantifierTerm.EvaluatedCount,
            quantifierTerm.TrueCount,
            quantifierTerm.FalseCount,
            quantifierTerm.UnknownCount,
            quantifierTerm.FaultCount,
            quantifierTerm.DecidedCount
        );
        Assert.Equal(
            (EvaluatedCount: 4, TrueCount: 2, FalseCount: 1, UnknownCount: 1, FaultCount: 0, DecidedCount: 4),
            actualTerm
        );

        NodeCoverage body = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[0]");
        Assert.Equal(
            (EvaluatedCount: 5, NotEvaluatedCount: 1, TrueCount: 3, FalseCount: 1, UnknownCount: 1),
            (body.EvaluatedCount, body.NotEvaluatedCount, body.TrueCount, body.FalseCount, body.UnknownCount)
        );
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static CompiledRule<RuleTestContext> Nnf(string text)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        return Compile(compiler, text).ToNnf().CompiledRule!;
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string text)
    {
        CompilationResult<RuleTestContext> result = compiler.Compile(text);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddCollection(
                "items",
                _ => Array.Empty<TruthValue>(),
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<RuleTestContext>(builder.Build());
    }

    private static RuleCompiler<QuantifierCorpusContext> CreateCorpusCompiler()
    {
        PredicateRegistryBuilder<QuantifierCorpusContext> builder = PredicateRegistry<QuantifierCorpusContext>
            .CreateBuilder()
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<QuantifierCorpusContext>(builder.Build());
    }

    private static async Task<Decision[]> EvaluateManyAsync(
        CompiledRule<QuantifierCorpusContext> rule,
        params QuantifierCorpusContext[] contexts
    )
    {
        return await rule.EvaluateManyAsync(contexts, cancellationToken: TestContext.Current.CancellationToken)
            .ToArrayAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>A collection context for the coverage corpus test: a plain list of K3 values the element predicate reads back.</summary>
    private sealed class QuantifierCorpusContext
    {
        public IReadOnlyList<TruthValue>? Items { get; init; }
    }
}
