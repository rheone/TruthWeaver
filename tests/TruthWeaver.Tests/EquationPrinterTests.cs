namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintEquation(EquationOptions?)"/>: the flat, single-line infix rendering of a
/// compiled rule in the plain Unicode dialect.
/// </summary>
public sealed class EquationPrinterTests
{
    /// <summary>An AND with an OR operand prints with Symbolic glyphs and parentheses only around the OR.</summary>
    [Fact]
    public void PrintEquation_AndWithOrOperand_PrintsSymbolicInfixWithMinimalParentheses_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string equation = rule.PrintEquation();

        Assert.Equal("a ∧ (b ∨ c)", equation);
    }

    /// <summary>Each binary K3 connective prints as its Symbolic glyph, infix, with no parentheses at the root.</summary>
    [Theory]
    [InlineData("AND", "∧")]
    [InlineData("OR", "∨")]
    [InlineData("XOR", "⊕")]
    [InlineData("EQUIVALENT", "↔")]
    [InlineData("IMPLIES", "→")]
    [InlineData("NAND", "↑")]
    [InlineData("NOR", "↓")]
    public void PrintEquation_BinaryConnective_PrintsItsSymbolicGlyphInfix_Test(string word, string glyph)
    {
        CompiledRule<RuleTestContext> rule = Compile($"a {word} b");

        string equation = rule.PrintEquation();

        Assert.Equal($"a {glyph} b", equation);
    }

    /// <summary><c>NOT</c> prints as the prefix glyph <c>¬</c>, directly before its operand.</summary>
    [Fact]
    public void PrintEquation_Not_PrintsThePrefixGlyph_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string equation = rule.PrintEquation();

        Assert.Equal("¬a", equation);
    }

    /// <summary>
    /// An operator with no infix symbol prints in function-call form, with the DSL spelling and its count arguments
    /// first, so a call over plain terms prints exactly as it is written in the DSL.
    /// </summary>
    [Theory]
    [InlineData("ExactlyOne(a, b, c)")]
    [InlineData("AtLeast(2, a, b, c)")]
    [InlineData("AtMost(2, a, b, c)")]
    [InlineData("Exactly(2, a, b, c)")]
    [InlineData("GreaterThan(1, a, b, c)")]
    [InlineData("LessThan(2, a, b, c)")]
    [InlineData("BETWEEN(1, 2, a, b, c)")]
    [InlineData("PARITY(a, b, c)")]
    [InlineData("ANY(a, b)")]
    [InlineData("ALL(a, b)")]
    [InlineData("NONE(a, b)")]
    [InlineData("IsTrue(a)")]
    [InlineData("IsFalse(a)")]
    [InlineData("IsUnknown(a)")]
    [InlineData("IsKnown(a)")]
    [InlineData("If(a, b, c)")]
    [InlineData("COALESCE(a, b)")]
    public void PrintEquation_OperatorWithNoInfixSymbol_PrintsFunctionCallForm_Test(string dsl)
    {
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string equation = rule.PrintEquation();

        Assert.Equal(dsl, equation);
    }

    /// <summary>
    /// A connective nested inside another connective is parenthesized. A negation, a function call and a
    /// function-call argument are never parenthesized.
    /// </summary>
    [Theory]
    [InlineData("NOT (a AND b)", "¬(a ∧ b)")]
    [InlineData("NOT NOT a", "¬¬a")]
    [InlineData("NOT a AND b", "¬a ∧ b")]
    [InlineData("(a XOR b) AND c", "(a ⊕ b) ∧ c")]
    [InlineData("a OR (b AND (c IMPLIES d))", "a ∨ (b ∧ (c → d))")]
    [InlineData("(a NAND b) EQUIVALENT (c NOR d)", "(a ↑ b) ↔ (c ↓ d)")]
    [InlineData("ExactlyOne(a AND b, c)", "ExactlyOne(a ∧ b, c)")]
    [InlineData("NOT AtLeast(2, a, b, c)", "¬AtLeast(2, a, b, c)")]
    [InlineData("AtLeast(2, a, b, c) OR (d AND ExactlyOne(a, b))", "AtLeast(2, a, b, c) ∨ (d ∧ ExactlyOne(a, b))")]
    public void PrintEquation_NestedExpression_ParenthesizesOnlyConnectivesInsideConnectives_Test(string dsl, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string equation = rule.PrintEquation();

        Assert.Equal(expected, equation);
    }

    /// <summary>A constant prints with its canonical spelling.</summary>
    [Fact]
    public void PrintEquation_Constant_PrintsCanonicalSpelling_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        string equation = rule.PrintEquation();

        Assert.Equal("a ∧ Unknown", equation);
    }

    /// <summary>A term prints as its full call, with its argument values, by default.</summary>
    [Fact]
    public void PrintEquation_DefaultOptions_PrintsTheFullTermCall_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND a");

        string equation = rule.PrintEquation();

        Assert.Equal("hasCrust(crust: \"thin\") ∧ a", equation);
    }

    /// <summary>With argument values hidden, a term prints as the bare predicate name.</summary>
    [Fact]
    public void PrintEquation_ArgumentValuesHidden_PrintsTheBarePredicateName_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\") AND a");

        string equation = rule.PrintEquation(new EquationOptions { ShowArgumentValues = false });

        Assert.Equal("hasCrust ∧ a", equation);
    }

    /// <summary>Printing an equation leaves the canonical DSL text word-only and fully grouped.</summary>
    [Fact]
    public void PrintEquation_AfterPrinting_LeavesCanonicalTextUnchanged_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a XOR (b AND c)");

        _ = rule.PrintEquation();

        Assert.Equal("(a XOR (b AND c))", rule.CanonicalText);
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
