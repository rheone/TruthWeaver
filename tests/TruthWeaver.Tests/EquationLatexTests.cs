namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> in the <see cref="EquationDialect.LaTeX"/>
/// dialect with its three wrap modes.
/// </summary>
public sealed class EquationLatexTests
{
    private const string Label = "a_b%&#{}^~$";

    private static readonly EquationOptions Raw = new() { Dialect = EquationDialect.LaTeX };

    /// <summary>The raw wrap mode writes the equation with no delimiters.</summary>
    [Fact]
    public void PrintEquation_LatexRaw_PrintsCommandsWithNoDelimiters_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation(Raw);

        Assert.Equal(@"\text{a} \land (\text{b} \lor \text{c})", equation);
    }

    /// <summary>Each binary connective prints as its LaTeX command, infix.</summary>
    [Theory]
    [InlineData("AND", @"\land")]
    [InlineData("OR", @"\lor")]
    [InlineData("XOR", @"\oplus")]
    [InlineData("EQUIVALENT", @"\leftrightarrow")]
    [InlineData("IMPLIES", @"\rightarrow")]
    [InlineData("NAND", @"\uparrow")]
    [InlineData("NOR", @"\downarrow")]
    public void PrintEquation_LatexBinaryConnective_PrintsItsCommandInfix_Test(string word, string command)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation(Raw);

        Assert.Equal($@"\text{{a}} {command} \text{{b}}", equation);
    }

    /// <summary><c>NOT</c> prints as <c>\lnot</c> followed by a space, so the command does not join its operand.</summary>
    [Fact]
    public void PrintEquation_LatexNot_PrintsTheCommandThenASpace_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string equation = rule.PrintEquation(Raw);

        Assert.Equal(@"\lnot \text{a}", equation);
    }

    /// <summary>A constant prints as its canonical spelling in a text group.</summary>
    [Fact]
    public void PrintEquation_LatexConstant_PrintsTheSpellingInATextGroup_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        string equation = rule.PrintEquation(Raw);

        Assert.Equal(@"\text{a} \land \text{Unknown}", equation);
    }

    /// <summary>
    /// An operator with no LaTeX symbol prints as a function call. The name is a text group. The count arguments come
    /// first, in the same order as the DSL.
    /// </summary>
    [Theory]
    [InlineData("AtLeast(2, a, b, c)", @"\text{AtLeast}(2, \text{a}, \text{b}, \text{c})")]
    [InlineData("ExactlyOne(a, b)", @"\text{ExactlyOne}(\text{a}, \text{b})")]
    [InlineData("BETWEEN(1, 2, a, b, c)", @"\text{BETWEEN}(1, 2, \text{a}, \text{b}, \text{c})")]
    [InlineData("If(a, b AND c, d)", @"\text{If}(\text{a}, \text{b} \land \text{c}, \text{d})")]
    public void PrintEquation_LatexOperatorWithNoSymbol_PrintsFunctionCallForm_Test(string dsl, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string equation = rule.PrintEquation(Raw);

        Assert.Equal(expected, equation);
    }

    /// <summary>The <c>$$</c> wrap mode puts the raw equation between double dollar signs.</summary>
    [Fact]
    public void PrintEquation_LatexDoubleDollar_WrapsTheEquationInDoubleDollars_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.DoubleDollar }
        );

        Assert.Equal(@"$$\text{a} \land \text{b}$$", equation);
    }

    /// <summary>
    /// The MathJax-safe wrap mode uses the <c>$`...`$</c> form, which GitHub reads as math and Markdown leaves alone,
    /// so an underscore does not start emphasis or a subscript.
    /// </summary>
    [Fact]
    public void PrintEquation_LatexMathJaxSafe_WrapsTheEquationInDollarBackticks_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.MathJaxSafe }
        );

        Assert.Equal(@"$`\text{a} \land \text{b}`$", equation);
    }

    /// <summary>In raw mode a term with every special character escapes inside the text group.</summary>
    [Fact]
    public void PrintEquation_LatexRawWithSpecialCharacters_EscapesThemInTheTextGroup_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileLabel();

        string equation = rule.PrintEquation(Raw);

        Assert.Equal(@"\text{hasCrust(crust: ""a\_b\%\&\#\{\}\textasciicircum{}\textasciitilde{}\$"")}", equation);
    }

    /// <summary>The <c>$$</c> mode escapes the same way as the raw mode.</summary>
    [Fact]
    public void PrintEquation_LatexDoubleDollarWithSpecialCharacters_EscapesThemInTheTextGroup_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileLabel();

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.DoubleDollar }
        );

        Assert.Equal(@"$$\text{hasCrust(crust: ""a\_b\%\&\#\{\}\textasciicircum{}\textasciitilde{}\$"")}$$", equation);
    }

    /// <summary>The MathJax-safe mode writes each special character as a math command outside the text group.</summary>
    [Fact]
    public void PrintEquation_LatexMathJaxSafeWithSpecialCharacters_UsesMathCommandsOutsideTheTextGroup_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileLabel();

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.MathJaxSafe }
        );

        Assert.Equal(@"$`\text{hasCrust(crust: ""a}\_\text{b}\%\&\#\{\}\hat{\ }\sim\$\text{"")}`$", equation);
    }

    /// <summary>With argument values hidden, the term is the escaped predicate name.</summary>
    [Fact]
    public void PrintEquation_LatexArgumentValuesHidden_PrintsTheEscapedPredicateName_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("has_crust AND a");

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.LaTeX, ShowArgumentValues = false }
        );

        Assert.Equal(@"\text{has\_crust} \land \text{a}", equation);
    }

    private static CompiledRule<RuleTestContext> CompileLabel()
    {
        return Compile($"hasCrust(crust: \"{Label}\")");
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .AddConstant("has_crust", true)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
