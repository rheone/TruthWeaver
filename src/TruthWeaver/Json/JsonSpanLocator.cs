namespace TruthWeaver.Json;

using System.Text;
using System.Text.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Finds where each node of a JSON rule tree sits in the original text, so a diagnostic located by a path (such as
/// <c>$.operands[1].op</c>) can also carry a <see cref="SourceSpan"/>. A <see cref="JsonElement"/> keeps no positions,
/// so this reads the text again with <see cref="Utf8JsonReader"/>, which reports byte offsets. Those are converted to
/// the UTF-16 character offsets <see cref="SourceSpan"/> uses, so multibyte text lines up. The pass runs only once a diagnostic needs it, never for a clean compile.
/// </summary>
internal sealed class JsonSpanLocator
{
    private readonly Dictionary<string, SourceSpan> spans;

    private JsonSpanLocator(Dictionary<string, SourceSpan> spans)
    {
        this.spans = spans;
    }

    /// <summary>Reads <paramref name="json"/> and records the span of every node by its path.</summary>
    /// <param name="json">The JSON text that was compiled, exactly as given.</param>
    /// <returns>A locator for that text; it is empty when the text is not well-formed JSON.</returns>
    public static JsonSpanLocator Create(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        int[]? charOffsets = bytes.Length == json.Length ? null : ByteToCharOffsets(json, bytes.Length);
        Dictionary<string, SourceSpan> spans = [with(StringComparer.Ordinal)];

        Utf8JsonReader reader = new(bytes);
        List<Frame> frames = [];
        try
        {
            while (reader.Read())
            {
                int start = (int)reader.TokenStartIndex;
                int end = (int)reader.BytesConsumed;
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject or JsonTokenType.StartArray:
                        frames.Add(new Frame(NextPath(frames), reader.TokenType == JsonTokenType.StartArray, start));
                        break;
                    case JsonTokenType.EndObject or JsonTokenType.EndArray:
                        Frame closed = frames[^1];
                        frames.RemoveAt(frames.Count - 1);
                        Record(spans, closed.Path, closed.Start, end, charOffsets);
                        break;
                    case JsonTokenType.PropertyName:
                        frames[^1].Name = reader.GetString() ?? string.Empty;
                        break;
                    case JsonTokenType.Comment or JsonTokenType.None:
                        break;
                    default:
                        Record(spans, NextPath(frames), start, end, charOffsets);
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed text was already reported as a syntax error; whatever was located before it stays usable.
        }

        return new JsonSpanLocator(spans);
    }

    /// <summary>Gets the span of the node at <paramref name="path"/>, or <see cref="SourceSpan.None"/> when it is not in the text.</summary>
    /// <param name="path">A path built by <see cref="TreePath"/>.</param>
    /// <returns>The node's span.</returns>
    public SourceSpan Locate(string path)
    {
        return this.spans.GetValueOrDefault(path, SourceSpan.None);
    }

    // The path of the value that is starting now: the root, the parent's next item, or the parent's current key.
    private static string NextPath(List<Frame> frames)
    {
        if (frames.Count == 0)
        {
            return TreePath.Root;
        }

        Frame parent = frames[^1];
        return parent.IsArray ? TreePath.Index(parent.Path, ++parent.ItemIndex) : TreePath.Property(parent.Path, parent.Name!);
    }

    private static void Record(
        Dictionary<string, SourceSpan> spans,
        string path,
        int startByte,
        int endByte,
        int[]? charOffsets
    )
    {
        int start = charOffsets is null ? startByte : charOffsets[startByte];
        int end = charOffsets is null ? endByte : charOffsets[endByte];

        // A repeated key keeps the last occurrence, the same one JsonElement.TryGetProperty resolves to.
        spans[path] = new SourceSpan(start, end - start);
    }

    // Maps every UTF-8 byte offset to the index of the character it belongs to; the offset one past the end maps to the length.
    private static int[] ByteToCharOffsets(string json, int byteCount)
    {
        int[] offsets = new int[byteCount + 1];
        int bytePosition = 0;
        int i = 0;
        while (i < json.Length)
        {
            int width = char.IsHighSurrogate(json[i]) && i + 1 < json.Length ? 2 : 1;
            int bytes = Encoding.UTF8.GetByteCount(json.AsSpan(i, width));
            for (int b = 0; b < bytes; b++)
            {
                offsets[bytePosition++] = i;
            }

            i += width;
        }

        offsets[byteCount] = json.Length;
        return offsets;
    }

    private sealed class Frame(string path, bool isArray, int start)
    {
        public string Path { get; } = path;

        public bool IsArray { get; } = isArray;

        public int Start { get; } = start;

        public string? Name { get; set; }

        public int ItemIndex { get; set; } = -1;
    }
}
