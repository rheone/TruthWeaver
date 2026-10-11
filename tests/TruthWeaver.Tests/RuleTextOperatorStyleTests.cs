namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 06: the <see cref="OperatorStyle"/> parameter of <see cref="CompiledRule{TContext}.PrintRuleText"/> and its
/// <see cref="CompiledRule{TContext}.TryPrintRuleText(GroupingStyle, OperatorStyle, out string)"/> Try-first sibling.
/// </summary>
public sealed class RuleTextOperatorStyleTests
{
    /// <summary>With no argument, or with <see cref="OperatorStyle.Word"/>, the output is the same as <see cref="CompiledRule{TContext}.CanonicalText"/>.</summary>
    [Fact]
    public void PrintRuleText_WordStyle_MatchesCanonicalText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND (isDepartmentHead OR NOT isManager)");

        Assert.Equal(rule.CanonicalText, rule.PrintRuleText());
        Assert.Equal(rule.CanonicalText, rule.PrintRuleText(operatorStyle: OperatorStyle.Word));
    }

    /// <summary>
    /// <see cref="OperatorStyle.Symbolic"/> writes the mathematical symbol the DSL parser itself reads back for
    /// <c>AND</c>, <c>OR</c> and <c>NOT</c>; <c>NOT</c> touches its operand, with no following space.
    /// </summary>
    [Fact]
    public void PrintRuleText_Symbolic_WritesTheMathematicalSymbols_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND (isDepartmentHead OR NOT isManager)");

        string text = rule.PrintRuleText(operatorStyle: OperatorStyle.Symbolic);

        Assert.Equal("isManager ∧ (isDepartmentHead ∨ ¬isManager)", text);
    }

    /// <summary>Each binary connective with a symbolic spelling prints that symbol, infix, in <see cref="OperatorStyle.Symbolic"/>.</summary>
    [Theory]
    [InlineData("AND", "∧")]
    [InlineData("OR", "∨")]
    [InlineData("XOR", "⊕")]
    [InlineData("EQUIVALENT", "↔")]
    [InlineData("IMPLIES", "→")]
    [InlineData("NAND", "↑")]
    [InlineData("NOR", "↓")]
    public void PrintRuleText_SymbolicBinaryConnective_PrintsItsSymbolInfix_Test(string word, string symbol)
    {
        CompiledRule<RuleTestContext> rule = Compile($"isManager {word} isDepartmentHead");

        string text = rule.PrintRuleText(operatorStyle: OperatorStyle.Symbolic);

        Assert.Contains(symbol, text, StringComparison.Ordinal);
        Assert.DoesNotContain(word, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>COALESCE</c> and <c>If</c> keep their word/function-call spelling in <see cref="OperatorStyle.Symbolic"/>:
    /// the DSL reads <c>??</c> only as an infix operator and has no call syntax for it.
    /// </summary>
    [Theory]
    [InlineData("COALESCE(isManager, isDepartmentHead)")]
    [InlineData("If(isManager, isDepartmentHead, isManager)")]
    public void PrintRuleText_Symbolic_LeavesCoalesceAndIfInWordForm_Test(string dsl)
    {
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string text = rule.PrintRuleText(operatorStyle: OperatorStyle.Symbolic);

        Assert.Equal(dsl, text);
    }

    /// <summary>The symbolic text re-parses to a tree structurally equal to the original, so it is a true round trip.</summary>
    [Fact]
    public void PrintRuleText_Symbolic_ReparsesToAnEqualTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = compiler
            .Compile("isManager AND (isDepartmentHead OR NOT isManager OR (isManager XOR isDepartmentHead))")
            .CompiledRule!;

        string symbolic = original.PrintRuleText(operatorStyle: OperatorStyle.Symbolic);
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(symbolic).CompiledRule!;

        Assert.Equal(original.Root, reparsed.Root);
    }

    /// <summary>Combining <see cref="GroupingStyle.DepthCycling"/> with <see cref="OperatorStyle.Symbolic"/> applies both.</summary>
    [Fact]
    public void PrintRuleText_SymbolicWithDepthCycling_AppliesBoth_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND (isDepartmentHead AND (isManager OR isDepartmentHead))");

        string text = rule.PrintRuleText(GroupingStyle.DepthCycling, OperatorStyle.Symbolic);

        Assert.Equal("isManager ∧ (isDepartmentHead ∧ [isManager ∨ isDepartmentHead])", text);
    }

    /// <summary><see cref="OperatorStyle.CStyle"/> has no round-trippable rule text, so <see cref="CompiledRule{TContext}.PrintRuleText"/> rejects it.</summary>
    [Fact]
    public void PrintRuleText_CStyle_ThrowsArgumentException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND isDepartmentHead");

        Assert.Throws<ArgumentException>(() => rule.PrintRuleText(operatorStyle: OperatorStyle.CStyle));
    }

    /// <summary><see cref="CompiledRule{TContext}.TryPrintRuleText(GroupingStyle, OperatorStyle, out string)"/> reports <see cref="OperatorStyle.CStyle"/> as a failure instead of throwing.</summary>
    [Fact]
    public void TryPrintRuleText_CStyle_ReturnsFalseWithNoText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND isDepartmentHead");

        bool ok = rule.TryPrintRuleText(GroupingStyle.Parentheses, OperatorStyle.CStyle, out string? text);

        Assert.False(ok);
        Assert.Null(text);
    }

    /// <summary><see cref="CompiledRule{TContext}.TryPrintRuleText(GroupingStyle, OperatorStyle, out string)"/> succeeds for <see cref="OperatorStyle.Word"/> and <see cref="OperatorStyle.Symbolic"/>.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "isManager AND isDepartmentHead")]
    [InlineData(OperatorStyle.Symbolic, "isManager ∧ isDepartmentHead")]
    public void TryPrintRuleText_WordOrSymbolic_ReturnsTrueWithText_Test(OperatorStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile("isManager AND isDepartmentHead");

        bool ok = rule.TryPrintRuleText(GroupingStyle.Parentheses, style, out string? text);

        Assert.True(ok);
        Assert.Equal(expected, text);
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        return CreateCompiler().Compile(dsl).CompiledRule!;
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
