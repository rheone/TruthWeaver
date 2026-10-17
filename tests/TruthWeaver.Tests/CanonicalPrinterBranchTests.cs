namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CanonicalPrinter"/> paths that the rule-level printing tests do not reach: the options overload with no width,
/// a quantifier broken across lines, and the refusal of an operator style the DSL does not read.
/// </summary>
public sealed class CanonicalPrinterBranchTests
{
    /// <summary>The options overload with no width prints the same single line as the canonical text.</summary>
    [Fact]
    public void Print_OptionsWithoutAWidth_GivesTheCanonicalSingleLine_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR c)");

        string printed = CanonicalPrinter.Print(rule.Root, RuleTextOptions.Default);

        Assert.Equal(rule.CanonicalText, printed);
    }

    /// <summary>A quantifier that does not fit the width breaks into its head and its body, one per line, and the text reads back to the same rule.</summary>
    [Fact]
    public void Print_QuantifierWiderThanTheWidth_BreaksTheHeadAndTheBodyOntoLines_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("FORALL(x IN items, elementTrue AND elementTrue)");

        string printed = CanonicalPrinter.Print(rule.Root, RuleTextOptions.Default with { Width = 40 });

        Assert.Equal("FORALL(\n  x IN items,\n  elementTrue AND elementTrue\n)", printed.ReplaceLineEndings("\n"));
        Assert.Equal(rule.CanonicalText, Compile(printed).CanonicalText);
    }

    /// <summary>A broken quantifier whose body is itself too wide breaks the body again, one level deeper.</summary>
    [Fact]
    public void Print_QuantifierWithABodyWiderThanTheWidth_BreaksTheBodyAgain_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("EXISTS(x IN items, elementTrue AND (elementTrue OR elementTrue))");

        string printed = CanonicalPrinter.Print(rule.Root, RuleTextOptions.Default with { Width = 12 });

        Assert.StartsWith("EXISTS(\n  x IN items,\n  ", printed.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.True(printed.Split('\n').Length > 5);
        Assert.Equal(rule.CanonicalText, Compile(printed).CanonicalText);
    }

    /// <summary>The C-style operator style is not DSL text, so printing a negation in it is refused.</summary>
    [Fact]
    public void Print_NegationInCStyle_ThrowsArgumentOutOfRange_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CanonicalPrinter.Print(rule.Root, GroupingStyle.Parentheses, OperatorStyle.CStyle)
        );
    }

    /// <summary>The C-style operator style is not DSL text, so printing an infix connective in it is refused.</summary>
    [Fact]
    public void Print_InfixConnectiveInCStyle_ThrowsArgumentOutOfRange_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CanonicalPrinter.Print(rule.Root, GroupingStyle.Parentheses, OperatorStyle.CStyle)
        );
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddCollection(
                "items",
                _ => Array.Empty<TruthValue>(),
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<RuleTestContext>(builder.Build()).Compile(text).GetRuleOrThrow();
    }
}
