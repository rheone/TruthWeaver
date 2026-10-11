namespace TruthWeaver.Tool.Core;

using System.Text.Json;
using TruthWeaver.Compilation;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Selects one rule inside a larger JSON or YAML document by a <see cref="DocumentPath"/>. A JSON object or YAML mapping is a
/// tree-format rule in the document's own format; a string is rule text.
/// </summary>
internal static class EmbeddedRuleReader
{
    /// <summary>Selects the rule at <paramref name="path"/>.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="format">The document format, <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</param>
    /// <param name="path">The path of the rule.</param>
    /// <returns>The rule's format and text, or a diagnostic.</returns>
    public static ToolResult<RuleSelection> Select(string document, RuleFormat format, DocumentPath path)
    {
        return format == RuleFormat.Json ? SelectJson(document, path) : SelectYaml(document, path);
    }

    private static ToolResult<RuleSelection> SelectJson(string document, DocumentPath path)
    {
        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(document);
        }
        catch (JsonException ex)
        {
            return ToolResults.Failure<RuleSelection>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The JSON document is not well formed (line {ex.LineNumber + 1}, byte position {ex.BytePositionInLine + 1})."
            );
        }

        using (parsed)
        {
            JsonElement current = parsed.RootElement;
            for (int i = 0; i < path.Segments.Count; i++)
            {
                if (!TryGetChild(current, path.Segments[i], out current))
                {
                    return NotFound(path, i);
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.Object => ToolResults.Success(new RuleSelection(RuleFormat.Json, current.GetRawText())),
                JsonValueKind.String => ToolResults.Success(new RuleSelection(RuleFormat.Text, current.GetString()!)),
                _ => NotARule(path),
            };
        }
    }

    private static ToolResult<RuleSelection> SelectYaml(string document, DocumentPath path)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(document);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            return ToolResults.Failure<RuleSelection>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The YAML document is not valid (line {ex.Start.Line}, column {ex.Start.Column})."
            );
        }

        if (stream.Documents.Count == 0)
        {
            // An empty stream has no root, so even '$' selects no rule.
            return path.Segments.Count == 0 ? NotARule(path) : NotFound(path, 0);
        }

        YamlNode current = stream.Documents[0].RootNode;
        for (int i = 0; i < path.Segments.Count; i++)
        {
            YamlNode? next = Child(current, path.Segments[i]);
            if (next is null)
            {
                return NotFound(path, i);
            }

            current = next;
        }

        return current switch
        {
            YamlScalarNode { Value: { } value } => ToolResults.Success(new RuleSelection(RuleFormat.Text, value)),
            YamlMappingNode mapping => ToolResults.Success(new RuleSelection(RuleFormat.Yaml, Slice(document, mapping))),
            _ => NotARule(path),
        };
    }

    private static bool TryGetChild(JsonElement element, DocumentPath.Segment segment, out JsonElement child)
    {
        if (segment.Name is { } name)
        {
            child = default;
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out child);
        }

        bool inRange = element.ValueKind == JsonValueKind.Array && segment.Position < element.GetArrayLength();
        child = inRange ? element[segment.Position] : default;
        return inRange;
    }

    private static YamlNode? Child(YamlNode node, DocumentPath.Segment segment)
    {
        if (segment.Name is { } name)
        {
            return node is YamlMappingNode mapping
                ? mapping.Children.FirstOrDefault(entry => entry.Key is YamlScalarNode { Value: { } key } && key == name).Value
                : null;
        }

        return node is YamlSequenceNode sequence && segment.Position < sequence.Children.Count
            ? sequence.Children[segment.Position]
            : null;
    }

    // The mapping's own source text. A block mapping's first line starts at its first key, while its other lines keep
    // their indentation, so the first line is indented to the same column to make the slice a valid document by itself.
    // Comments inside the slice stay, and a 'note' key is part of the mapping, so notes stay too.
    private static string Slice(string document, YamlMappingNode mapping)
    {
        int start = OffsetOf(document, mapping.Start);
        int end = Math.Max(EndOffset(document, mapping.Start), start);

        // A flow mapping's end event is marked at its closing brace, not after it.
        if (mapping.Style == MappingStyle.Flow && end < document.Length && document[end] == '}')
        {
            end++;
        }

        int column = checked((int)mapping.Start.Column) - 1;
        return new string(' ', Math.Max(column, 0)) + document[start..end];
    }

    // The offset just past a mapping. A representation-model node's End mark is the end of its start event, not of the
    // mapping, so the event stream is read again to find the mapping end event that matches the start at 'start'.
    private static int EndOffset(string document, Mark start)
    {
        using StringReader reader = new(document);
        Parser parser = new(reader);
        int depth = -1;
        while (parser.MoveNext())
        {
            ParsingEvent current = parser.Current!;
            if (depth < 0)
            {
                bool isStart =
                    current is MappingStart && current.Start.Line == start.Line && current.Start.Column == start.Column;
                depth = isStart ? 0 : -1;
                continue;
            }

            depth += current.NestingIncrease;
            if (depth < 0)
            {
                return OffsetOf(document, current.End);
            }
        }

        return document.Length;
    }

    // The character offset of a mark, from its 1-based line and column.
    private static int OffsetOf(string document, Mark mark)
    {
        int offset = 0;
        for (long line = 1; line < mark.Line && offset < document.Length; line++)
        {
            int lineFeed = document.IndexOf('\n', offset);
            offset = lineFeed < 0 ? document.Length : lineFeed + 1;
        }

        return Math.Min(offset + checked((int)mark.Column) - 1, document.Length);
    }

    private static ToolResult<RuleSelection> NotFound(DocumentPath path, int failedSegment)
    {
        return ToolResults.Failure<RuleSelection>(
            ToolDiagnosticCodes.PathNotFound,
            $"The path '{path.Text}' selects nothing: '{path.Format(failedSegment)}' has no '{path.Segments[failedSegment].Text}'.",
            path.Text
        );
    }

    private static ToolResult<RuleSelection> NotARule(DocumentPath path)
    {
        return ToolResults.Failure<RuleSelection>(
            ToolDiagnosticCodes.NotARule,
            $"The value at '{path.Text}' is not a rule. A rule is an object (a JSON object or a YAML mapping) or a string of rule text.",
            path.Text
        );
    }
}
