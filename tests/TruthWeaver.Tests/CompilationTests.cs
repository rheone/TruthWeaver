namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 02: the tracer bullet — compile and evaluate the smallest possible rule shapes.</summary>
public sealed class CompilationTests
{
    [Fact]
    public async Task Registered_zero_arg_predicate_evaluates_to_its_registered_value()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        CompilationResult<RuleTestContext> result = compiler.Compile("isManager");

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
        Assert.True(decision.IsSatisfied);
    }

    [Fact]
    public async Task Registered_zero_arg_predicate_evaluates_to_false_when_registered_false()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("isManager")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.False(decision.IsSatisfied);
    }

    [Fact]
    public void Unregistered_predicate_produces_error_diagnostic_with_span_and_null_compiled_rule()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>.CreateBuilder().Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, diagnostic.Code);
        Assert.Equal(new SourceSpan(0, "noSuchPredicate".Length), diagnostic.Span);
    }

    [Fact]
    public void Unrecognized_escape_sequence_in_a_string_literal_produces_error_diagnostic_and_null_compiled_rule()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"a\\pb\")");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.InvalidEscapeSequence && d.Severity == DiagnosticSeverity.Error
        );
    }

    [Theory]
    [InlineData("true", TruthValue.True)]
    [InlineData("false", TruthValue.False)]
    public async Task Constant_literal_rule_evaluates_to_the_matching_constant(string dsl, TruthValue expected)
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        Decision decision = await compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task Throwing_predicate_is_absorbed_as_a_fault_and_evaluation_completes()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("flaky")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("flaky")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("flaky", fault.Term.PredicateName);
        Assert.IsType<InvalidOperationException>(fault.Exception);
    }

    /// <summary>
    /// <c>CompileText</c> names the DSL the way <c>CompileJson</c> and <c>CompileYaml</c> name their own formats
    /// (ticket 24); it is the same method as <c>Compile</c>, which stays, so the two give identical results for
    /// a rule that compiles successfully.
    /// </summary>
    [Fact]
    public void CompileText_SameDslTextAsCompile_ProducesEquivalentResult_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<RuleTestContext> viaCompile = compiler.Compile("isManager AND NOT isManager");
        CompilationResult<RuleTestContext> viaCompileText = compiler.CompileText("isManager AND NOT isManager");

        Assert.True(viaCompile.Succeeded);
        Assert.True(viaCompileText.Succeeded);
        Assert.Equal(viaCompile.CompiledRule!.CanonicalText, viaCompileText.CompiledRule!.CanonicalText);
    }

    /// <summary>
    /// <c>CompileText</c> reports the same diagnostics as <c>Compile</c> for rule text that fails to compile, since
    /// it forwards to the same method.
    /// </summary>
    [Fact]
    public void CompileText_DslTextWithAnError_ProducesTheSameDiagnosticsAsCompile_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> viaCompile = compiler.Compile("noSuchPredicate");
        CompilationResult<RuleTestContext> viaCompileText = compiler.CompileText("noSuchPredicate");

        Assert.False(viaCompileText.Succeeded);
        Assert.Equal(viaCompile.Diagnostics, viaCompileText.Diagnostics);
    }

    /// <summary>
    /// <see cref="CompilationResult{TContext}.Diagnostics"/> is an <see cref="EquatableArray{T}"/> (ticket 28), so two
    /// failed compiles of the same invalid rule text — each producing a fresh diagnostics list and a
    /// <see langword="null"/> <c>CompiledRule</c> — compare equal and share a hash code, instead of the reference
    /// equality a plain <see cref="IReadOnlyList{T}"/> member would give two different lists.
    /// </summary>
    [Fact]
    public void Compile_SameInvalidRuleTwice_ProducesEqualCompilationResults_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> first = compiler.Compile("noSuchPredicate");
        CompilationResult<RuleTestContext> second = compiler.Compile("noSuchPredicate");

        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// <see cref="Decision.Faults"/> and <see cref="Decision.Trace"/> are <see cref="EquatableArray{T}"/>-backed
    /// (ticket 28), so two <c>EvaluateAsync</c> calls against the same compiled rule and an equivalent context
    /// produce Decisions that compare equal and share a hash code — the trace entries and tree nodes compare by
    /// value, not by the reference identity of the lists the evaluator happened to allocate for each call.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_CalledTwiceWithTheSameRuleAndContext_ProducesEqualDecisions_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .AddConstant("isSuspended", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager AND NOT isSuspended").GetRuleOrThrow();

        Decision first = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision second = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
