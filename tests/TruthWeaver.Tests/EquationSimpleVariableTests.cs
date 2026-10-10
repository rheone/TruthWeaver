namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Simple-variable mode of <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> and
/// <see cref="CompiledRule{TContext}.PrintEquationWithLegend(EquationOptions?)"/>: each term becomes a letter, and the
/// legend maps each letter back to its term.
/// </summary>
public sealed class EquationSimpleVariableTests
{
    private static readonly EquationOptions Simple = new() { SimpleVariables = true };

    /// <summary>Letters start at <c>p</c> and follow the first occurrence of each term, left to right.</summary>
    [Fact]
    public void PrintEquation_SimpleVariables_AssignsLettersByFirstOccurrence_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(c AND a) OR b");

        string equation = rule.PrintEquation(Simple);

        Assert.Equal("(p ∧ q) ∨ r", equation);
    }

    /// <summary>The letters follow a depth-first walk, so a term inside a nested operand is lettered before a later sibling.</summary>
    [Fact]
    public void PrintEquation_SimpleVariables_AssignsLettersDepthFirst_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(d AND NOT b) OR (c AND a)");

        string equation = rule.PrintEquation(Simple);

        Assert.Equal("(p ∧ ¬q) ∨ (r ∧ s)", equation);
    }

    /// <summary>Identical terms share one letter.</summary>
    [Fact]
    public void PrintEquation_SimpleVariablesWithRepeatedTerm_ReusesTheLetter_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR a)");

        string equation = rule.PrintEquation(Simple);

        Assert.Equal("p ∧ (q ∨ p)", equation);
    }

    /// <summary>The same rule gets the same lettering on every call.</summary>
    [Fact]
    public void PrintEquation_SimpleVariablesCalledTwice_GivesTheSameText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c) AND NOT a");

        Assert.Equal(rule.PrintEquation(Simple), rule.PrintEquation(Simple));
        Assert.Equal(rule.PrintEquationWithLegend(Simple).LegendText, rule.PrintEquationWithLegend(Simple).LegendText);
    }

    /// <summary>A constant and a function name are not terms, so they get no letter.</summary>
    [Fact]
    public void PrintEquation_SimpleVariablesWithConstantAndFunction_LettersOnlyTheTerms_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(2, a, b, Unknown)");

        string equation = rule.PrintEquation(Simple);

        Assert.Equal("AtLeast(2, p, q, Unknown)", equation);
    }

    /// <summary>Two terms of one predicate with different argument values are different terms and get different letters.</summary>
    [Fact]
    public void PrintEquation_SimpleVariablesWithDifferentArguments_UsesTwoLetters_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND hasCrust(crust: \"thick\")");

        string equation = rule.PrintEquation(Simple);

        Assert.Equal("p ∧ q", equation);
    }

    /// <summary>The legend has one entry per distinct term, in letter order, with the full term call.</summary>
    [Fact]
    public void PrintEquationWithLegend_RuleWithRepeatedTerm_ReturnsOneEntryPerDistinctTerm_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND (a OR hasCrust(crust: \"thin\"))");

        EquationWithLegend result = rule.PrintEquationWithLegend();

        Assert.Equal("p ∧ (q ∨ p)", result.Equation);
        Assert.Collection(
            result.Legend,
            entry =>
            {
                Assert.Equal("p", entry.Variable);
                Assert.Equal("hasCrust(crust: \"thin\")", entry.TermText);
                Assert.Equal("hasCrust", entry.Term.PredicateName);
            },
            entry =>
            {
                Assert.Equal("q", entry.Variable);
                Assert.Equal("a", entry.TermText);
            }
        );
    }

    /// <summary>The legend text has one <c>letter = term</c> line per entry.</summary>
    [Fact]
    public void PrintEquationWithLegend_Unicode_WritesOneLinePerEntry_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"thin\")");

        EquationWithLegend result = rule.PrintEquationWithLegend();

        Assert.Equal("p = a\nq = hasCrust(crust: \"thin\")", result.LegendText);
    }

    /// <summary>The legend shows the full term call even when the equation hides argument values.</summary>
    [Fact]
    public void PrintEquationWithLegend_ArgumentValuesHidden_StillShowsTheFullTermInTheLegend_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\")");

        EquationWithLegend result = rule.PrintEquationWithLegend(new EquationOptions { ShowArgumentValues = false });

        Assert.Equal("p = hasCrust(crust: \"thin\")", result.LegendText);
    }

    /// <summary>The overload turns on simple-variable mode without the option.</summary>
    [Fact]
    public void PrintEquationWithLegend_DefaultOptions_MatchesPrintEquationWithSimpleVariables_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a IMPLIES (b XOR c)");

        Assert.Equal(rule.PrintEquation(Simple), rule.PrintEquationWithLegend().Equation);
    }

    /// <summary>
    /// After the eleventh term, the letters <c>p</c> to <c>z</c> repeat with a numeric subscript: <c>p₁</c>, <c>q₁</c>,
    /// and so on.
    /// </summary>
    [Fact]
    public void PrintEquationWithLegend_MoreTermsThanLetters_ContinuesWithSubscripts_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileManyTerms(24);

        EquationWithLegend result = rule.PrintEquationWithLegend();

        string[] variables = [.. result.Legend.Select(e => e.Variable)];
        Assert.Equal(24, variables.Length);
        Assert.Equal("p", variables[0]);
        Assert.Equal("z", variables[10]);
        Assert.Equal("p₁", variables[11]);
        Assert.Equal("z₁", variables[21]);
        Assert.Equal("p₂", variables[22]);
        Assert.Equal("q₂", variables[23]);
        Assert.Equal(24, variables.Distinct().Count());
    }

    /// <summary>In the LaTeX dialect a subscript is written with braces.</summary>
    [Fact]
    public void PrintEquationWithLegend_LatexMoreTermsThanLetters_WritesBracedSubscripts_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileManyTerms(12);

        EquationWithLegend result = rule.PrintEquationWithLegend(new EquationOptions { Dialect = EquationDialect.LaTeX });

        Assert.EndsWith(@"\land p_{1}", result.Equation);
    }

    /// <summary>In the AsciiMath dialect a subscript follows an underscore.</summary>
    [Fact]
    public void PrintEquationWithLegend_AsciiMathMoreTermsThanLetters_WritesUnderscoreSubscripts_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileManyTerms(12);

        EquationWithLegend result = rule.PrintEquationWithLegend(new EquationOptions { Dialect = EquationDialect.AsciiMath });

        Assert.EndsWith("^^ p_1`", result.Equation);
    }

    /// <summary>In the LaTeX dialect the letters are plain math and the legend lines use the wrap mode.</summary>
    [Fact]
    public void PrintEquationWithLegend_LatexDoubleDollar_WrapsEquationAndEachLegendLine_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND hasCrust(crust: \"a_b\")");

        EquationWithLegend result = rule.PrintEquationWithLegend(
            new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = LatexWrapMode.DoubleDollar }
        );

        Assert.Equal(@"$$p \land q$$", result.Equation);
        Assert.Equal("$$p = \\text{a}$$\n$$q = \\text{hasCrust(crust: \"a\\_b\")}$$", result.LegendText);
        Assert.Equal(@"\text{hasCrust(crust: ""a\_b"")}", result.Legend[1].TermText);
    }

    /// <summary>In the AsciiMath dialect the legend term is a quoted run between backticks.</summary>
    [Fact]
    public void PrintEquationWithLegend_AsciiMath_QuotesTheLegendTerms_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND NOT b");

        EquationWithLegend result = rule.PrintEquationWithLegend(new EquationOptions { Dialect = EquationDialect.AsciiMath });

        Assert.Equal("`p ^^ not q`", result.Equation);
        Assert.Equal("`p = \"a\"`\n`q = \"b\"`", result.LegendText);
    }

    /// <summary>A rule with no term has an empty legend.</summary>
    [Fact]
    public void PrintEquationWithLegend_RuleWithoutTerms_ReturnsAnEmptyLegend_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True OR Unknown");

        EquationWithLegend result = rule.PrintEquationWithLegend();

        Assert.Empty(result.Legend);
        Assert.Equal(string.Empty, result.LegendText);
    }

    private static CompiledRule<RuleTestContext> CompileManyTerms(int count)
    {
        string dsl = string.Join(" AND ", Enumerable.Range(0, count).Select(i => $"hasCrust(crust: \"c{i}\")"));
        return Compile(dsl);
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
