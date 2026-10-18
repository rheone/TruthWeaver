namespace TruthWeaver.Tool;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// Prints a loaded rule in a format with the settings of the tool: rule text in the operator style of the settings,
/// JSON and YAML as <see cref="RuleConverter.Convert"/> prints them. The pretty form breaks rule text at 80 characters
/// and indents JSON by two spaces; YAML is always one node per line.
/// </summary>
internal static class RuleTextPrinting
{
    /// <summary>Prints a rule in a format.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="format">The format to print.</param>
    /// <param name="settings">The settings: the limits and the operator style.</param>
    /// <param name="includeNotes"><see langword="true"/> to keep the notes.</param>
    /// <param name="pretty"><see langword="true"/> for the pretty form; <see langword="false"/> for the canonical form (one line for rule text and JSON).</param>
    /// <returns>The text, or no text and the compile diagnostics.</returns>
    public static ToolResult<string> Print(
        LoadedRule rule,
        RuleFormat format,
        ToolSettings settings,
        bool includeNotes = true,
        bool pretty = false
    )
    {
        if (format != RuleFormat.Text)
        {
            ToolResult<string> converted = RuleConverter.Convert(rule, format, settings, includeNotes);
            return pretty && format == RuleFormat.Json && converted.Value is { } json
                ? converted with
                {
                    Value = CommandOutcome.FormatJson(CommandOutcome.ParseJson(json)),
                }
                : converted;
        }

        CompilationResult<StubAssignment> compiled = RuleConverter.Compile(rule, settings);
        if (!compiled.TryGetRule(out CompiledRule<StubAssignment>? value, out EquatableArray<Diagnostic> diagnostics))
        {
            return new ToolResult<string>(null, diagnostics);
        }

        RuleTextOptions options = new()
        {
            OperatorStyle = settings.OperatorStyle,
            IncludeNotes = includeNotes,
            Width = pretty ? ArtifactCatalog.PrettyWidth : null,
        };
        return new ToolResult<string>(value.PrintRuleText(options), diagnostics);
    }
}
