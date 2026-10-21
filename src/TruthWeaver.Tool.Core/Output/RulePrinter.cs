namespace TruthWeaver.Tool.Core.Output;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Yaml;

/// <summary>
/// The one place that decides how a rule is written back to text in a format. It holds the print policy: rule text keeps
/// its notes, uses the operator style of the settings and breaks at <see cref="PrettyWidth"/> in the pretty form; JSON is
/// indented by two spaces in the pretty form; YAML always has line feeds, because the library YAML printer writes the
/// line ending of the platform.
/// </summary>
public static class RulePrinter
{
    /// <summary>The line width of the pretty form of rule text.</summary>
    public const int PrettyWidth = 80;

    /// <summary>Prints a compiled rule in a format.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="format">The format to print.</param>
    /// <param name="settings">The settings. The operator style applies to rule text.</param>
    /// <param name="pretty">
    /// <see langword="true"/> for the pretty form: rule text broken at <see cref="PrettyWidth"/> characters and JSON
    /// indented by two spaces. YAML has one form and ignores it.
    /// </param>
    /// <returns>The text of the rule. Rule text keeps the notes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string Print(CompiledRule<StubAssignment> rule, RuleFormat format, ToolSettings settings, bool pretty)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(settings);

        return format switch
        {
            RuleFormat.Text => rule.PrintRuleText(
                new RuleTextOptions
                {
                    OperatorStyle = settings.OperatorStyle,
                    IncludeNotes = true,
                    Width = pretty ? PrettyWidth : null,
                }
            ),
            RuleFormat.Json when pretty => IndentJson(rule.PrintAs(RuleFormat.Json)),

            // The YAML printer may write CRLF on Windows; printed text keeps line feeds, as the output writer does.
            RuleFormat.Yaml => rule.PrintAs(RuleFormat.Yaml).ReplaceLineEndings("\n"),
            _ => rule.PrintAs(format),
        };
    }

    /// <summary>
    /// Prints a rule document that has <c>let</c> definitions, given as the JSON tree of
    /// <see cref="RuleDefinitions.PrintJson"/>, in a format and keeps its definitions and uses.
    /// </summary>
    /// <param name="document">The document in the JSON tree format, with its definitions section.</param>
    /// <param name="format">The format to print.</param>
    /// <param name="settings">The settings: the operator style of rule text and the limits of the compiler.</param>
    /// <param name="pretty">The same meaning as for <see cref="Print"/>.</param>
    /// <returns>The text, or <see langword="null"/> when a part of the document does not compile on its own.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string? PrintDefinitions(JsonObject document, RuleFormat format, ToolSettings settings, bool pretty)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);

        return format switch
        {
            RuleFormat.Json => pretty ? IndentJson(document.ToJsonString()) : document.ToJsonString(),

            // The YAML printer may write CRLF on Windows; printed text keeps line feeds.
            RuleFormat.Yaml => RuleDefinitions.ReadJson(document.ToJsonString()).PrintYaml()?.ReplaceLineEndings("\n"),
            _ => DefinitionsPrinter.PrintText(document, settings, pretty ? PrettyWidth : null),
        };
    }

    /// <summary>Indents JSON by two spaces with line feeds, without escaping characters that JSON allows as they are.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The indented text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public static string IndentJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonNode node = JsonNode.Parse(json) ?? throw new InvalidOperationException("The printer wrote JSON null.");
        using MemoryStream stream = new();
        using (
            Utf8JsonWriter writer = new(
                stream,
                new JsonWriterOptions
                {
                    Indented = true,
                    NewLine = "\n",
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }
            )
        )
        {
            node.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
