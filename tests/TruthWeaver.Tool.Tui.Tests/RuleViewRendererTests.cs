namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core;
using TruthWeaver.Yaml;

/// <summary>The text of the view pane: it equals the library printer for the same options, or it gives the reason.</summary>
public sealed class RuleViewRendererTests
{
    private const string Source = "a AND (b OR NOT c)";

    /// <summary>Every form prints the same text as the library printer called with default options.</summary>
    [Fact]
    public void Render_DefaultSettings_EqualsTheLibraryPrinter_Test()
    {
        CompiledRule<StubAssignment> rule = Compile(Source);
        (ViewKind Kind, string Expected)[] expected =
        [
            (ViewKind.Tree, rule.PrintPlainText()),
            (ViewKind.RuleText, rule.PrintRuleText()),
            (ViewKind.Equation, rule.PrintEquation()),
            (ViewKind.Mermaid, rule.PrintMermaid(MermaidOptions.Default)),
            (ViewKind.Dot, rule.PrintDot()),
            (ViewKind.Html, rule.PrintHtml()),
            (ViewKind.Json, rule.PrintJson()),
            (ViewKind.Yaml, rule.PrintYaml()),
        ];

        foreach ((ViewKind kind, string text) in expected)
        {
            ViewOutput output = RuleViewRenderer.Render(rule, new ViewSettings { Kind = kind });

            Assert.True(output.Printed, $"{kind}: {output.Reason}");
            Assert.Equal(text.ReplaceLineEndings("\n"), output.Text);
        }
    }

    /// <summary>The equation view prints each dialect, with the LaTeX envelopes and the constant style, as the printer does.</summary>
    [Fact]
    public void Render_EveryEquationDialect_EqualsThePrinterWithTheSameOptions_Test()
    {
        CompiledRule<StubAssignment> rule = Compile("a AND (b OR True)");

        foreach (EquationDialect dialect in Enum.GetValues<EquationDialect>())
        {
            ViewSettings settings = new()
            {
                Kind = ViewKind.Equation,
                Dialect = dialect,
                ConstantStyle = ConstantStyle.LogicSymbols,
                SimpleVariables = true,
            };

            ViewOutput output = RuleViewRenderer.Render(rule, settings);

            string expected = rule.PrintEquation(
                new EquationOptions
                {
                    Dialect = dialect,
                    SimpleVariables = true,
                    Constants = new ConstantFormat { Style = ConstantStyle.LogicSymbols },
                }
            );
            Assert.Equal(expected.ReplaceLineEndings("\n"), output.Text);
        }
    }

    /// <summary>The LaTeX dialect prints each envelope, and another dialect ignores the envelope setting.</summary>
    [Fact]
    public void Render_LatexEnvelope_EqualsThePrinterWithTheSameEnvelope_Test()
    {
        CompiledRule<StubAssignment> rule = Compile(Source);

        foreach (LatexWrapMode wrap in Enum.GetValues<LatexWrapMode>())
        {
            ViewOutput output = RuleViewRenderer.Render(
                rule,
                new ViewSettings
                {
                    Kind = ViewKind.Equation,
                    Dialect = EquationDialect.LaTeX,
                    LatexWrap = wrap,
                }
            );

            Assert.Equal(
                rule.PrintEquation(new EquationOptions { Dialect = EquationDialect.LaTeX, LatexWrap = wrap })
                    .ReplaceLineEndings("\n"),
                output.Text
            );
        }

        ViewOutput unicode = RuleViewRenderer.Render(
            rule,
            new ViewSettings { Kind = ViewKind.Equation, LatexWrap = LatexWrapMode.DoubleDollar }
        );
        Assert.Equal(rule.PrintEquation().ReplaceLineEndings("\n"), unicode.Text);
    }

    /// <summary>The operator style, the constant style and the overrides reach the Mermaid, DOT and HTML printers.</summary>
    [Fact]
    public void Render_OperatorAndConstantToggles_EqualThePrintersWithTheSameOptions_Test()
    {
        CompiledRule<StubAssignment> rule = Compile("a AND (b OR True)");
        ConstantFormat constants = new() { Style = ConstantStyle.Letters, TrueText = "YES" };
        ViewSettings settings = new()
        {
            OperatorStyle = OperatorStyle.Symbolic,
            ConstantStyle = ConstantStyle.Letters,
            TrueText = "YES",
            SimpleVariables = true,
            Kind = ViewKind.Mermaid,
        };
        string mermaid = RuleViewRenderer.Render(rule, settings).Text!;
        settings.Kind = ViewKind.Dot;
        string dot = RuleViewRenderer.Render(rule, settings).Text!;
        settings.Kind = ViewKind.Html;
        string html = RuleViewRenderer.Render(rule, settings).Text!;

        Assert.Equal(
            rule.PrintMermaid(new MermaidOptions { OperatorStyle = OperatorStyle.Symbolic, Constants = constants })
                .ReplaceLineEndings("\n"),
            mermaid
        );
        Assert.Equal(
            rule.PrintDot(new DotOptions { OperatorStyle = OperatorStyle.Symbolic, Constants = constants })
                .ReplaceLineEndings("\n"),
            dot
        );
        Assert.Equal(
            rule.PrintHtml(
                    new HtmlOptions
                    {
                        OperatorStyle = OperatorStyle.Symbolic,
                        SimpleVariables = true,
                        Constants = constants,
                    }
                )
                .ReplaceLineEndings("\n"),
            html
        );
    }

