namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A <c>let</c> use works as an operand of every operator form, not only <c>AND</c>, <c>OR</c> and <c>NOT</c>: the
/// expansion reaches into each operator and leaves a tree equal to the one written without the definition.
/// </summary>
public sealed class DefinitionsInEveryOperatorTests
{
    /// <summary>A definition used as an operand of an operator expands to the same canonical text as writing its body in place.</summary>
    [Theory]
    [InlineData("p XOR b", "a XOR b")]
    [InlineData("p EQUIVALENT b", "a EQUIVALENT b")]
    [InlineData("p IMPLIES b", "a IMPLIES b")]
    [InlineData("p NAND b", "a NAND b")]
    [InlineData("p NOR b", "a NOR b")]
    [InlineData("PARITY(p, b, c)", "PARITY(a, b, c)")]
    [InlineData("ANY(p, b)", "ANY(a, b)")]
    [InlineData("ALL(p, b)", "ALL(a, b)")]
    [InlineData("NONE(p, b)", "NONE(a, b)")]
    [InlineData("ExactlyOne(p, b, c)", "ExactlyOne(a, b, c)")]
    [InlineData("AtLeast(2, p, b, c)", "AtLeast(2, a, b, c)")]
    [InlineData("BETWEEN(1, 2, p, b, c)", "BETWEEN(1, 2, a, b, c)")]
    [InlineData("COALESCE(p, b)", "COALESCE(a, b)")]
    [InlineData("If(p, b, c)", "If(a, b, c)")]
    [InlineData("IsKnown(p)", "IsKnown(a)")]
    public void Compile_DefinitionAsOperandOfAnOperator_ExpandsToTheBodyInPlace_Test(string withDefinition, string inPlace)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> expanded = compiler.Compile($"let p = a; {withDefinition}").GetRuleOrThrow();
        CompiledRule<RuleTestContext> written = compiler.Compile(inPlace).GetRuleOrThrow();

        Assert.Equal(written.CanonicalText, expanded.CanonicalText);
        Assert.DoesNotContain("let", expanded.CanonicalText, StringComparison.Ordinal);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
    }
}
