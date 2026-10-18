namespace TruthWeaver.Tool.Core.Patching;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;

/// <summary>
/// Writes the small pieces of text that <see cref="RulePatcher"/> puts into a document: scalars in the style the document
/// used, a rule in JSON flow form, and lines indented to fit their place.
/// </summary>
internal static class PatchText
{
    // Keeps non-ASCII text as it is; quotes, backslashes and control characters are still escaped.
    private static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    // Plain YAML scalars that the YAML rule reader (or a YAML 1.1 reader) types as something other than a string.
    private static readonly string[] ReservedPlainWords = ["true", "false", "null", "yes", "no", "on", "off", "y", "n"];

    /// <summary>Gets the line break the document uses: CRLF when it has one, otherwise LF.</summary>
    public static string NewLineOf(string document)
    {
        return document.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

    /// <summary>Writes a string as a JSON string, which is also a YAML double-quoted scalar.</summary>
    public static string Quote(string value)
    {
        return "\"" + JsonEncodedText.Encode(value, Encoder).ToString() + "\"";
    }

    /// <summary>Writes a JSON scalar value as JSON text.</summary>
    public static string JsonScalar(JsonNode? value)
    {
        return value is JsonValue text && text.GetValueKind() == JsonValueKind.String
            ? Quote(text.GetValue<string>())
            : value?.ToJsonString() ?? "null";
    }

    /// <summary>
    /// Writes a JSON scalar value as a YAML scalar. A string keeps the style the document used when the new value can be
    /// written in it with the same meaning; otherwise it is double-quoted. Numbers, Booleans and null are plain.
    /// </summary>
    public static string YamlScalar(JsonNode? value, ScalarStyle style)
    {
        return value is JsonValue text && text.GetValueKind() == JsonValueKind.String
            ? YamlString(text.GetValue<string>(), style)
            : value?.ToJsonString() ?? "null";
    }

    /// <summary>Writes a string as a YAML scalar in <paramref name="style"/> when it can be, otherwise double-quoted.</summary>
    public static string YamlString(string value, ScalarStyle style)
    {
        if (style == ScalarStyle.Plain && IsPlainSafe(value))
        {
            return value;
        }

        return style == ScalarStyle.SingleQuoted && !value.Any(char.IsControl)
            ? "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'"
            : Quote(value);
    }

    /// <summary>
    /// Writes a JSON value on one line, with a space after each colon and comma. The text is valid JSON and a valid YAML
    /// flow node, so it fits a JSON document and a YAML flow mapping alike.
    /// </summary>
    public static string Flow(JsonNode? node)
    {
        return node switch
        {
            JsonObject obj => "{"
                + string.Join(", ", obj.Select(member => Quote(member.Key) + ": " + Flow(member.Value)))
                + "}",
            JsonArray array => "[" + string.Join(", ", array.Select(Flow)) + "]",
            _ => JsonScalar(node),
        };
    }

    /// <summary>Writes a JSON value indented, with LF line breaks.</summary>
    public static string Indented(JsonNode node, int indentSize)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            IndentSize = indentSize,
            NewLine = "\n",
            Encoder = Encoder,
        };
        return node.ToJsonString(options);
    }

    /// <summary>
    /// Joins lines (separated by LF) with the document's line break, and puts <paramref name="indent"/> before every line
    /// but the first, which continues at the place it is written. Empty lines stay empty.
    /// </summary>
    public static string Reindent(string text, string indent, string newLine)
    {
        string[] lines = text.Split('\n');
        return string.Join(newLine, lines.Select((line, i) => i == 0 || line.Length == 0 ? line : indent + line));
    }

    /// <summary>Gets the offset of the start of the line that holds <paramref name="offset"/>.</summary>
    public static int LineStart(string document, int offset)
    {
        return offset == 0 ? 0 : document.LastIndexOf('\n', offset - 1) + 1;
    }

    /// <summary>Gets the spaces and tabs at the start of the line that holds <paramref name="offset"/>.</summary>
    public static string LineIndent(string document, int offset)
    {
        int start = LineStart(document, offset);
        int end = start;
        while (end < document.Length && document[end] is ' ' or '\t')
        {
            end++;
        }

        return document[start..end];
    }

    /// <summary>Replaces spans of a document, given in any order, that do not overlap.</summary>
    public static string Replace(string document, IEnumerable<(int Start, int End, string Text)> replacements)
    {
        StringBuilder builder = new(document);
        foreach ((int start, int end, string text) in replacements.OrderByDescending(replacement => replacement.Start))
        {
            builder.Remove(start, end - start).Insert(start, text);
        }

        return builder.ToString();
    }

    // A plain scalar that reads back as the same string anywhere, in block and flow context: it starts with a letter or
    // '_', holds only letters, digits, spaces and a few punctuation marks (no ':', '#', ',', brackets or quotes that
    // start), does not end with a space, and is not a word that a YAML reader types as a Boolean or null.
    private static bool IsPlainSafe(string value)
    {
        return value.Length > 0
            && (char.IsAsciiLetter(value[0]) || value[0] == '_')
            && value[^1] != ' '
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c is ' ' or '_' or '.' or '-' or '(' or ')' or '/' or '!' or '?')
            && !ReservedPlainWords.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
