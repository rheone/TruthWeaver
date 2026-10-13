namespace TruthWeaver.Tool.Core;

using System.Text.Encodings.Web;
using System.Text.Json;
using TruthWeaver.Diagnostics;

/// <summary>
/// Writes diagnostics as text, JSON or SARIF 2.1.0. The output is deterministic: the documents and their diagnostics stay
/// in the order given, every line ends with a line feed, and nothing depends on the time or the machine.
/// </summary>
/// <remarks>
/// <para>A location comes from the span of a diagnostic and the text of its <see cref="DiagnosticDocument"/>: a 1-based
/// line and column, and the end line and column after the last character. A diagnostic whose span is
/// <see cref="SourceSpan.None"/> has no position.</para>
/// <para>The SARIF rules are the rows of <see cref="DiagnosticCodeTable"/>, and each result names its row by
/// <c>ruleId</c> and <c>ruleIndex</c>. A result has a physical location (the file and a region) when the whole input is
/// the rule, and a logical location (the tree path) when the diagnostic has a path or the rule is embedded in the input.
/// The expected text, the found text and the suggestion of a diagnostic are in the <c>properties</c> of the result.</para>
/// </remarks>
public static class DiagnosticWriter
{
    /// <summary>The URI of the SARIF 2.1.0 JSON schema that the log names in <c>$schema</c>.</summary>
    public const string SarifSchema = "https://json.schemastore.org/sarif-2.1.0.json";

    private const string ToolName = "TruthWeaver";

    private const string ToolHomePage = "https://github.com/rheone/TruthWeaver";

