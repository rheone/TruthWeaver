namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>The <c>Rule</c> and <c>GetRuleOrThrow</c> members let a caller read a compiled rule without the null-forgiving operator.</summary>
public sealed class CompilationResultRuleAccessorTests
{
    /// <summary>After a successful check, <c>Rule</c> is the compiled rule and needs no <c>!</c>.</summary>
    [Fact]
    public void Rule_AfterSucceededCheck_ReturnsTheCompiledRule_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("isManager");

        Assert.True(result.Succeeded);
        CompiledRuleHolder holder = new(result.Rule);
        Assert.Same(result.CompiledRule, holder.Rule);
    }

    /// <summary>A failed compilation has no <c>Rule</c>.</summary>
    [Fact]
    public void Rule_OnFailedCompilation_IsNull_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("noSuchPredicate");

        Assert.Null(result.Rule);
    }

    /// <summary>A successful result returns its rule from <c>GetRuleOrThrow</c>.</summary>
    [Fact]
    public void GetRuleOrThrow_OnSuccess_ReturnsTheCompiledRule_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("isManager");

        Assert.Same(result.CompiledRule, result.GetRuleOrThrow());
    }

    /// <summary>A failed result throws an exception whose message contains each error diagnostic and no warning or info finding.</summary>
    [Fact]
    public void GetRuleOrThrow_OnFailure_ThrowsWithEachErrorDiagnostic_Test()
    {
        CompilationResult<RuleTestContext> result = CreateCompiler().Compile("noSuchPredicate AND otherMissing");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => result.GetRuleOrThrow());

        IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
        Assert.NotEmpty(errors);
        Assert.All(errors, d => Assert.Contains(d.Message, exception.Message, StringComparison.Ordinal));
        Assert.All(errors, d => Assert.Contains(d.Code, exception.Message, StringComparison.Ordinal));
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .Build();
        return new RuleCompiler<RuleTestContext>(registry);
    }

    // Takes a non-nullable rule, so the test compiles with a warning-as-error only when the Succeeded check narrows Rule.
    private sealed record CompiledRuleHolder(TruthWeaver.Evaluation.CompiledRule<RuleTestContext> Rule);
}
