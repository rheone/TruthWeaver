namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 07: <see cref="ConstantStyle"/> and the override strings of <see cref="ConstantFormat"/>, in rule text,
/// equations (every dialect), plain-text trees and Mermaid diagrams.
/// </summary>
public sealed class ConstantStyleTests
{
    private const string AllConstants = "True AND False AND Unknown";

    /// <summary>The default format is the word style with no overrides, so output does not change.</summary>
    [Fact]
    public void ConstantFormat_Default_IsWordStyleWithNoOverrides_Test()
    {
        Assert.Equal(ConstantStyle.Word, ConstantFormat.Default.Style);
        Assert.Null(ConstantFormat.Default.TrueText);
        Assert.Null(ConstantFormat.Default.FalseText);
        Assert.Null(ConstantFormat.Default.UnknownText);
    }

    /// <summary>An empty override, or one with a line break, is rejected when it is set.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public void ConstantFormat_InvalidOverride_Throws_Test(string text)
    {
        Assert.Throws<ArgumentException>(() => new ConstantFormat { TrueText = text });
        Assert.Throws<ArgumentException>(() => new ConstantFormat { FalseText = text });
        Assert.Throws<ArgumentException>(() => new ConstantFormat { UnknownText = text });
    }

    /// <summary>Each style writes its own text for the three constants in the Unicode equation dialect.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, "True ∧ False ∧ Unknown")]
    [InlineData(ConstantStyle.Letters, "T ∧ F ∧ U")]
    [InlineData(ConstantStyle.LogicSymbols, "⊤ ∧ ⊥ ∧ U")]
    public void PrintEquation_UnicodeStyle_WritesTheStyleText_Test(ConstantStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string equation = rule.PrintEquation(new EquationOptions { Constants = new ConstantFormat { Style = style } });

        Assert.Equal(expected, equation);
    }

