namespace TruthWeaver.Tool.Core.Documents;

using System.Text.Json;
using TruthWeaver.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>The JSON kind of a <see cref="TreeValue"/>.</summary>
internal enum TreeValueKind
{
    Object,
    Array,
    String,
    Number,
    Boolean,
    Null,
}

/// <summary>
/// One value of a JSON or YAML rule document, with its JSON kind and its span in the source text. The tree schema check
/// reads JSON and YAML into this one shape, so the same schema check runs on both formats and every finding has a span.
/// </summary>
/// <param name="Kind">The JSON kind.</param>
/// <param name="Span">The span of the value in the source text.</param>
internal sealed record TreeValue(TreeValueKind Kind, SourceSpan Span)
{
    /// <summary>Gets the members of an object, in document order. Empty for other kinds.</summary>
    public IReadOnlyList<KeyValuePair<string, TreeValue>> Members { get; init; } = [];

    /// <summary>Gets the items of an array. Empty for other kinds.</summary>
    public IReadOnlyList<TreeValue> Items { get; init; } = [];

    /// <summary>Gets the text of a string, or the written form of a number or a Boolean. <see langword="null"/> for other kinds.</summary>
    public string? Text { get; init; }

    /// <summary>Gets a value indicating whether a number has no fraction, so it is a JSON schema <c>integer</c>.</summary>
    public bool IsInteger { get; init; }

    /// <summary>Reads a JSON document.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The root value, or <see langword="null"/> when the text is not well-formed JSON.</returns>
    public static TreeValue? ReadJson(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new(bytes, new JsonReaderOptions { MaxDepth = 4096 });
        OffsetMap offsets = new(bytes);
        try
        {
            if (!reader.Read())
            {
                return null;
            }

            TreeValue root = ReadJsonValue(ref reader, offsets);
            return reader.Read() ? null : root;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Reads the first document of a YAML stream, typing plain scalars as the YAML rule reader does.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <returns>The root value, or <see langword="null"/> when the text is not well-formed YAML or has no document.</returns>
    public static TreeValue? ReadYaml(string yaml)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException)
        {
            return null;
        }

        return stream.Documents.Count == 0 ? null : FromYaml(stream.Documents[0].RootNode);
    }

    private static TreeValue ReadJsonValue(ref Utf8JsonReader reader, OffsetMap offsets)
    {
        int start = offsets.CharOffset((int)reader.TokenStartIndex);
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
            {
                List<KeyValuePair<string, TreeValue>> members = [];
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    string name = reader.GetString() ?? string.Empty;
                    _ = reader.Read();
                    members.Add(new(name, ReadJsonValue(ref reader, offsets)));
                }

                return new TreeValue(TreeValueKind.Object, EndSpan(start, ref reader, offsets)) { Members = members };
            }

            case JsonTokenType.StartArray:
            {
                List<TreeValue> items = [];
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    items.Add(ReadJsonValue(ref reader, offsets));
                }

                return new TreeValue(TreeValueKind.Array, EndSpan(start, ref reader, offsets)) { Items = items };
            }

            case JsonTokenType.String:
                return new TreeValue(TreeValueKind.String, EndSpan(start, ref reader, offsets)) { Text = reader.GetString() };

            case JsonTokenType.Number:
            {
                string number = Encoding.UTF8.GetString(reader.ValueSpan);
                return new TreeValue(TreeValueKind.Number, EndSpan(start, ref reader, offsets))
                {
                    Text = number,
                    IsInteger = IsWholeNumber(number),
                };
            }

            case JsonTokenType.True:
            case JsonTokenType.False:
                return new TreeValue(TreeValueKind.Boolean, EndSpan(start, ref reader, offsets))
                {
                    Text = reader.TokenType == JsonTokenType.True ? "true" : "false",
                };

            default:
                return new TreeValue(TreeValueKind.Null, EndSpan(start, ref reader, offsets));
        }
    }

    // The span ends after the current token: a closing brace or bracket for a container, the scalar itself otherwise. A
    // string token's ValueSpan excludes its quotes, so the end is found from the reader's position after the token.
    private static SourceSpan EndSpan(int start, ref Utf8JsonReader reader, OffsetMap offsets)
    {
        int end = offsets.CharOffset((int)reader.BytesConsumed);
        return new SourceSpan(start, end - start);
    }

    private static TreeValue FromYaml(YamlNode node)
    {
        int start = (int)node.Start.Index;
        SourceSpan span = new(start, Math.Max(0, (int)node.End.Index - start));
        return node switch
        {
            YamlMappingNode mapping => new TreeValue(TreeValueKind.Object, span)
            {
                Members =
                [
                    .. mapping.Children.Select(entry => new KeyValuePair<string, TreeValue>(
                        (entry.Key as YamlScalarNode)?.Value ?? string.Empty,
                        FromYaml(entry.Value)
                    )),
                ],
            },
            YamlSequenceNode sequence => new TreeValue(TreeValueKind.Array, span)
            {
                Items = [.. sequence.Children.Select(FromYaml)],
            },
            YamlScalarNode scalar => FromYamlScalar(scalar, span),
            _ => new TreeValue(TreeValueKind.Null, span),
        };
    }

    // The same typing as the YAML rule reader: a quoted, block or !!str scalar is a string; a plain scalar is null, a
    // Boolean (true or false in any case), a number (any decimal), or else a string.
    private static TreeValue FromYamlScalar(YamlScalarNode scalar, SourceSpan span)
    {
        string text = scalar.Value ?? string.Empty;
        if (scalar.Style is not ScalarStyle.Plain || scalar.Tag == "tag:yaml.org,2002:str")
        {
            return new TreeValue(TreeValueKind.String, span) { Text = text };
        }

        if (text.Length == 0 || text == "~" || text.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return new TreeValue(TreeValueKind.Null, span);
        }

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return new TreeValue(TreeValueKind.Boolean, span) { Text = text.ToLowerInvariant() };
        }

        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            ? new TreeValue(TreeValueKind.Number, span) { Text = text, IsInteger = IsWholeNumber(text) }
            : new TreeValue(TreeValueKind.String, span) { Text = text };
    }

    private static bool IsWholeNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
            && decimal.Truncate(value) == value;
    }

    /// <summary>
    /// Converts UTF-8 byte offsets to character offsets. The JSON reader reports byte offsets, and diagnostic spans are
    /// character offsets. The reader asks in increasing order, so the map counts forward from the last answer.
    /// </summary>
    private sealed class OffsetMap(byte[] bytes)
    {
        private int lastByte;
        private int lastChar;

        public int CharOffset(int byteOffset)
        {
            if (byteOffset < this.lastByte)
            {
                this.lastByte = 0;
                this.lastChar = 0;
            }

            this.lastChar += Encoding.UTF8.GetCharCount(bytes, this.lastByte, byteOffset - this.lastByte);
            this.lastByte = byteOffset;
            return this.lastChar;
        }
    }
}