    /// <summary>Rule text in the C-style operators cannot round-trip, so the view gives the reason, not text.</summary>
    [Fact]
    public void Render_RuleTextWithCStyleOperators_GivesTheReason_Test()
    {
        ViewOutput output = RuleViewRenderer.Render(
            Compile(Source),
            new ViewSettings { Kind = ViewKind.RuleText, OperatorStyle = OperatorStyle.CStyle }
        );

        Assert.False(output.Printed);
        Assert.Contains("CStyle", output.Reason, StringComparison.Ordinal);
        Assert.StartsWith("Cannot show this view: ", output.Display, StringComparison.Ordinal);
    }

    /// <summary>A document with no compiled tree has nothing to show, and says why.</summary>
    [Fact]
    public void Render_DocumentWithNoTree_GivesTheReason_Test()
    {
        ViewOutput output = RuleViewRenderer.Render(RuleDocument.Create("AND AND"), new ViewSettings());

        Assert.False(output.Printed);
        Assert.Equal(RuleViewRenderer.NoTreeReason, output.Reason);
    }

    /// <summary>While the text has an error, the view prints the last valid tree under a notice.</summary>
    [Fact]
    public void Render_DocumentWithAnErrorAfterAValidTree_ShowsTheLastTreeUnderTheNotice_Test()
    {
        RuleDocument document = RuleDocument.Create("a AND b");
        document.SetText("a AND");

        ViewOutput output = RuleViewRenderer.Render(document, new ViewSettings());

        Assert.Equal(RuleViewRenderer.StaleNotice, output.Notice);
        Assert.Equal(Compile("a AND b").PrintPlainText().ReplaceLineEndings("\n"), output.Text);
    }

    /// <summary>With an evaluation, the tree, Mermaid, DOT and HTML views mark the decision as the printers do.</summary>
    [Fact]
    public async Task Render_WithAnEvaluation_EqualsThePrintersThatTakeADecision_Test()
    {
        LoadedRule loaded = RuleLoader.LoadString(Source).Value!;
        StubEvaluation evaluation = (
            await StubEvaluator.EvaluateAsync(
                loaded,
                new StubEvaluationOptions { Evaluation = EvaluationOptions.Default with { IncludeTrace = true } },
                TestContext.Current.CancellationToken
            )
        ).Value!;
        CompiledRule<StubAssignment> rule = Compile(Source);
        Decision decision = evaluation.Decision;

        string tree = RuleViewRenderer.Render(rule, new ViewSettings { Kind = ViewKind.Tree }, evaluation).Text!;
        string mermaid = RuleViewRenderer.Render(rule, new ViewSettings { Kind = ViewKind.Mermaid }, evaluation).Text!;
        string dot = RuleViewRenderer.Render(rule, new ViewSettings { Kind = ViewKind.Dot }, evaluation).Text!;
        string html = RuleViewRenderer.Render(rule, new ViewSettings { Kind = ViewKind.Html }, evaluation).Text!;
        string hidden = RuleViewRenderer
            .Render(rule, new ViewSettings { Kind = ViewKind.Tree, ShowDecision = false }, evaluation)
            .Text!;

        Assert.Equal(evaluation.Rule.PrintPlainText(decision).ReplaceLineEndings("\n"), tree);
        Assert.Equal(evaluation.Rule.PrintMermaid(decision, MermaidOptions.Default).ReplaceLineEndings("\n"), mermaid);
        Assert.Equal(evaluation.Rule.PrintDot(decision).ReplaceLineEndings("\n"), dot);
        Assert.Equal(evaluation.Rule.PrintHtml(decision).ReplaceLineEndings("\n"), html);
        Assert.Equal(rule.PrintPlainText().ReplaceLineEndings("\n"), hidden);
        Assert.NotEqual(hidden, tree);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        return RuleDocument.Create(text).Rule!;
    }
}
