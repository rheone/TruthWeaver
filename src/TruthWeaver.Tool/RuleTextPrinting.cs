namespace TruthWeaver.Tool;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// Prints a loaded rule in a format with the settings of the tool: rule text in the operator style of the settings,
/// JSON and YAML as <see cref="RulePrinter.Print"/> prints them. The pretty form breaks rule text at 80 characters
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
        CompilationResult<StubAssignment> compiled = RuleConverter.Compile(rule, settings);
        return compiled.TryGetRule(out CompiledRule<StubAssignment>? value, out EquatableArray<Diagnostic> diagnostics)
            ? new ToolResult<string>(RulePrinter.Print(value, format, settings, pretty, includeNotes), diagnostics)
            : new ToolResult<string>(null, diagnostics);
    }
}
