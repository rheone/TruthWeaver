namespace TruthWeaver.Tool.Core;

using System.Collections.Frozen;
using System.Text.Json;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Stub values: one truth value for each term of a rule, which the tool uses in place of a host's predicates. A stub-value
/// file is a JSON object or a YAML mapping from a term, written as rule text, to <c>True</c>, <c>False</c> or
/// <c>Unknown</c>.
/// </summary>
/// <remarks>
/// <para>A key is one term in rule text: <c>isAdmin</c> or <c>hasRole(role: "admin")</c>. The loader compiles each key and
/// keys the value by the term's canonical text, so <c>hasRole( role:"admin" )</c> and <c>hasRole(role: "admin")</c> are the
/// same key. The predicate and argument names keep the case they are written in. A value is <c>True</c>, <c>False</c> or <c>Unknown</c>, or <c>T</c>, <c>F</c> or <c>U</c>, with any case, or a
/// JSON <c>true</c> or <c>false</c>.</para>
/// <para>A key that is not exactly one term, a value that is not a truth value, and two keys for the same term are
/// <see cref="ToolDiagnosticCodes.InvalidStubValue"/> errors. A term with no entry is <see cref="TruthValue.Unknown"/>.</para>
/// </remarks>
public sealed class StubAssignment
{
    private StubAssignment(FrozenDictionary<string, TruthValue> values)
    {
        this.Values = values;
    }

    /// <summary>Gets a stub assignment with no values, in which every term is <see cref="TruthValue.Unknown"/>.</summary>
    public static StubAssignment Empty { get; } = new([]);

    /// <summary>Gets the value of each term, keyed by the canonical text of the term.</summary>
    public IReadOnlyDictionary<string, TruthValue> Values { get; }

    /// <summary>Gets the terms in ordinal order of their canonical text, so output that lists them is the same on each run.</summary>
    public IReadOnlyList<string> Terms => [.. this.Values.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Reads a stub-value file. The file extension, then the content, decides between JSON and YAML.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The stub values, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<StubAssignment> Load(string path)
    {
        ToolResult<SourceText> read = SourceReader.ReadFile(path);
        if (!read.TryGetValue(out SourceText? source, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<StubAssignment>(diagnostics);
        }

        return Parse(source.Text, FormatDetector.DetectDocument(source.Text, path, null));
    }

    /// <summary>Reads stub values from text.</summary>
    /// <param name="text">The JSON or YAML text.</param>
    /// <param name="format"><see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>; <see cref="RuleFormat.Text"/> is read as YAML.</param>
    /// <returns>The stub values, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<StubAssignment> Parse(string text, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<Diagnostic> diagnostics = [];
        List<Entry>? entries = format == RuleFormat.Json ? ReadJson(text, diagnostics) : ReadYaml(text, diagnostics);
        if (entries is null)
        {
            return ToolResults.Failure<StubAssignment>(diagnostics);
        }

        RuleCompiler<StubAssignment> compiler = RuleConverter.CreateCompiler();
        Dictionary<string, TruthValue> values = [with(StringComparer.Ordinal)];
        foreach (Entry entry in entries)
        {
            string path = "$['" + entry.Key + "']";
            string? term = CanonicalTerm(compiler, entry.Key);
            if (term is null)
            {
                diagnostics.Add(
                    Invalid(
                        $"The stub key '{entry.Key}' is not exactly one term, such as isAdmin or hasRole(role: \"admin\").",
                        path
                    )
                );
            }
            else if (entry.Value is not { } value)
            {
                diagnostics.Add(
                    Invalid($"The stub value for '{entry.Key}' is not a truth value. Use True, False or Unknown.", path)
                );
            }
            else if (!values.TryAdd(term, value))
            {
                diagnostics.Add(Invalid($"The stub key '{entry.Key}' names the term '{term}' a second time.", path));
            }
        }

        return diagnostics.Count > 0
            ? ToolResults.Failure<StubAssignment>(diagnostics)
            : ToolResults.Success(new StubAssignment(values.ToFrozenDictionary(StringComparer.Ordinal)));
    }

    /// <summary>Gets the value of a term, or <see cref="TruthValue.Unknown"/> when the term has no entry.</summary>
    /// <param name="term">The canonical text of the term, as <see cref="CompiledRule{TContext}.CanonicalText"/> prints a one-term rule.</param>
    /// <returns>The value.</returns>
    public TruthValue ValueOf(string term)
    {
        return this.TryGetValue(term, out TruthValue value) ? value : TruthValue.Unknown;
    }

    /// <summary>Gets the value of a term when it has an entry.</summary>
    /// <param name="term">The canonical text of the term.</param>
    /// <param name="value">The value, or <see cref="TruthValue.Unknown"/> when the term has no entry.</param>
    /// <returns><see langword="true"/> when the term has an entry.</returns>
    public bool TryGetValue(string term, out TruthValue value)
    {
        ArgumentNullException.ThrowIfNull(term);

        bool found = this.Values.TryGetValue(term, out value);
        if (!found)
        {
            value = TruthValue.Unknown;
        }

        return found;
    }

    private static string? CanonicalTerm(RuleCompiler<StubAssignment> compiler, string key)
    {
        return
            compiler.TryCompile(key, out CompiledRule<StubAssignment>? rule, out _)
            && rule.Metrics is { NodeCount: 1, DistinctTermCount: 1 }
            ? rule.CanonicalText
            : null;
    }

    private static List<Entry>? ReadJson(string text, List<Diagnostic> diagnostics)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            diagnostics.Add(Malformed($"line {ex.LineNumber + 1}, byte position {ex.BytePositionInLine + 1}", "JSON"));
            return null;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(Invalid("The stub values must be a JSON object from term to value.", "$"));
                return null;
            }

