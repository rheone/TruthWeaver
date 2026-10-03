namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Project is a method on the result, not part of the rule (ADR-0005 decision 12): <c>Decision.Project(unknownAs)</c>
/// keeps <c>True</c>/<c>False</c> and replaces <c>Unknown</c> with the chosen definite value without changing
/// <c>Decision.Result</c>, and rule text, JSON and YAML that still declare a <c>Project</c> are rejected with a diagnostic
/// that points to <c>COALESCE</c> and <c>Decision.Project</c>.
/// </summary>
public sealed class ProjectTests
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
    /// <c>Decision.Project(unknownAs)</c> over every K3 result and both choices matches the oracle, is never
    /// <c>Unknown</c>, and the decision's own result is the raw value whatever was projected.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Project_OverAllInputs_MatchesOracleAndLeavesTheResultRaw_Test(bool unknownAs)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;
        TruthValue replacement = unknownAs ? TruthValue.True : TruthValue.False;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            TruthValue projected = decision.Project(unknownAs);

            Assert.Equal(K3Oracle.Project(assignment[0], replacement), projected);
            Assert.NotEqual(TruthValue.Unknown, projected);
            Assert.Equal(assignment[0], decision.Result);
        }
    }

    /// <summary>The projection applies to a composed expression's K3 result, not to its parts.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Project_OverAComposedExpression_ProjectsItsK3Result_Test(bool unknownAs)
    {
        K3Rule rule = K3Rule.TryCreate("a AND (b OR NOT c)", 3)!;
        TruthValue replacement = unknownAs ? TruthValue.True : TruthValue.False;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            TruthValue inner = K3Oracle.And([assignment[0], K3Oracle.Or([assignment[1], K3Oracle.Not(assignment[2])])]);

            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Project(inner, replacement), decision.Project(unknownAs));
        }
    }

    /// <summary>
    /// <c>Decision.Project(v)</c> and a rule written <c>COALESCE(rule, v)</c> agree for every input (ADR-0005 decision 12),
    /// which is why the in-rule form needs no operator of its own.
    /// </summary>
    [Theory]
    [InlineData(true, "True")]
    [InlineData(false, "False")]
    public async Task Project_OverAllInputs_AgreesWithACoalesceInsideTheRule_Test(bool unknownAs, string value)
    {
        K3Rule raw = K3Rule.TryCreate("a AND b", 2)!;
        K3Rule coalesced = K3Rule.TryCreate($"COALESCE(a AND b, {value})", 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision viaMethod = await raw.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision viaCoalesce = await coalesced.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(viaCoalesce.Result, viaMethod.Project(unknownAs));
        }
    }

    /// <summary>
    /// A faulting predicate is Unknown plus a Fault; projecting makes the value definite but is pure, so the Fault, the
    /// <c>Unknown</c> result and the fail-closed <c>IsSatisfied</c> are all unchanged and a caller can still tell "not known"
    /// from "broke".
    /// </summary>
    [Theory]
    [InlineData(true, TruthValue.True)]
    [InlineData(false, TruthValue.False)]
    public async Task Project_OverAFaultingPredicate_KeepsTheFaultAndTheUnknownResult_Test(bool unknownAs, TruthValue expected)
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
        TruthValue projected = decision.Project(unknownAs);

        Assert.Equal(expected, projected);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Single(decision.Faults);
    }

    /// <summary>
    /// <c>Decision.IsSatisfied</c> stays fail-closed: true only for a <c>True</c> result. Projecting <c>Unknown</c> to
    /// <c>True</c> does not change that.
    /// </summary>
    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    [InlineData(TruthValue.Unknown, false)]
    public async Task IsSatisfied_IsTrueOnlyForTrueWhateverProjectionIsApplied_Test(TruthValue input, bool expected)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);
        TruthValue lenient = decision.Project(unknownAs: true);

        Assert.Equal(expected, decision.IsSatisfied);
        Assert.Equal(input == TruthValue.False ? TruthValue.False : TruthValue.True, lenient);
        Assert.Equal(expected, decision.IsSatisfied);
    }

    /// <summary>
    /// A rule that still declares <c>Project</c> is rejected wherever it sits and in any letter case, with a diagnostic
    /// that names <c>COALESCE</c> and <c>Decision.Project</c> and spans the whole project call.
    /// </summary>
    [Theory]
    [InlineData("Project(a, True)", "Project(a, True)")]
    [InlineData("project(a AND b, FALSE)", "project(a AND b, FALSE)")]
    [InlineData("a AND Project(b, False)", "Project(b, False)")]
    [InlineData("NOT Project(a, False)", "Project(a, False)")]
    [InlineData("Project(a)", "Project(a)")]
    [InlineData("Project(a, Unknown)", "Project(a, Unknown)")]
    public void Compile_DeclaredProject_IsRejectedWithADiagnosticPointingToCoalesceAndDecisionProject_Test(
        string text,
        string call
    )
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic outermost = Assert.Single(
            result.Diagnostics,
            d => d.Message.Contains("Decision.Project", StringComparison.Ordinal)
        );
        Assert.Equal(DiagnosticCodes.SyntaxError, outermost.Code);
        Assert.Contains("COALESCE", outermost.Message, StringComparison.Ordinal);
        Assert.Equal(call, text.Substring(outermost.Span.Start, outermost.Span.Length));
    }

    /// <summary>Every project in a rule is its own diagnostic, and the rest of the text is still checked.</summary>
    [Fact]
    public void Compile_TwoDeclaredProjects_ReportsEachOne_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("Project(a, True) AND Project(b, False)");

        Assert.Equal(2, result.Diagnostics.Count(d => d.Message.Contains("Decision.Project", StringComparison.Ordinal)));
    }

    /// <summary><c>Project</c> stays reserved so a predicate cannot shadow the rejected word.</summary>
    [Theory]
    [InlineData("project")]
    [InlineData("Project")]
    [InlineData("PROJECT")]
    public void IsReservedWord_Project_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>The replacement the diagnostic recommends, <c>COALESCE(x, True|False)</c>, compiles and is definite.</summary>
    [Theory]
    [InlineData("COALESCE(a, True)")]
    [InlineData("COALESCE(a AND b, False)")]
    public void Compile_CoalesceWithAConstant_IsAcceptedAsTheInRuleReplacement_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
    }

    /// <summary>A JSON project node, in any letter case and nested or not, is rejected pointing to <c>Decision.Project</c>.</summary>
    [Theory]
    [InlineData("""{"op": "project", "unknownAs": true, "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "Project", "unknownAs": "false", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "project", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "not", "operands": [{"op": "project", "unknownAs": true, "operands": [{"predicate": "a"}]}]}""")]
    public void CompileJson_DeclaredProject_IsRejectedWithADiagnosticPointingToDecisionProject_Test(string json)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Contains("Decision.Project", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A YAML project node is rejected the same way, at the node's path.</summary>
    [Fact]
    public void CompileYaml_DeclaredProject_IsRejectedWithADiagnosticPointingToDecisionProject_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: not\noperands:\n- op: project\n  unknownAs: true\n  operands:\n  - predicate: a\n"
        );

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, error.Code);
        Assert.Contains("Decision.Project", error.Message, StringComparison.Ordinal);
        Assert.NotNull(error.Path);
    }

    /// <summary>The compiled rule's canonical text, JSON and description never mention a project.</summary>
    [Fact]
    public void Compile_PlainRule_HasNoProjectAnywhereInItsRenderings_Test()
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("COALESCE(a AND b, True)").CompiledRule!;

        Assert.DoesNotContain("project", rule.CanonicalText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("project", rule.PrintJson(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("COALESCE", rule.Describe().Label);
    }
}
