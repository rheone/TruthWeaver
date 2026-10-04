namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The dedicated Unicode logic symbols <c>⊼ ⊽ ⊻ ⇒ ⇔</c> are input-only aliases of <c>NAND</c>, <c>NOR</c>,
/// <c>XOR</c>, <c>IMPLIES</c> and <c>EQUIVALENT</c> (ADR-0005 decision 2): they compile to the same tree as the named
/// operator and the canonical printer never writes them.
/// </summary>
public sealed class UnicodeInputAliasTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Each Unicode symbol compiles to the same canonical text as the named operator it aliases.</summary>
    [Theory]
    [InlineData("a ⊼ b", "(a NAND b)")]
    [InlineData("a ⊽ b", "(a NOR b)")]
    [InlineData("a ⊻ b", "(a XOR b)")]
    [InlineData("a ⇒ b", "(a IMPLIES b)")]
    [InlineData("a ⇔ b", "(a EQUIVALENT b)")]
    public void Compile_UnicodeAlias_ProducesTheNamedOperatorsCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>The canonical text never contains any of the five alias symbols, so it stays word-only.</summary>
    [Theory]
    [InlineData("a ⊼ b")]
    [InlineData("a ⊽ b")]
    [InlineData("a ⊻ b")]
    [InlineData("a ⇒ b")]
    [InlineData("a ⇔ b")]
    public void Compile_UnicodeAlias_CanonicalTextIsWordOnly_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.CompiledRule!.CanonicalText, c => "⊼⊽⊻⇒⇔".Contains(c));
    }

    /// <summary>An alias mixes freely with other operators under the same precedence rules as the named operator.</summary>
    [Fact]
    public void Compile_UnicodeAliasInsideAnAndChain_FollowsTheNamedOperatorsPrecedence_Test()
    {
        CompilationResult<RuleTestContext> aliased = Compiler.Compile("(a ⇒ b) && c");
        CompilationResult<RuleTestContext> named = Compiler.Compile("(a IMPLIES b) AND c");

        Assert.True(aliased.Succeeded);
        Assert.Equal(named.CompiledRule!.CanonicalText, aliased.CompiledRule!.CanonicalText);
    }

    /// <summary>The no-mixing rule applies to an alias exactly as to the named operator.</summary>
    [Fact]
    public void Compile_TwoUnicodeAliasesWithoutParentheses_IsRejectedLikeTheNamedOperators_Test()
    {
        CompilationResult<RuleTestContext> aliased = Compiler.Compile("a ⇒ b ⇔ c");
        CompilationResult<RuleTestContext> named = Compiler.Compile("a IMPLIES b EQUIVALENT c");

        Assert.False(named.Succeeded);
        Assert.False(aliased.Succeeded);
    }
}
