namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 14: <c>RuleNodeCompiler</c>'s malformed-tree diagnostics and optional-argument default
/// substitution. The JSON front end doesn't enforce AND/OR/ExactlyOne/threshold operand-count arity at
/// parse time (only the DSL parser's own grammar makes that structurally impossible), so a
/// hand-written or embedded JSON subtree reaches the compiler's own defensive arity checks — reachable
/// through the public <c>RuleCompiler.CompileJson</c> surface, no internal access needed.
/// </summary>
public sealed class RuleNodeCompilerDiagnosticsTests
{
    /// <summary>A JSON operator below its minimum operand count is an operand-count diagnostic, not an exception.</summary>
    [Theory]
    [InlineData("""{"op": "and", "operands": [{"const": true}]}""")]
    [InlineData("""{"op": "or", "operands": []}""")]
    [InlineData("""{"op": "exactlyOne", "operands": [{"const": true}]}""")]
    public void CompileJson_VariadicOperatorWithFewerThanTwoOperands_ReportsInfixArityViolationNotAnException_Test(string json)
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.InfixArityViolation
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("requires at least", StringComparison.Ordinal)
        );
    }

    /// <summary>A JSON threshold with no operands is an operand-count diagnostic, not an exception.</summary>
    [Fact]
    public void CompileJson_ThresholdWithZeroOperands_ReportsInfixArityViolationNotAnException_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = compiler.CompileJson("""{"op": "atLeast", "k": 1, "operands": []}""");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.InfixArityViolation
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("requires at least one operand", StringComparison.Ordinal)
        );
    }

    /// <summary>A term argument name that the predicate schema does not declare produces an unknown argument diagnostic.</summary>
    [Fact]
    public void Arguments_UndeclaredName_ProducesUnknownArgumentDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(bogusArg: \"x\")");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.UnknownArgument
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("bogusArg", StringComparison.Ordinal)
        );
    }

    /// <summary>An omitted optional argument with a declared default resolves to that default when evaluated.</summary>
    [Fact]
    public async Task Arguments_OmittedOptionalWithDefault_ResolvesToDefault_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasRoleOrDefault",
                        "hasRoleOrDefault",
                        "True iff 'role' equals 'GUEST', its declared default when omitted.",
                        [
                            new PredicateArgumentSchema(
                                "role",
                                "The role to check for; defaults to 'GUEST' when omitted.",
                                LiteralKind.String,
                                Required: false,
                                Default: LiteralValue.OfString("GUEST")
                            ),
                        ]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(
                            string.Equals(args.GetString("role"), "GUEST", StringComparison.Ordinal)
                                ? TruthValue.True
                                : TruthValue.False
                        )
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRoleOrDefault");

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }
}
