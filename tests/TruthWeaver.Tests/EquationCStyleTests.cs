namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> in the <see cref="EquationDialect.CStyle"/>
/// dialect (ticket 06): the C-family operator glyphs, and <c>If</c>'s ternary rendering.
/// </summary>
public sealed class EquationCStyleTests
{
    private static readonly EquationOptions CStyle = new() { Dialect = EquationDialect.CStyle };

    /// <summary>An AND with an OR operand prints with the C-family glyphs and parentheses only around the OR.</summary>
    [Fact]
    public void PrintEquation_CStyle_AndWithOrOperand_PrintsCFamilyInfixWithMinimalParentheses_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("a && (b || c)", equation);
    }

    /// <summary>
    /// <c>AND</c>, <c>OR</c>, <c>XOR</c> and <c>EQUIVALENT</c> print as their C-family glyphs; <c>IMPLIES</c>,
    /// <c>NAND</c> and <c>NOR</c> have no C-family spelling and keep their word form.
    /// </summary>
    [Theory]
    [InlineData("AND", "&&")]
    [InlineData("OR", "||")]
    [InlineData("XOR", "^")]
    [InlineData("EQUIVALENT", "==")]
    [InlineData("IMPLIES", "IMPLIES")]
    [InlineData("NAND", "NAND")]
    [InlineData("NOR", "NOR")]
    public void PrintEquation_CStyleBinaryConnective_PrintsItsGlyphOrWordFormInfix_Test(string word, string token)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal($"a {token} b", equation);
    }

    /// <summary><c>NOT</c> prints as the prefix glyph <c>!</c>, directly before its operand.</summary>
    [Fact]
    public void PrintEquation_CStyleNot_PrintsThePrefixGlyph_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("!a", equation);
    }

    /// <summary>
    /// <c>If(condition, whenTrue, whenFalse)</c> prints as the ternary <c>condition ? whenTrue : whenFalse</c>
    /// instead of a call, uniquely among the dialects.
    /// </summary>
    [Fact]
    public void PrintEquation_CStyleIf_PrintsAsTernary_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("If(a, b, c)");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("a ? b : c", equation);
    }

    /// <summary>A connective nested inside an <c>If</c> branch is parenthesized, matching every other connective operand.</summary>
    [Fact]
    public void PrintEquation_CStyleIfWithConnectiveBranch_ParenthesizesTheBranch_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("If(a, b AND c, d)");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("a ? (b && c) : d", equation);
    }

    /// <summary>An <c>If</c> nested as an operand of a connective is parenthesized, since it no longer delimits itself as a call does.</summary>
    [Fact]
    public void PrintEquation_CStyleConnectiveWithIfOperand_ParenthesizesTheTernary_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND If(b, c, d)");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("a && (b ? c : d)", equation);
    }

    /// <summary><c>COALESCE</c> has no call syntax in C and keeps its function-call form, unlike <c>If</c>.</summary>
    [Fact]
    public void PrintEquation_CStyleCoalesce_KeepsFunctionCallForm_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("COALESCE(a, b)");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("COALESCE(a, b)", equation);
    }

    /// <summary>A constant prints as its letter by default, unescaped.</summary>
    [Fact]
    public void PrintEquation_CStyleConstant_PrintsCanonicalSpelling_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("a && U", equation);
    }

    /// <summary>A term prints as its full call, with its argument values, by default.</summary>
    [Fact]
    public void PrintEquation_CStyleDefaultOptions_PrintsTheFullTermCall_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND a");

        string equation = rule.PrintEquation(CStyle);

        Assert.Equal("hasCrust(crust: \"thin\") && a", equation);
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
