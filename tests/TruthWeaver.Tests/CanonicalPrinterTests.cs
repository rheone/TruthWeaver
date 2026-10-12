namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 06: canonical DSL printer and round-trip.</summary>
public sealed class CanonicalPrinterTests
{
    /// <summary>The printer reproduces the ADR 0003 worked example verbatim.</summary>
    [Fact]
    public void Print_Adr0003WorkedExample_IsVerbatim_Test()
    {
        const string source =
            "hasRole(role: \"Y\") AND (hasTraining(training: \"Q\") OR hasTraining(training: \"Z\") OR (isManager XOR isDepartmentHead))";
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile(source).CompiledRule!;

        Assert.Equal(source, rule.CanonicalText);
    }

    /// <summary>An XOR operand is always parenthesized, even when precedence makes it unnecessary.</summary>
    [Fact]
    public void Print_XorOperand_IsAlwaysParenthesized_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager XOR isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager XOR isDepartmentHead)", rule.CanonicalText);
    }

    /// <summary>Printing Nand and Nor parenthesizes and uses the word operator name.</summary>
    [Theory]
    [InlineData("isManager ↑ isDepartmentHead", "(isManager NAND isDepartmentHead)")]
    [InlineData("isManager nor isDepartmentHead", "(isManager NOR isDepartmentHead)")]
    public void Print_NandAndNor_ParenthesizesAndUsesWordOperator_Test(string text, string expected)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile(text).CompiledRule!;

        Assert.Equal(expected, rule.CanonicalText);
    }

    /// <summary>Printing Implies parenthesizes and uses the word operator name.</summary>
    [Fact]
    public void Print_Implies_ParenthesizesAndUsesWordOperator_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager → isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager IMPLIES isDepartmentHead)", rule.CanonicalText);
    }

    [Fact]
    public void Print_EquivalentOperand_ParenthesizesEvenWhenPrecedenceDoesNotRequireIt_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager XNOR isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager EQUIVALENT isDepartmentHead)", rule.CanonicalText);
    }

    /// <summary>An AND operand of OR is parenthesized, although precedence makes it unambiguous.</summary>
    [Fact]
    public void Print_AndOperandOfOr_IsParenthesized_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler
            .Compile("isManager AND isDepartmentHead OR hasRole(role: \"Y\")")
            .CompiledRule!;

        Assert.Equal("(isManager AND isDepartmentHead) OR hasRole(role: \"Y\")", rule.CanonicalText);
    }

    /// <summary>Parsing then printing round trips to a structurally equal tree.</summary>
    [Theory]
    [InlineData("isManager AND isDepartmentHead")]
    [InlineData("isManager OR isDepartmentHead")]
    [InlineData("NOT isManager")]
    [InlineData("(isManager XOR isDepartmentHead)")]
    [InlineData("PARITY(isManager, isDepartmentHead, isManager)")]
    [InlineData("ANY(isManager, isDepartmentHead, isManager)")]
    [InlineData("ALL(isManager, isDepartmentHead, isManager)")]
    [InlineData("NONE(isManager, isDepartmentHead, isManager)")]
    [InlineData("BETWEEN(1, 2, isManager, isDepartmentHead, isManager)")]
    [InlineData("COALESCE(isManager, isDepartmentHead, isManager)")]
    [InlineData("If(isManager, isDepartmentHead, isManager)")]
    [InlineData("IsTrue(isManager)")]
    [InlineData("IsFalse(isManager AND isDepartmentHead)")]
    [InlineData("IsUnknown(isManager)")]
    [InlineData("IsKnown(NOT isManager)")]
    [InlineData("COALESCE(isManager, True)")]
    [InlineData("COALESCE(isManager AND isDepartmentHead, False)")]
    [InlineData("ExactlyOne(isManager, isDepartmentHead, isManager)")]
    [InlineData("AtLeast(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("AtMost(1, isManager, isDepartmentHead, isManager)")]
    [InlineData("GreaterThan(1, isManager, isDepartmentHead, isManager)")]
    [InlineData("LessThan(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("Exactly(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("(isManager EQUIVALENT isDepartmentHead)")]
    [InlineData("hasRole(role: \"Y\")")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("isManager AND (isDepartmentHead OR hasRole(role: \"Y\"))")]
    [InlineData("(isManager AND isDepartmentHead) OR hasRole(role: \"Y\")")]
    public void Print_ParseThenPrint_RoundTripsToEqualTree_Test(string source)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> original = compiler.Compile(source).CompiledRule!;
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A string argument with a quote prints the quote escaped.</summary>
    [Fact]
    public void Print_StringWithQuote_EscapesQuote_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("hasRole(role: \"a\\\"b\")").CompiledRule!;

        Assert.Equal("hasRole(role: \"a\\\"b\")", rule.CanonicalText);
    }

    /// <summary>A string argument with a backslash prints the backslash escaped.</summary>
    [Fact]
    public void Print_StringWithBackslash_EscapesBackslash_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("hasRole(role: \"a\\\\b\")").CompiledRule!;

        Assert.Equal("hasRole(role: \"a\\\\b\")", rule.CanonicalText);
    }

    /// <summary>A string argument with a quote and a backslash round trips.</summary>
    [Fact]
    public void Print_StringWithQuoteAndBackslash_RoundTrips_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> original = compiler.Compile("hasRole(role: \"a\\\"\\\\b\")").CompiledRule!;
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A DateTimeOffset argument prints as a quoted string and round trips.</summary>
    [Fact]
    public void Print_DateTimeOffsetArgument_IsQuotedAndRoundTrips_Test()
    {
        DateTimeOffset matchValue = new(2024, 3, 17, 9, 30, 0, TimeSpan.FromHours(-5));
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDateTimeOffsetArgPredicate("startedAt", "when", matchValue)
                .Build()
        );

        CompiledRule<RuleTestContext> original = compiler.Compile($"startedAt(when: \"{matchValue:O}\")").CompiledRule!;

        Assert.Equal($"startedAt(when: \"{matchValue:O}\")", original.CanonicalText);

        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>Printing the same tree twice gives byte identical text.</summary>
    [Fact]
    public void Print_SameTreeTwice_IsByteIdentical_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        Assert.Equal(rule.CanonicalText, rule.CanonicalText);
    }

    /// <summary>ToString returns the same text as the canonical text.</summary>
    [Fact]
    public void ToString_Default_MatchesCanonicalText_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        Assert.Equal(rule.CanonicalText, rule.ToString());
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddStringArgPredicate("hasTraining", "training", "Q")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
