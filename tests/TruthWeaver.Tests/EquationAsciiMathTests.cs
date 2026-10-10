namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> in the <see cref="EquationDialect.AsciiMath"/>
/// dialect, and the quoting that the dialect applies to literal text.
/// </summary>
public sealed class EquationAsciiMathTests
{
    private static readonly EquationOptions Ascii = new() { Dialect = EquationDialect.AsciiMath };

    /// <summary>The equation is wrapped in backticks, the AsciiMath delimiter, and each term is quoted text.</summary>
    [Fact]
    public void PrintEquation_AsciiMath_WrapsInBackticksAndQuotesTerms_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal("`\"a\" ^^ (\"b\" vv \"c\")`", equation);
    }

    /// <summary>Each binary connective prints as its AsciiMath symbol, infix.</summary>
    [Theory]
    [InlineData("AND", "^^")]
    [InlineData("OR", "vv")]
    [InlineData("XOR", "oplus")]
    [InlineData("EQUIVALENT", "<=>")]
    [InlineData("IMPLIES", "=>")]
    [InlineData("NAND", "uarr")]
    [InlineData("NOR", "darr")]
    public void PrintEquation_AsciiMathBinaryConnective_PrintsItsSymbolInfix_Test(string word, string symbol)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal($"`\"a\" {symbol} \"b\"`", equation);
    }

    /// <summary><c>NOT</c> prints as <c>not</c> followed by a space.</summary>
    [Fact]
    public void PrintEquation_AsciiMathNot_PrintsTheSymbolThenASpace_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal("`not \"a\"`", equation);
    }

    /// <summary>A constant prints as its canonical spelling in quotes.</summary>
    [Fact]
    public void PrintEquation_AsciiMathConstant_PrintsTheSpellingInQuotes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal("`\"a\" ^^ \"Unknown\"`", equation);
    }

    /// <summary>An operator with no AsciiMath symbol prints as a function call with a quoted name.</summary>
    [Theory]
    [InlineData("AtLeast(2, a, b, c)", "`\"AtLeast\"(2, \"a\", \"b\", \"c\")`")]
    [InlineData("ExactlyOne(a, b)", "`\"ExactlyOne\"(\"a\", \"b\")`")]
    [InlineData("BETWEEN(1, 2, a, b, c)", "`\"BETWEEN\"(1, 2, \"a\", \"b\", \"c\")`")]
    [InlineData("If(a, b AND c, d)", "`\"If\"(\"a\", \"b\" ^^ \"c\", \"d\")`")]
    public void PrintEquation_AsciiMathOperatorWithNoSymbol_PrintsFunctionCallForm_Test(string dsl, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal(expected, equation);
    }

    /// <summary>
    /// A term with a quote in its argument value keeps the text in one quoted run. AsciiMath has no escape for a
    /// quote, so the printer replaces each straight quote with a right double quotation mark.
    /// </summary>
    [Fact]
    public void PrintEquation_AsciiMathTermWithArgument_ReplacesTheStraightQuotes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND a");

        string equation = rule.PrintEquation(Ascii);

        Assert.Equal("`\"hasCrust(crust: ”thin”)\" ^^ \"a\"`", equation);
    }

    /// <summary>A backtick in the text would end the AsciiMath envelope, so the printer replaces it.</summary>
    [Fact]
    public void Quote_TextWithBacktickAndQuote_ReplacesBoth_Test()
    {
        string quoted = AsciiMathText.Quote("a`b\"c");

        Assert.Equal("\"a‵b”c\"", quoted);
    }

    /// <summary>Characters other than the quote and the backtick need no escape inside AsciiMath quotes.</summary>
    [Fact]
    public void Quote_TextWithOtherSpecialCharacters_KeepsThemLiteral_Test()
    {
        string quoted = AsciiMathText.Quote("a_b^c\\d{e}%&#$~");

        Assert.Equal("\"a_b^c\\d{e}%&#$~\"", quoted);
    }

    /// <summary>With argument values hidden, the term is the quoted predicate name.</summary>
    [Fact]
    public void PrintEquation_AsciiMathArgumentValuesHidden_PrintsTheQuotedPredicateName_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND a");

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.AsciiMath, ShowArgumentValues = false }
        );

        Assert.Equal("`\"hasCrust\" ^^ \"a\"`", equation);
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
