namespace TruthWeaver.Tool.Core.Assignment;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>
/// Reads the data for the <c>from("source", "query")</c> references of a rule from one JSON or YAML document. The document
/// is an object that maps each source name to the data of that source:
/// <c>{"user": {"region": "EU"}}</c> gives the source <c>user</c> one object, which <c>from("user", "$.region")</c> queries.
/// </summary>
/// <remarks>
/// A YAML document reads as JSON would: a plain scalar is <c>null</c>, a Boolean, a whole number, a decimal number or a
/// string, and a quoted scalar is a string. The same reading serves both formats, so the data does not depend on the format
/// it is written in.
/// </remarks>
public static class AssignmentVariables
{
    private const int MaxDepth = 64;

    /// <summary>Reads the document as one JSON object that maps each source name to its data.</summary>
    /// <param name="text">The JSON or YAML text. An empty text, or YAML with only comments, is an object with no source.</param>
    /// <returns>The object, or a <see cref="ToolDiagnosticCodes.MalformedDocument"/> error. The message never repeats document content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<JsonObject> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return ToolResults.Success(new JsonObject());
        }

        if (!TryReadAny(text, out JsonNode? root, out string? error))
        {
            return Malformed(error);
        }

        return root is JsonObject sources
            ? ToolResults.Success(sources)
            : Malformed(
                "The data must be an object that maps each source name to its data, such as {\"user\": {\"region\": \"EU\"}}."
            );
    }

    /// <summary>Reads the document and makes a data source for each source name.</summary>
    /// <param name="text">The JSON or YAML text.</param>
    /// <returns>The data sources, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static ToolResult<DataSourceSet> Parse(string text)
    {
        if (!Read(text).TryGetValue(out JsonObject? root, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<DataSourceSet>(diagnostics);
        }

        DataSourceSet sources = [];
        foreach ((string name, JsonNode? data) in root)
        {
            // Each source goes through the same loader as a data file, so the answers equal those of `truthweaver evaluate --data`.
            ToolResult<IDataSource> source = DataSourceFiles.Parse(data?.ToJsonString() ?? "null", RuleFormat.Json);
            if (!source.TryGetValue(out IDataSource? value, out EquatableArray<Diagnostic> sourceDiagnostics))
            {
                return ToolResults.Failure<DataSourceSet>(sourceDiagnostics);
            }

            sources.Add(name, value);
        }

        return ToolResults.Success(sources);
    }

    /// <summary>Writes a JSON value as block YAML, for a document that embeds the data, such as a rule test file.</summary>
    /// <param name="value">The value.</param>
    /// <param name="indent">The number of spaces in front of each line.</param>
    /// <returns>The lines of the value, with no trailing line feed. A scalar is one line and a string is double-quoted.</returns>
    public static string ToYamlBlock(JsonNode? value, int indent)
    {
        StringBuilder text = new();
        WriteBlock(text, value, indent);
        return text.ToString().TrimEnd('\n');
    }

    /// <summary>Reads JSON or YAML text of any shape as one JSON value.</summary>
    /// <param name="text">The text, not empty.</param>
    /// <param name="root">The value; <see langword="null"/> for a JSON or YAML null.</param>
    /// <param name="error">The problem when the text is not well-formed. It never repeats document content.</param>
    /// <returns><see langword="true"/> when the text is well-formed.</returns>
    internal static bool TryReadAny(string text, out JsonNode? root, [NotNullWhen(false)] out string? error)
    {
        try
        {
            root = FormatDetector.DetectDocument(text, null, null) == RuleFormat.Json ? JsonNode.Parse(text) : ReadYaml(text);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.LineNumber is { } line
                ? $"The JSON data is not well-formed (line {line + 1})."
                : "The JSON data is not well-formed.";
        }
        catch (YamlException ex)
        {
            error = $"The YAML data is not well-formed (line {ex.Start.Line}, column {ex.Start.Column}).";
        }
        catch (ArgumentException)
        {
            error = "The data names a key two times, or nests too deep.";
        }

        root = null;
        return false;
    }

    /// <summary>Reads a YAML node as a JSON value, with the typing of plain scalars that this class describes.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">A key is not a scalar, a key is used two times, or the node nests deeper than 64 levels.</exception>
    internal static JsonNode? FromYaml(YamlNode node)
    {
        return FromYaml(node, 0);
    }

    private static ToolResult<JsonObject> Malformed(string message)
    {
        return ToolResults.Failure<JsonObject>(ToolDiagnosticCodes.MalformedDocument, message);
    }

    private static JsonNode? ReadYaml(string text)
    {
        YamlStream stream = [];
        using StringReader reader = new(text);
        stream.Load(reader);
        return stream.Documents.Count == 0 ? new JsonObject() : FromYaml(stream.Documents[0].RootNode, 0);
    }

    private static JsonNode? FromYaml(YamlNode node, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new ArgumentException("The data nests too deep.");
        }

        switch (node)
        {
            case YamlMappingNode mapping:
                JsonObject result = [];
                foreach ((YamlNode key, YamlNode value) in mapping.Children)
                {
                    if (key is not YamlScalarNode name)
                    {
                        throw new ArgumentException("A key must be a scalar.");
                    }

                    result.Add(name.Value ?? string.Empty, FromYaml(value, depth + 1));
                }

                return result;
            case YamlSequenceNode sequence:
                return new JsonArray([.. sequence.Children.Select(child => FromYaml(child, depth + 1))]);
            case YamlScalarNode scalar:
                return FromScalar(scalar);
            default:
                return null;
        }
    }

    private static JsonNode? FromScalar(YamlScalarNode scalar)
    {
        string value = scalar.Value ?? string.Empty;
        if (scalar.Style != ScalarStyle.Plain)
        {
            return JsonValue.Create(value);
        }

        if (value.Length == 0 || value is "~" or "null" or "Null" or "NULL")
        {
            return null;
        }

        if (value is "true" or "True" or "TRUE")
        {
            return JsonValue.Create(true);
        }

        if (value is "false" or "False" or "FALSE")
        {
            return JsonValue.Create(false);
        }

        if (long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long whole))
        {
            return JsonValue.Create(whole);
        }

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
            ? JsonValue.Create(number)
            : JsonValue.Create(value);
    }

    private static void WriteBlock(StringBuilder text, JsonNode? value, int indent)
    {
        string pad = new(' ', indent);
        switch (value)
        {
            case JsonObject { Count: > 0 } map:
                foreach ((string key, JsonNode? child) in map)
                {
                    text.Append(pad).Append(JsonValue.Create(key).ToJsonString()).Append(':');
                    WriteChild(text, child, indent);
                }

                break;
            case JsonArray { Count: > 0 } list:
                foreach (JsonNode? child in list)
                {
                    text.Append(pad).Append('-');
                    WriteChild(text, child, indent);
                }

                break;
            default:
                text.Append(pad).Append(Scalar(value)).Append('\n');
                break;
        }
    }

    // A scalar and an empty collection follow their key or dash on the same line; a filled collection goes on the lines below.
    private static void WriteChild(StringBuilder text, JsonNode? child, int indent)
    {
        if (child is JsonObject { Count: > 0 } or JsonArray { Count: > 0 })
        {
            text.Append('\n');
            WriteBlock(text, child, indent + 2);
        }
        else
        {
            text.Append(' ').Append(Scalar(child)).Append('\n');
        }
    }

    private static string Scalar(JsonNode? value)
    {
        return value switch
        {
            JsonObject => "{}",
            JsonArray => "[]",
            null => "null",
            _ => value.ToJsonString(),
        };
    }
}