    /// <summary>Writes diagnostics to a string.</summary>
    /// <param name="documents">The documents and their diagnostics.</param>
    /// <param name="format">The output format.</param>
    /// <returns>The output, with line feeds and no final line feed. Write it with <see cref="OutputWriter"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined value.</exception>
    public static string Format(IEnumerable<DiagnosticDocument> documents, DiagnosticOutputFormat format)
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
        Write(writer, documents, format);
        return writer.ToString();
    }

    /// <summary>Writes diagnostics to a writer.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="documents">The documents and their diagnostics.</param>
    /// <param name="format">The output format.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="documents"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined value.</exception>
    public static void Write(TextWriter writer, IEnumerable<DiagnosticDocument> documents, DiagnosticOutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documents);

        List<DiagnosticDocument> all = [.. documents];
        switch (format)
        {
            case DiagnosticOutputFormat.Text:
                writer.Write(FormatText(all));
                break;
            case DiagnosticOutputFormat.Json:
                writer.Write(WriteJson(json => WriteDiagnosticsJson(json, all)));
                break;
            case DiagnosticOutputFormat.Sarif:
                writer.Write(WriteJson(json => WriteSarif(json, all)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "The value is not a defined output format.");
        }
    }

    /// <summary>Gets the lower-case word for a severity: <c>error</c>, <c>warning</c> or <c>info</c>.</summary>
    /// <param name="severity">The severity.</param>
    /// <returns>The word.</returns>
    internal static string SeverityWord(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };
    }

    /// <summary>Writes a JSON document with two-space indents and line feeds, and keeps quotes and non-ASCII text readable.</summary>
    /// <param name="write">Writes the document.</param>
    /// <returns>The JSON text.</returns>
    internal static string WriteJson(Action<Utf8JsonWriter> write)
    {
        using MemoryStream stream = new();
        using (
            Utf8JsonWriter json = new(
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
            write(json);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Writes diagnostics as text: one block for each diagnostic, each block headed by its input.</summary>
    /// <param name="documents">The documents.</param>
    /// <returns>The text, with no final line feed.</returns>
    internal static string FormatText(IEnumerable<DiagnosticDocument> documents)
    {
        // Each block starts with the input, so the diagnostics of several inputs stay apart.
        return string.Join(
            "\n",
            documents.SelectMany(document =>
                document.Diagnostics.Select(d =>
                    Prefix(document) + ": " + DiagnosticFormatter.Format(d, document.Text).ReplaceLineEndings("\n")
                )
            )
        );
    }

    /// <summary>Writes one diagnostic as the JSON object of the JSON format.</summary>
    /// <param name="json">The writer.</param>
    /// <param name="document">The document of the diagnostic.</param>
    /// <param name="diagnostic">The diagnostic.</param>
    internal static void WriteDiagnosticObject(Utf8JsonWriter json, DiagnosticDocument document, Diagnostic diagnostic)
    {
        json.WriteStartObject();
        json.WriteString("origin", document.Origin);
        WriteIfPresent(json, "rulePath", document.RulePath);
        json.WriteString("code", diagnostic.Code);
        if (DiagnosticCodeTable.TryGet(diagnostic.Code, out DiagnosticCodeInfo? info))
        {
            json.WriteString("name", info.Name);
        }

        json.WriteString("severity", SeverityWord(diagnostic.Severity));
        json.WriteString("message", diagnostic.Message);
        WriteIfPresent(json, "path", diagnostic.Path);
        if (HasPosition(diagnostic))
        {
            json.WriteStartObject("span");
            json.WriteNumber("start", diagnostic.Span.Start);
            json.WriteNumber("length", diagnostic.Span.Length);
            json.WriteEndObject();
            if (document.Text is { } text)
            {
                (SourceLocation start, SourceLocation end) = Locate(diagnostic.Span, text);
                json.WriteNumber("line", start.Line);
                json.WriteNumber("column", start.Column);
                json.WriteNumber("endLine", end.Line);
                json.WriteNumber("endColumn", end.Column);
            }
        }

        WriteDetails(json, diagnostic);
        json.WriteEndObject();
    }

    private static string Prefix(DiagnosticDocument document)
    {
        return document.RulePath is null ? document.Origin : $"{document.Origin} {document.RulePath}";
    }

    private static void WriteDiagnosticsJson(Utf8JsonWriter json, List<DiagnosticDocument> documents)
    {
        json.WriteStartObject();
        json.WriteStartArray("diagnostics");
        foreach (DiagnosticDocument document in documents)
        {
            foreach (Diagnostic diagnostic in document.Diagnostics)
            {
                WriteDiagnosticObject(json, document, diagnostic);
            }
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteSarif(Utf8JsonWriter json, List<DiagnosticDocument> documents)
    {
        json.WriteStartObject();
        json.WriteString("$schema", SarifSchema);
        json.WriteString("version", "2.1.0");
        json.WriteStartArray("runs");
        json.WriteStartObject();

        json.WriteStartObject("tool");
        json.WriteStartObject("driver");
        json.WriteString("name", ToolName);
        json.WriteString("informationUri", ToolHomePage);
        json.WriteStartArray("rules");
        foreach (DiagnosticCodeInfo rule in DiagnosticCodeTable.All)
        {
            json.WriteStartObject();
            json.WriteString("id", rule.Code);
            json.WriteString("name", rule.Name);
            json.WriteStartObject("shortDescription");
            json.WriteString("text", rule.Description);
            json.WriteEndObject();
            json.WriteStartObject("defaultConfiguration");
            json.WriteString("level", SarifLevel(rule.DefaultSeverity));
            json.WriteEndObject();
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
        json.WriteEndObject();

        json.WriteString("columnKind", "utf16CodeUnits");
        json.WriteStartArray("results");
        foreach (DiagnosticDocument document in documents)
        {
            foreach (Diagnostic diagnostic in document.Diagnostics)
            {
                WriteSarifResult(json, document, diagnostic);
            }
        }

        json.WriteEndArray();
        json.WriteEndObject();
        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static void WriteSarifResult(Utf8JsonWriter json, DiagnosticDocument document, Diagnostic diagnostic)
    {
        json.WriteStartObject();
        json.WriteString("ruleId", diagnostic.Code);
        int index = IndexOfRule(diagnostic.Code);
        if (index >= 0)
        {
            json.WriteNumber("ruleIndex", index);
        }

        json.WriteString("level", SarifLevel(diagnostic.Severity));
        json.WriteStartObject("message");
        json.WriteString("text", diagnostic.Message);
        json.WriteEndObject();

        string? uri = ArtifactUri(document.Origin);
        bool region = document.RulePath is null && document.Text is not null && HasPosition(diagnostic);
        string? logical = LogicalName(document.RulePath, diagnostic.Path);
        if (uri is not null || logical is not null)
        {
            json.WriteStartArray("locations");
            json.WriteStartObject();
            if (uri is not null)
            {
                json.WriteStartObject("physicalLocation");
                json.WriteStartObject("artifactLocation");
                json.WriteString("uri", uri);
                json.WriteEndObject();
                if (region)
                {
                    WriteRegion(json, diagnostic.Span, document.Text!);
                }

                json.WriteEndObject();
            }

            if (logical is not null)
            {
                json.WriteStartArray("logicalLocations");
                json.WriteStartObject();
                json.WriteString("fullyQualifiedName", logical);
                json.WriteString("kind", "element");
                json.WriteEndObject();
                json.WriteEndArray();
            }

            json.WriteEndObject();
            json.WriteEndArray();
        }

        if (diagnostic.Expected is not null || diagnostic.Found is not null || diagnostic.Suggestion is not null)
        {
            json.WriteStartObject("properties");
            WriteDetails(json, diagnostic);
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }

    private static void WriteRegion(Utf8JsonWriter json, SourceSpan span, string text)
    {
        (SourceLocation start, SourceLocation end) = Locate(span, text);
        json.WriteStartObject("region");
        json.WriteNumber("startLine", start.Line);
        json.WriteNumber("startColumn", start.Column);
        json.WriteNumber("endLine", end.Line);
        json.WriteNumber("endColumn", end.Column);
        json.WriteNumber("charOffset", span.Start);
        json.WriteNumber("charLength", span.Length);
        json.WriteEndObject();
    }

    private static void WriteDetails(Utf8JsonWriter json, Diagnostic diagnostic)
    {
        WriteIfPresent(json, "expected", diagnostic.Expected);
        WriteIfPresent(json, "found", diagnostic.Found);
        if (diagnostic.Suggestion is { } suggestion)
        {
            json.WriteStartObject("suggestion");
            json.WriteString("kind", suggestion.Kind == DiagnosticSuggestionKind.Replacement ? "replacement" : "advice");
            json.WriteString("text", suggestion.Text);
            json.WriteEndObject();
        }
    }

    private static void WriteIfPresent(Utf8JsonWriter json, string name, string? value)
    {
        if (value is not null)
        {
            json.WriteString(name, value);
        }
    }

    // SourceSpan.None marks a finding about the whole input, which has no position.
    private static bool HasPosition(Diagnostic diagnostic)
    {
        return diagnostic.Span != SourceSpan.None;
    }

    // The end location is the position after the last character, which is what SARIF's endColumn means.
    private static (SourceLocation Start, SourceLocation End) Locate(SourceSpan span, string text)
    {
        return (span.GetLocation(text), new SourceSpan(span.End, 0).GetLocation(text));
    }

    private static string SarifLevel(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "note",
        };
    }

    private static int IndexOfRule(string code)
    {
        IReadOnlyList<DiagnosticCodeInfo> rules = DiagnosticCodeTable.All;
        for (int i = 0; i < rules.Count; i++)
        {
            if (string.Equals(rules[i].Code, code, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    // The tree path of a finding inside an embedded rule is the rule's path followed by the finding's path without its "$".
    private static string? LogicalName(string? rulePath, string? path)
    {
        if (rulePath is null)
        {
            return path;
        }

        return path is null ? rulePath : rulePath + path.TrimStart('$');
    }

    /// <summary>
    /// Gets the SARIF artifact URI of an input: a <c>file</c> URI for an absolute path, a relative reference with
    /// <c>/</c> separators for a relative path, and nothing for standard input, which has no file.
    /// </summary>
    private static string? ArtifactUri(string origin)
    {
        if (origin == SourceReader.StandardInput)
        {
            return null;
        }

        if (Path.IsPathFullyQualified(origin))
        {
            return new Uri(origin).AbsoluteUri;
        }

        return string.Join('/', origin.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));
    }
}