            List<Entry> entries = [];
            using JsonElement.ObjectEnumerator properties = document.RootElement.EnumerateObject();
            foreach (JsonProperty property in properties)
            {
                entries.Add(new Entry(property.Name, ParseJsonValue(property.Value)));
            }

            return entries;
        }
    }

    private static TruthValue? ParseJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => TruthValue.True,
            JsonValueKind.False => TruthValue.False,
            JsonValueKind.String => ParseValue(value.GetString()!),
            _ => null,
        };
    }

    private static List<Entry>? ReadYaml(string text, List<Diagnostic> diagnostics)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(text);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            diagnostics.Add(Malformed($"line {ex.Start.Line}, column {ex.Start.Column}", "YAML"));
            return null;
        }

        if (stream.Documents.Count == 0)
        {
            return [];
        }

        if (stream.Documents[0].RootNode is not YamlMappingNode mapping)
        {
            diagnostics.Add(Invalid("The stub values must be a YAML mapping from term to value.", "$"));
            return null;
        }

        return
        [
            .. mapping.Children.Select(entry => new Entry(
                entry.Key is YamlScalarNode { Value: { } key } ? key : string.Empty,
                entry.Value is YamlScalarNode { Value: { } value } ? ParseValue(value) : null
            )),
        ];
    }

    private static TruthValue? ParseValue(string text)
    {
        return text.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "T" => TruthValue.True,
            "FALSE" or "F" => TruthValue.False,
            "UNKNOWN" or "U" => TruthValue.Unknown,
            _ => null,
        };
    }

    private static Diagnostic Invalid(string message, string path)
    {
        return ToolResults.Error(ToolDiagnosticCodes.InvalidStubValue, message, path);
    }

    private static Diagnostic Malformed(string position, string format)
    {
        return ToolResults.Error(
            ToolDiagnosticCodes.MalformedDocument,
            $"The stub-value document is not well-formed {format} ({position})."
        );
    }

    /// <summary>One key and value as the document wrote them; the value is <see langword="null"/> when it is not a truth value.</summary>
    private sealed record Entry(string Key, TruthValue? Value);
}
