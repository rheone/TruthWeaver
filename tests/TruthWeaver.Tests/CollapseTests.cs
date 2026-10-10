namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Collapse is a method on the result, not part of the rule (ADR-0005 decision 14): <c>Decision.Result</c> is always the raw
/// K3 value, <c>Decision.Collapse(policy)</c> is a pure call-site choice over it, <c>Decision.IsSatisfied</c> stays
/// fail-closed, and rule text, JSON and YAML that still declare a <c>Collapse</c> are rejected with a diagnostic that points
/// to <c>Decision.Collapse</c>.
/// </summary>
public sealed class CollapseTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>
    /// <c>Decision.Collapse(policy)</c> over every K3 result and every policy matches the oracle, and the decision's own
    /// result is the raw value whatever policy was applied.
    /// </summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse)]
    [InlineData(CollapsePolicy.UnknownAsTrue)]
    [InlineData(CollapsePolicy.UnknownIsError)]
    public async Task Collapse_OverAllInputs_MatchesOracleAndLeavesTheResultRaw_Test(CollapsePolicy policy)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Collapse(assignment[0], policy), decision.Collapse(policy));
            Assert.Equal(assignment[0], decision.Result);
        }
    }

    /// <summary>The collapse applies to a composed expression's K3 result, not to its parts.</summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse)]
    [InlineData(CollapsePolicy.UnknownAsTrue)]
    [InlineData(CollapsePolicy.UnknownIsError)]
    public async Task Collapse_OverAComposedExpression_CollapsesItsK3Result_Test(CollapsePolicy policy)
    {
        K3Rule rule = K3Rule.TryCreate("a AND (b OR NOT c)", 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            TruthValue inner = K3Oracle.And([assignment[0], K3Oracle.Or([assignment[1], K3Oracle.Not(assignment[2])])]);

            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Collapse(inner, policy), decision.Collapse(policy));
        }
    }

    /// <summary><c>UnknownIsError</c> on a merely unknown result is a rejected outcome with no fault and no exception.</summary>
    [Fact]
    public async Task Collapse_UnknownIsErrorOverAnUnknown_IsRejectedWithoutAFault_Test()
    {
        K3Rule rule = K3Rule.TryCreate("a AND b", 2)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.Unknown],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(CollapseOutcome.RejectedUnresolved, decision.Collapse(CollapsePolicy.UnknownIsError));
        Assert.Empty(decision.Faults);
    }

    /// <summary>
    /// "Not known" and "something broke" stay distinguishable: a faulting predicate yields the same rejected outcome but the
    /// fault is on <c>Decision.Faults</c>, which the clean unknown above does not have. Collapsing is pure and never adds or
    /// hides a fault, and the result stays <c>Unknown</c>.
    /// </summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse, CollapseOutcome.False)]
    [InlineData(CollapsePolicy.UnknownAsTrue, CollapseOutcome.True)]
    [InlineData(CollapsePolicy.UnknownIsError, CollapseOutcome.RejectedUnresolved)]
    public async Task Collapse_OverAFaultingPredicate_KeepsTheFaultAndTheUnknownResult_Test(
        CollapsePolicy policy,
        CollapseOutcome expected
    )
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("boom").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("boom").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        CollapseOutcome outcome = decision.Collapse(policy);

        Assert.Equal(expected, outcome);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Single(decision.Faults);
    }

    /// <summary>
    /// An undefined <see cref="CollapsePolicy"/> is a caller error whatever the result: it is rejected for True and False
    /// results as well as Unknown, so a bad value is not silently accepted just because it happened not to be needed.
    /// </summary>
    [Theory]
    [InlineData(TruthValue.True)]
    [InlineData(TruthValue.False)]
    [InlineData(TruthValue.Unknown)]
    public void Collapse_UndefinedPolicy_ThrowsForEveryResult_Test(TruthValue result)
    {
        Decision decision = new(result, []);

        Assert.Throws<ArgumentOutOfRangeException>("policy", () => decision.Collapse((CollapsePolicy)99));
    }

    /// <summary>
    /// <c>Decision.IsSatisfied</c> stays fail-closed: true only for a <c>True</c> result. Calling
    /// <c>Collapse(UnknownAsTrue)</c> on an <c>Unknown</c> decision does not change that.
    /// </summary>
    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    [InlineData(TruthValue.Unknown, false)]
    public async Task IsSatisfied_IsTrueOnlyForTrueWhateverPolicyIsApplied_Test(TruthValue input, bool expected)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);
        CollapseOutcome lenient = decision.Collapse(CollapsePolicy.UnknownAsTrue);

        Assert.Equal(expected, decision.IsSatisfied);
        Assert.Equal(input == TruthValue.False ? CollapseOutcome.False : CollapseOutcome.True, lenient);
        Assert.Equal(expected, decision.IsSatisfied);
    }

    /// <summary>
    /// A rule that still declares <c>Collapse</c> is rejected wherever it sits and in any letter case, with a diagnostic
    /// that names <c>Decision.Collapse</c> and spans the whole collapse call.
    /// </summary>
    [Theory]
    [InlineData("Collapse(a, UnknownAsFalse)", "Collapse(a, UnknownAsFalse)")]
    [InlineData("collapse(a AND b, unknownIsError)", "collapse(a AND b, unknownIsError)")]
    [InlineData("a AND Collapse(b, UnknownAsFalse)", "Collapse(b, UnknownAsFalse)")]
    [InlineData("NOT Collapse(a, UnknownIsError)", "Collapse(a, UnknownIsError)")]
    [InlineData("COALESCE(Collapse(a, UnknownAsFalse), True)", "Collapse(a, UnknownAsFalse)")]
    [InlineData("Collapse(a)", "Collapse(a)")]
    public void Compile_DeclaredCollapse_IsRejectedWithADiagnosticPointingToDecisionCollapse_Test(string text, string call)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.SyntaxError, error.Code);
        Assert.Contains("Decision.Collapse", error.Message, StringComparison.Ordinal);
        Assert.Equal(call, text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary>Every collapse in a rule is its own diagnostic, and the rest of the text is still checked.</summary>
    [Fact]
    public void Compile_TwoDeclaredCollapses_ReportsEachOne_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(
            "Collapse(a, UnknownAsFalse) AND Collapse(b, UnknownAsTrue)"
        );

        Assert.Equal(2, result.Diagnostics.Count(d => d.Message.Contains("Decision.Collapse", StringComparison.Ordinal)));
    }

    /// <summary><c>Collapse</c> stays reserved so a predicate cannot shadow the rejected word.</summary>
    [Theory]
    [InlineData("collapse")]
    [InlineData("Collapse")]
    [InlineData("COLLAPSE")]
    public void IsReservedWord_Collapse_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>A JSON collapse node, outermost or nested and in any letter case, is rejected pointing to <c>Decision.Collapse</c>.</summary>
    [Theory]
    [InlineData("""{"op": "collapse", "policy": "unknownAsFalse", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "Collapse", "policy": "unknownAsFalse", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "collapse", "operands": [{"predicate": "a"}]}""")]
    [InlineData(
        """{"op": "not", "operands": [{"op": "collapse", "policy": "unknownAsTrue", "operands": [{"predicate": "a"}]}]}"""
    )]
    public void CompileJson_DeclaredCollapse_IsRejectedWithADiagnosticPointingToDecisionCollapse_Test(string json)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Contains("Decision.Collapse", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A YAML collapse node is rejected the same way, at the node's path.</summary>
    [Fact]
    public void CompileYaml_DeclaredCollapse_IsRejectedWithADiagnosticPointingToDecisionCollapse_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: not\noperands:\n- op: collapse\n  policy: unknownAsFalse\n  operands:\n  - predicate: a\n"
        );

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Contains("Decision.Collapse", error.Message, StringComparison.Ordinal);
        Assert.NotNull(error.Path);
    }

    /// <summary>The compiled rule's canonical text, JSON and description never mention a collapse.</summary>
    [Fact]
    public void Compile_PlainRule_HasNoCollapseAnywhereInItsRenderings_Test()
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("a AND b").CompiledRule!;

        Assert.DoesNotContain("collapse", rule.CanonicalText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("collapse", rule.PrintJson(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("AND", rule.Outline().Label);
    }
}
