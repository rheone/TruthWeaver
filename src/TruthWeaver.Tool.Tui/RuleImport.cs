namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>Makes a document from text that did not come from a file the workbench opened: the clipboard or standard input.</summary>
internal static class RuleImport
{
    /// <summary>The name of the clipboard as an origin in messages.</summary>
    public const string ClipboardOrigin = "<clipboard>";

    /// <summary>Makes a document from text. The format comes from the override, or from the content.</summary>
    /// <param name="text">The text.</param>
    /// <param name="origin">The name of the source in messages.</param>
    /// <param name="format">The format the author names, or <see langword="null"/> to detect it.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="requireValid"><see langword="true"/> to refuse text that does not compile, so only a valid expression gets in.</param>
    /// <returns>The document and how its format was found, or the diagnostics.</returns>
    public static ToolResult<ImportedRule> FromText(
        string text,
        string origin,
        RuleFormat? format,
        ToolSettings settings,
        bool requireValid
    )
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(settings);

        ToolResult<LoadedRule> loaded = RuleLoader.LoadString(text, origin, new RuleLoadOptions { Format = format });
        if (!loaded.TryGetValue(out LoadedRule? rule, out EquatableArray<Diagnostic> diagnostics))
        {
            return Results.Failure<ImportedRule>(diagnostics);
        }

        RuleDocument document = RuleDocument.Create(rule.Text, rule.Format, settings);
        if (requireValid && !document.IsRuleCurrent)
        {
            return Results.Failure<ImportedRule>(document.Diagnostics);
        }

        return Results.Success(new ImportedRule(document, rule.Detection), document.Diagnostics);
    }
}
