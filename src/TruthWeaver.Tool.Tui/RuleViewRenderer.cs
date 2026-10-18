namespace TruthWeaver.Tool.Tui;

using System.Globalization;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Yaml;

/// <summary>
/// Prints a rule in the form and with the options of a <see cref="ViewSettings"/>. Every view calls the library printer
/// (through its <see cref="TextWriter"/> overload) with the options the settings name, so the text equals the text of the
/// library printer for the same options. A view that the printer refuses shows the reason instead of text.
/// </summary>
internal static class RuleViewRenderer
{
    /// <summary>The notice above the view while the editor text has errors.</summary>
    public const string StaleNotice = "Last valid tree (the rule text has errors):";

    /// <summary>The reason when no text of the document compiled yet.</summary>
    public const string NoTreeReason = "the rule text has errors and no tree compiled yet.";

    /// <summary>Prints the rule of a document in the form of the settings.</summary>
    /// <param name="document">The document. Its last valid tree is printed.</param>
    /// <param name="settings">The view settings.</param>
    /// <param name="evaluation">The current stub evaluation, or <see langword="null"/> when there is none.</param>
    /// <returns>The text or the reason.</returns>
    public static ViewOutput Render(RuleDocument document, ViewSettings settings, StubEvaluation? evaluation = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        if (document.Rule is not { } rule)
        {
            return new ViewOutput(null, NoTreeReason);
        }

        ViewOutput output = Render(rule, settings, evaluation);
        return document.IsRuleCurrent ? output : output with { Notice = StaleNotice };
    }

    /// <summary>Prints a compiled rule in the form of the settings.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="settings">The view settings.</param>
    /// <param name="evaluation">The current stub evaluation, or <see langword="null"/> when there is none.</param>
    /// <returns>The text or the reason.</returns>
    public static ViewOutput Render(CompiledRule<StubAssignment> rule, ViewSettings settings, StubEvaluation? evaluation = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            // A decision only fits the views that mark nodes, and it needs the trace of the evaluation.
            Decision? decision =
                settings.ShowDecision && evaluation?.Decision.TraceTree is not null ? evaluation.Decision : null;
            CompiledRule<StubAssignment> shown = decision is null ? rule : evaluation!.Rule;
            ConstantFormat? constants = ConstantsOf(settings);
            OperatorStyle style = settings.OperatorStyle ?? OperatorStyle.Word;
            using StringWriter writer = new(CultureInfo.InvariantCulture);
            switch (settings.Kind)
            {
                case ViewKind.Tree when decision is not null:
                    shown.PrintPlainText(writer, decision, constants: constants);
                    break;
                case ViewKind.Tree:
                    shown.PrintPlainText(writer, constants: constants);
                    break;
                case ViewKind.RuleText:
                    shown.PrintRuleText(writer, GroupingStyle.Parentheses, style, constants);
                    break;
                case ViewKind.Equation:
                    shown.PrintEquation(writer, EquationOf(settings));
                    break;
                case ViewKind.Mermaid:
                    MermaidOptions mermaid = MermaidOptions.Default with
                    {
                        OperatorStyle = style,
                        Constants = constants ?? ConstantFormat.Default,
                    };
                    if (decision is null)
                    {
                        shown.PrintMermaid(writer, mermaid);
                    }
                    else
                    {
                        shown.PrintMermaid(writer, decision, mermaid);
                    }

                    break;
                case ViewKind.Dot:
                    DotOptions dot = DotOptions.Default with
                    {
                        OperatorStyle = style,
                        Constants = constants ?? ConstantFormat.Default,
                    };
                    if (decision is null)
                    {
                        shown.PrintDot(writer, dot);
                    }
                    else
                    {
                        shown.PrintDot(writer, decision, dot);
                    }

                    break;
                case ViewKind.Html:
                    HtmlOptions html = HtmlOptions.Default with
                    {
                        OperatorStyle = style,
                        SimpleVariables = settings.SimpleVariables,
                        Constants = constants ?? ConstantFormat.Default,
                    };
                    if (decision is null)
                    {
                        shown.PrintHtml(writer, html);
                    }
                    else
                    {
                        shown.PrintHtml(writer, decision, html);
                    }

                    break;
                case ViewKind.Json:
                    shown.PrintJson(writer);
                    break;
                case ViewKind.Yaml:
                    shown.PrintYaml(writer);
                    break;
                default:
                    return new ViewOutput(null, $"{settings.Kind} is not a view.");
            }

            return new ViewOutput(writer.ToString().ReplaceLineEndings("\n"), null);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            // The printers refuse an option they cannot write, such as rule text in the C-style operators or with the
            // logic symbols. The pane shows the printer's own words.
            return new ViewOutput(null, ex.Message);
        }
    }

    /// <summary>Gets the equation options that the settings name.</summary>
    /// <param name="settings">The view settings.</param>
    /// <returns>The options.</returns>
    public static EquationOptions EquationOf(ViewSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        EquationOptions options = EquationOptions.Default with
        {
            Dialect = settings.Dialect,
            LatexWrap = settings.Dialect == EquationDialect.LaTeX ? settings.LatexWrap : LatexWrapMode.None,
            SimpleVariables = settings.SimpleVariables,
        };
        return ConstantsOf(settings) is { } constants ? options with { Constants = constants } : options;
    }

    /// <summary>Gets the constant format that the settings name, or <see langword="null"/> for the printer default.</summary>
    /// <param name="settings">The view settings.</param>
    /// <returns>The format, or <see langword="null"/>.</returns>
    public static ConstantFormat? ConstantsOf(ViewSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.HasConstantFormat)
        {
            return null;
        }

        // An equation spells constants as letters by default and the other views as words. Keep each default when only an
        // override is set.
        ConstantStyle style =
            settings.ConstantStyle ?? (settings.Kind == ViewKind.Equation ? ConstantStyle.Letters : ConstantStyle.Word);
        return new ConstantFormat
        {
            Style = style,
            TrueText = settings.TrueText,
            FalseText = settings.FalseText,
            UnknownText = settings.UnknownText,
        };
    }
}
