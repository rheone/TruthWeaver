namespace TruthWeaver.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Parses the flat, key-discriminated JSON tree shape (ADR-0003) into the same raw
/// <see cref="RuleNode"/> tree the DSL parser produces, so both front ends funnel through identical
/// validation (ticket 07). A node is discriminated by which key is present: <c>const</c>,
/// <c>predicate</c>, or <c>op</c>. Never throws for malformed input — it reports a
/// <see cref="DiagnosticCodes.MalformedTree"/> diagnostic instead. Every diagnostic is located by its path from the
/// document root (<c>$.operands[1].op</c>), because a <see cref="JsonElement"/> does not keep source positions. Invalid JSON syntax carries the reader's
/// position; for every other diagnostic <c>RuleCompiler.CompileJson</c> adds the span afterwards (see
/// <see cref="JsonSpanLocator"/>), so this parser leaves them at <see cref="SourceSpan.None"/>.
/// </summary>
internal static class JsonTreeParser
{
    /// <summary>Parses JSON tree text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="json">The JSON tree text.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the JSON itself was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(
        [StringSyntax(StringSyntaxAttribute.Json)] string json
    )
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            List<Diagnostic> diagnostics = [SyntaxError(json, ex)];
            return (null, diagnostics);
        }

        using (document)
        {
            return Parse(document.RootElement);
        }
    }

    /// <summary>Parses an already-materialized JSON tree node (e.g. a subtree of a larger document) into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="element">The JSON tree node.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the element was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(JsonElement element)
    {
        return TreeFormatReader.Read(new JsonNodeCursor(element), JsonNodeCursor.Vocabulary);
    }

    /// <summary>Builds the diagnostic for text that is not JSON at all: the reader's message, its position, and the nearest valid ancestor.</summary>
    private static Diagnostic SyntaxError(string json, JsonException ex)
    {
        string reason = ex.Message;
        int detail = reason.IndexOf(" Path:", StringComparison.Ordinal);
        if (detail >= 0)
        {
            reason = reason[..detail];
        }

        SourceSpan span = ex is { LineNumber: { } line, BytePositionInLine: { } column }
            ? new SourceSpan(OffsetOf(json, line, column), 1)
            : SourceSpan.None;
        return Diagnostic.Error(
            DiagnosticCodes.MalformedTree,
            $"Malformed JSON: {ex.Message}",
            span,
            expected: "well-formed JSON",
            found: reason,
            path: ContainerPathAt(json)
        );
    }

    // JsonException reports a 0-based line and a byte (not character) offset within it; convert to a character offset.
    private static int OffsetOf(string json, long line, long bytePosition)
    {
        int lineStart = 0;
        for (long current = 0; current < line; current++)
        {
            int next = json.IndexOf('\n', lineStart);
            if (next < 0)
            {
                return json.Length;
            }

            lineStart = next + 1;
        }

        int offset = lineStart;
        long bytes = 0;
        while (offset < json.Length && bytes < bytePosition && json[offset] != '\n')
        {
            bytes += Encoding.UTF8.GetByteCount(json.AsSpan(offset, char.IsHighSurrogate(json[offset]) ? 2 : 1));
            offset += char.IsHighSurrogate(json[offset]) ? 2 : 1;
        }

        return Math.Min(offset, json.Length);
    }

    /// <summary>Reads the text token by token until the reader gives up, and returns the path of the innermost container still open.</summary>
    private static string ContainerPathAt(string json)
    {
        TreePathTracker tracker = new();
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        tracker.Enter(isArray: false);
                        break;
                    case JsonTokenType.StartArray:
                        tracker.Enter(isArray: true);
                        break;
                    case JsonTokenType.EndObject or JsonTokenType.EndArray:
                        tracker.Exit();
                        break;
                    case JsonTokenType.PropertyName:
                        tracker.Key(reader.GetString() ?? string.Empty);
                        break;
                    case JsonTokenType.Comment or JsonTokenType.None:
                        break;
                    default:
                        tracker.Scalar();
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // The reader stopping is the point: the tracker now holds the last good position.
        }

        return tracker.ContainerPath;
    }
}
