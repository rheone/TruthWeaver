namespace TruthWeaver.Tool.Core.Loading;

using System.Text.Json;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;

/// <summary>
/// Finds the format of an input: rule text, JSON or YAML. An override wins, then the file extension, then the content.
/// </summary>
/// <remarks>
/// <para>The extensions are <c>.json</c> (JSON), <c>.yaml</c> and <c>.yml</c> (YAML), and <c>.rule</c>, <c>.tw</c> and
/// <c>.txt</c> (rule text). Case is ignored. Any other extension, or none, leaves the decision to the content.</para>
/// <para>The content rules, in order: text that parses as JSON, or that starts with <c>{</c> followed by <c>"</c>, is JSON.
/// Text whose first line that is not blank and not a <c>#</c> comment starts with <c>---</c>, <c>%YAML</c>, a <c>- </c>
/// sequence item or a <c>key:</c> mapping entry is YAML. Anything else is rule text. Rule text never has a <c>#</c> comment,
/// a leading <c>-</c> or a <c>name:</c> at the start of a line outside parentheses, so these rules do not take a rule for a
/// document.</para>
/// </remarks>
public static partial class FormatDetector
{
    /// <summary>Finds the format of a rule input.</summary>
    /// <param name="text">The decoded text.</param>
    /// <param name="fileName">The file name or path, or <see langword="null"/> when there is none (standard input, a string).</param>
    /// <param name="formatOverride">The format the caller names, or <see langword="null"/> to detect it.</param>
    /// <returns>The format and what decided it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static FormatDetection Detect(string text, string? fileName = null, RuleFormat? formatOverride = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (formatOverride is { } format)
        {
            return new FormatDetection(format, FormatEvidence.Override);
        }

        if (FromExtension(fileName) is { } byExtension)
        {
            return new FormatDetection(byExtension, FormatEvidence.Extension);
        }

        return new FormatDetection(FromContent(text), FormatEvidence.Content);
    }

    /// <summary>
    /// Finds the format of a data document (a stub-value file or a data file), which is JSON or YAML, never rule text. An
    /// override or extension that names rule text counts as no evidence. With no evidence, text that parses as JSON is JSON
    /// and anything else is YAML.
    /// </summary>
    /// <param name="text">The decoded text.</param>
    /// <param name="fileName">The file name or path, or <see langword="null"/>.</param>
    /// <param name="formatOverride">The format the caller names, or <see langword="null"/>.</param>
    /// <returns><see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</returns>
    internal static RuleFormat DetectDocument(string text, string? fileName, RuleFormat? formatOverride)
    {
        if (formatOverride is RuleFormat.Json or RuleFormat.Yaml)
        {
            return formatOverride.Value;
        }

        if (FromExtension(fileName) is RuleFormat.Json or RuleFormat.Yaml)
        {
            return FromExtension(fileName)!.Value;
        }

        return IsJson(text) ? RuleFormat.Json : RuleFormat.Yaml;
    }

    private static RuleFormat? FromExtension(string? fileName)
    {
        string extension = Path.GetExtension(fileName ?? string.Empty).ToUpperInvariant();
        return extension switch
        {
            ".JSON" => RuleFormat.Json,
            ".YAML" or ".YML" => RuleFormat.Yaml,
            ".RULE" or ".TW" or ".TXT" => RuleFormat.Text,
            _ => null,
        };
    }

    private static RuleFormat FromContent(string text)
    {
        string trimmed = text.TrimStart();
        if (IsJson(trimmed) || (trimmed.StartsWith('{') && trimmed[1..].TrimStart().StartsWith('"')))
        {
            // The second test keeps a malformed JSON document a JSON document, so the JSON reader reports the problem.
            return RuleFormat.Json;
        }

        foreach (string line in text.Split('\n'))
        {
            string content = line.Trim();
            if (content.Length == 0 || content.StartsWith('#'))
            {
                continue;
            }

            return YamlStart().IsMatch(content) ? RuleFormat.Yaml : RuleFormat.Text;
        }

        return RuleFormat.Text;
    }

    private static bool IsJson(string text)
    {
        string trimmed = text.TrimStart();
        if (!(trimmed.StartsWith('{') || trimmed.StartsWith('[')))
        {
            // A JSON scalar ("x", 1, true) parses, but it is never a rule or a data document worth detecting as JSON.
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // A document marker, a directive, a sequence item, or a mapping key (plain or quoted) followed by a colon and a space or the end.
    [GeneratedRegex("""^(---|%YAML|-(\s|$)|("[^"]*"|'[^']*'|[A-Za-z_$][\w$.\-]*)\s*:(\s|$))""", RegexOptions.CultureInvariant)]
    private static partial Regex YamlStart();
}