    /// <summary>With no constant format set, the equation keeps the word style.</summary>
    [Fact]
    public void PrintEquation_DefaultOptions_KeepsTheWordStyle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        Assert.Equal("True ∧ False ∧ Unknown", rule.PrintEquation());
    }

    /// <summary>The C-style dialect writes the style text as plain text.</summary>
    [Theory]
    [InlineData(ConstantStyle.Letters, "T && F && U")]
    [InlineData(ConstantStyle.LogicSymbols, "⊤ && ⊥ && U")]
    public void PrintEquation_CStyleStyle_WritesThePlainStyleText_Test(ConstantStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string equation = rule.PrintEquation(
            new EquationOptions
            {
                Dialect = EquationDialect.CStyle,
                Constants = new ConstantFormat { Style = style },
            }
        );

        Assert.Equal(expected, equation);
    }

    /// <summary>The LaTeX dialect writes a letter as a text group and the logic symbols as the commands <c>\top</c> and <c>\bot</c>.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, @"\text{True} \land \text{False} \land \text{Unknown}")]
    [InlineData(ConstantStyle.Letters, @"\text{T} \land \text{F} \land \text{U}")]
    [InlineData(ConstantStyle.LogicSymbols, @"\top \land \bot \land \text{U}")]
    public void PrintEquation_LatexStyle_WritesTextGroupsAndSymbolCommands_Test(ConstantStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string equation = rule.PrintEquation(
            new EquationOptions
            {
                Dialect = EquationDialect.LaTeX,
                Constants = new ConstantFormat { Style = style },
            }
        );

        Assert.Equal(expected, equation);
    }

    /// <summary>The AsciiMath dialect quotes a word or letter and writes the logic symbols as <c>top</c> and <c>bot</c>.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, "`\"True\" ^^ \"False\" ^^ \"Unknown\"`")]
    [InlineData(ConstantStyle.Letters, "`\"T\" ^^ \"F\" ^^ \"U\"`")]
    [InlineData(ConstantStyle.LogicSymbols, "`top ^^ bot ^^ \"U\"`")]
    public void PrintEquation_AsciiMathStyle_QuotesTextAndUsesSymbolKeywords_Test(ConstantStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string equation = rule.PrintEquation(
            new EquationOptions
            {
                Dialect = EquationDialect.AsciiMath,
                Constants = new ConstantFormat { Style = style },
            }
        );

        Assert.Equal(expected, equation);
    }

    /// <summary>An override wins over the style for its own constant only, in the Unicode dialect.</summary>
    [Fact]
    public void PrintEquation_Override_WinsOverTheStyleForThatConstantOnly_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);
        ConstantFormat constants = new() { Style = ConstantStyle.Letters, UnknownText = "?" };

        string equation = rule.PrintEquation(new EquationOptions { Constants = constants });

        Assert.Equal("T ∧ F ∧ ?", equation);
    }

    /// <summary>An override is literal text: LaTeX escapes it, and it replaces a logic symbol command.</summary>
    [Fact]
    public void PrintEquation_LatexOverride_IsEscapedAsLiteralText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);
        ConstantFormat constants = new()
        {
            Style = ConstantStyle.LogicSymbols,
            TrueText = "yes_1",
            FalseText = "no",
        };

        string equation = rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX, Constants = constants });

        Assert.Equal(@"\text{yes\_1} \land \text{no} \land \text{U}", equation);
    }

    /// <summary>An AsciiMath override is quoted, and a straight quote inside it is replaced as in a term.</summary>
    [Fact]
    public void PrintEquation_AsciiMathOverride_IsQuotedAndEscaped_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);
        ConstantFormat constants = new() { TrueText = "say \"hi\"" };

        string equation = rule.PrintEquation(
            new EquationOptions { Dialect = EquationDialect.AsciiMath, Constants = constants }
        );

        Assert.Equal("`\"say ”hi”\" ^^ \"False\" ^^ \"Unknown\"`", equation);
    }

    /// <summary>The examples in <c>docs/equations.md</c> (the Constants section) print exactly the documented text.</summary>
    [Fact]
    public void PrintEquation_DocumentedConstantsExamples_PrintTheDocumentedText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("isDineIn AND (False OR Unknown)");
        ConstantFormat letters = new() { Style = ConstantStyle.Letters };
        ConstantFormat symbols = new() { Style = ConstantStyle.LogicSymbols };

        Assert.Equal("isDineIn ∧ (False ∨ Unknown)", rule.PrintEquation());
        Assert.Equal("isDineIn ∧ (F ∨ U)", rule.PrintEquation(new EquationOptions { Constants = letters }));
        Assert.Equal("isDineIn ∧ (⊥ ∨ U)", rule.PrintEquation(new EquationOptions { Constants = symbols }));
        Assert.Equal(
            @"\text{isDineIn} \land (\bot \lor \text{U})",
            rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX, Constants = symbols })
        );
        Assert.Equal(
            "`\"isDineIn\" ^^ (\"F\" vv \"U\")`",
            rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.AsciiMath, Constants = letters })
        );
    }

    /// <summary>The simple-variable mode with a legend writes constants in the chosen style too.</summary>
    [Fact]
    public void PrintEquationWithLegend_LettersStyle_WritesConstantsInTheStyle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        EquationWithLegend result = rule.PrintEquationWithLegend(
            new EquationOptions { Constants = new ConstantFormat { Style = ConstantStyle.Letters } }
        );

        Assert.Equal("p ∧ U", result.Equation);
    }

    /// <summary>Rule text accepts the word style, and an override that is a letter-case variant of the word.</summary>
    [Fact]
    public void PrintRuleText_WordStyleWithCaseVariantOverride_PrintsTheOverrideAndReparses_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);
        ConstantFormat constants = new() { TrueText = "TRUE", UnknownText = "unknown" };

        string text = rule.PrintRuleText(constants: constants);

        Assert.Equal("TRUE AND False AND unknown", text);
        Assert.Equal(rule.CanonicalText, Compile(text).CanonicalText);
    }

    /// <summary>Rule text rejects a style that the parser does not read back, with a message that names the style.</summary>
    [Theory]
    [InlineData(ConstantStyle.Letters)]
    [InlineData(ConstantStyle.LogicSymbols)]
    public void PrintRuleText_StyleTheParserDoesNotRead_ThrowsWithAClearMessage_Test(ConstantStyle style)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            rule.PrintRuleText(constants: new ConstantFormat { Style = style })
        );

        Assert.Contains(style.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains("TryPrintRuleText", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Rule text rejects an override that the parser would read as another constant or as a term.</summary>
    [Theory]
    [InlineData("T")]
    [InlineData("Unknown")]
    [InlineData("1")]
    public void PrintRuleText_OverrideThatBreaksRoundTrip_ThrowsWithAClearMessage_Test(string text)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            rule.PrintRuleText(constants: new ConstantFormat { TrueText = text })
        );

        Assert.Contains($"'{text}'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>The Try form reports a rejected format through its return value, and an accepted one through the text.</summary>
    [Fact]
    public void TryPrintRuleText_RejectedAndAcceptedFormats_ReportThroughTheReturnValue_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        bool letters = rule.TryPrintRuleText(
            GroupingStyle.Parentheses,
            OperatorStyle.Word,
            new ConstantFormat { Style = ConstantStyle.Letters },
            out string? lettersText
        );
        bool word = rule.TryPrintRuleText(
            GroupingStyle.Parentheses,
            OperatorStyle.Word,
            ConstantFormat.Default,
            out string? wordText
        );

        Assert.False(letters);
        Assert.Null(lettersText);
        Assert.True(word);
        Assert.Equal(rule.CanonicalText, wordText);
    }

    /// <summary>The plain-text tree writes each constant leaf in the chosen style.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, "├─ True\n├─ False\n└─ Unknown\n")]
    [InlineData(ConstantStyle.Letters, "├─ T\n├─ F\n└─ U\n")]
    [InlineData(ConstantStyle.LogicSymbols, "├─ ⊤\n├─ ⊥\n└─ U\n")]
    public void PrintPlainText_Style_WritesConstantLeavesInTheStyle_Test(ConstantStyle style, string expectedLeaves)
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string text = rule.PrintPlainText(constants: new ConstantFormat { Style = style });

        Assert.Equal("AND\n" + expectedLeaves, text);
    }

    /// <summary>A plain-text override wins over the style, and the result suffix of an evaluated node is unchanged.</summary>
    [Fact]
    public async Task PrintPlainText_OverrideWithDecision_WinsOverTheStyleAndKeepsTheResultSuffix_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = rule.PrintPlainText(
            decision,
            constants: new ConstantFormat { Style = ConstantStyle.Letters, UnknownText = "?" }
        );

        Assert.Contains("└─ ? [unknown]", text, StringComparison.Ordinal);
    }

    /// <summary>The Mermaid diagram writes each constant node label in the chosen style.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, "n1[\"True\"]", "n2[\"False\"]", "n3[\"Unknown\"]")]
    [InlineData(ConstantStyle.Letters, "n1[\"T\"]", "n2[\"F\"]", "n3[\"U\"]")]
    [InlineData(ConstantStyle.LogicSymbols, "n1[\"⊤\"]", "n2[\"⊥\"]", "n3[\"U\"]")]
    public void PrintMermaid_Style_WritesConstantNodeLabelsInTheStyle_Test(
        ConstantStyle style,
        string first,
        string second,
        string third
    )
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);

        string diagram = rule.PrintMermaid(
            new MermaidOptionsBuilder().WithConstants(new ConstantFormat { Style = style }).Build()
        );

        Assert.Contains(first, diagram, StringComparison.Ordinal);
        Assert.Contains(second, diagram, StringComparison.Ordinal);
        Assert.Contains(third, diagram, StringComparison.Ordinal);
    }

    /// <summary>A Mermaid override wins over the style, and a quote in it is escaped so it cannot end the label.</summary>
    [Fact]
    public void PrintMermaid_Override_WinsOverTheStyleAndIsEscaped_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(AllConstants);
        MermaidOptions options = new()
        {
            Constants = new ConstantFormat { Style = ConstantStyle.Letters, TrueText = "a\"b" },
        };

        string diagram = rule.PrintMermaid(options);

        Assert.Contains("n1[\"a#quot;b\"]", diagram, StringComparison.Ordinal);
        Assert.Contains("n2[\"F\"]", diagram, StringComparison.Ordinal);
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("isDineIn", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
