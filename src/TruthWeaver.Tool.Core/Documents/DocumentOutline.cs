namespace TruthWeaver.Tool.Core.Documents;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Lists the places of a JSON or YAML document where a rule can be, so that a front end can let the author browse the
/// document and pick one, and finds the places that a path pattern names. A rule in the tree format is an object with a
/// <c>const</c>, <c>predicate</c> or <c>op</c> key; a string is rule text.
/// </summary>
public static class DocumentOutline
{
    private const int LabelLength = 48;
    private static readonly string[] RuleKeys = ["const", "predicate", "op"];

    /// <summary>
    /// Lists the objects, arrays and strings of a document in document order. A rule in the tree format is one entry: the
    /// outline does not list the nodes inside it.
    /// </summary>
    /// <param name="document">The document text.</param>
    /// <param name="format">The document format, <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</param>
    /// <returns>The entries, or a <see cref="ToolDiagnosticCodes.MalformedDocument"/> error when the document cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not <see cref="RuleFormat.Json"/> or <see cref="RuleFormat.Yaml"/>.</exception>
    public static ToolResult<IReadOnlyList<DocumentNode>> Browse(string document, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (format is not (RuleFormat.Json or RuleFormat.Yaml))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Only a JSON or YAML document has paths.");
        }

        List<DocumentNode> nodes = [];
        try
        {
            if (format == RuleFormat.Json)
            {
                WalkJson(JsonNode.Parse(document), [], nodes);
            }
            else
            {
                YamlStream stream = [];
                using StringReader reader = new(document);
                stream.Load(reader);
                if (stream.Documents.Count > 0)
                {
                    WalkYaml(stream.Documents[0].RootNode, [], nodes);
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or YamlException or ArgumentException)
        {
            return ToolResults.Failure<IReadOnlyList<DocumentNode>>(
                ToolDiagnosticCodes.MalformedDocument,
                $"The {(format == RuleFormat.Json ? "JSON" : "YAML")} document cannot be read: {ex.Message}"
            );
        }

        return ToolResults.Success<IReadOnlyList<DocumentNode>>(nodes);
    }

    /// <summary>Reads a document file and lists the places where a rule can be, as <see cref="Browse"/> does.</summary>
    /// <param name="path">The document file. Its format is found as for any rule file (the extension, then the content).</param>
    /// <returns>
    /// The entries, or the diagnostics: the file cannot be read, it is rule text (which has no paths, a
    /// <see cref="ToolDiagnosticCodes.NotARule"/> error), or it is not well formed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<IReadOnlyList<DocumentNode>> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        ToolResult<LoadedRule> loaded = RuleLoader.LoadFile(path);
        if (!loaded.TryGetValue(out LoadedRule? document, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<IReadOnlyList<DocumentNode>>(diagnostics);
        }

        return document.Format == RuleFormat.Text
            ? ToolResults.Failure<IReadOnlyList<DocumentNode>>(
                ToolDiagnosticCodes.NotARule,
                $"'{System.IO.Path.GetFileName(path)}' is rule text, which has no paths. A path selects a rule inside a JSON or YAML document."
            )
            : Browse(document.Source.Text, document.Format);
    }

    /// <summary>
    /// Finds the entries that a path pattern names. The pattern is a path (see <see cref="RuleLoadOptions.Path"/>) in which a
    /// segment can be <c>*</c> (as <c>.*</c> or <c>[*]</c>) to match every key or index at that place.
    /// </summary>
    /// <param name="outline">The entries of <see cref="Browse"/>.</param>
    /// <param name="pattern">The pattern, such as <c>$.rules.publish</c>, <c>rules.*</c> or <c>$.list[*]</c>.</param>
    /// <returns>The matching entries (none, one or many), or an <see cref="ToolDiagnosticCodes.InvalidPath"/> error when the pattern is not a path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="outline"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
    public static ToolResult<IReadOnlyList<DocumentNode>> Match(IReadOnlyList<DocumentNode> outline, string pattern)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(pattern);

        if (!TryParsePattern(pattern, out List<DocumentPath.Segment?>? segments, out string? problem))
        {
            return ToolResults.Failure<IReadOnlyList<DocumentNode>>(ToolDiagnosticCodes.InvalidPath, problem);
        }

        // The root has no segments, and the outline lists every entry below it, so a longer entry never matches.
        IReadOnlyList<DocumentNode> matches =
        [
            .. outline.Where(node =>
                node.Segments.Count == segments.Count
                && node.Segments.Zip(segments).All(pair => pair.Second is null || pair.First == pair.Second)
            ),
        ];
        return ToolResults.Success(matches);
    }

    private static void WalkJson(JsonNode? node, List<DocumentPath.Segment> path, List<DocumentNode> nodes)
    {
        switch (node)
        {
            case JsonObject obj:
                string[] keys = [.. obj.Select(property => property.Key)];
                if (keys.Intersect(RuleKeys, StringComparer.Ordinal).Any())
                {
                    nodes.Add(
                        Entry(
                            path,
                            DocumentNodeKind.Rule,
                            RuleLabel(keys, name => obj[name] is JsonValue v && v.TryGetValue(out string? s) ? s : null)
                        )
                    );
                    return;
                }

                nodes.Add(Entry(path, DocumentNodeKind.Container, KeysLabel(keys)));
                foreach (KeyValuePair<string, JsonNode?> property in obj)
                {
                    WalkJson(property.Value, [.. path, DocumentPath.Segment.Key(property.Key)], nodes);
                }

                break;
            case JsonArray array:
                nodes.Add(Entry(path, DocumentNodeKind.Container, ItemsLabel(array.Count)));
                for (int index = 0; index < array.Count; index++)
                {
                    WalkJson(array[index], [.. path, DocumentPath.Segment.Index(index)], nodes);
                }

                break;
            case JsonValue value when value.TryGetValue(out string? text):
                nodes.Add(Entry(path, DocumentNodeKind.Text, TextLabel(text)));
                break;
            default:
                break;
        }
    }

    private static void WalkYaml(YamlNode node, List<DocumentPath.Segment> path, List<DocumentNode> nodes)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                string[] keys = [.. mapping.Children.Select(c => (c.Key as YamlScalarNode)?.Value ?? string.Empty)];
                if (keys.Intersect(RuleKeys, StringComparer.Ordinal).Any())
                {
                    nodes.Add(
                        Entry(
                            path,
                            DocumentNodeKind.Rule,
                            RuleLabel(
                                keys,
                                name =>
                                    mapping.Children.FirstOrDefault(c => (c.Key as YamlScalarNode)?.Value == name).Value
                                        is YamlScalarNode { Value: { } value }
                                        ? value
                                        : null
                            )
                        )
                    );
                    return;
                }

                nodes.Add(Entry(path, DocumentNodeKind.Container, KeysLabel(keys)));
                foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
                {
                    if (child.Key is YamlScalarNode { Value: { } name })
                    {
                        WalkYaml(child.Value, [.. path, DocumentPath.Segment.Key(name)], nodes);
                    }
                }

                break;
            case YamlSequenceNode sequence:
                nodes.Add(Entry(path, DocumentNodeKind.Container, ItemsLabel(sequence.Children.Count)));
                int index = 0;
                foreach (YamlNode item in sequence.Children)
                {
                    WalkYaml(item, [.. path, DocumentPath.Segment.Index(index++)], nodes);
                }

                break;
            case YamlScalarNode { Value: { } value } scalar when IsString(scalar):
                nodes.Add(Entry(path, DocumentNodeKind.Text, TextLabel(value)));
                break;
            default:
                break;
        }
    }

    // A quoted or block scalar is a string. A plain scalar is a string unless it reads as a number, a boolean or null.
    private static bool IsString(YamlScalarNode scalar)
    {
        if (scalar.Style != ScalarStyle.Plain)
        {
            return true;
        }

        string value = scalar.Value ?? string.Empty;
        return value.Length > 0
            && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            && !value.ToLowerInvariant().Equals("true", StringComparison.Ordinal)
            && !value.ToLowerInvariant().Equals("false", StringComparison.Ordinal)
            && !value.Equals("~", StringComparison.Ordinal)
            && !value.ToLowerInvariant().Equals("null", StringComparison.Ordinal);
    }

    private static DocumentNode Entry(List<DocumentPath.Segment> path, DocumentNodeKind kind, string label)
    {
        string text = "$" + string.Concat(path.Select(segment => segment.Text));
        return new DocumentNode(text, path.Count, kind, label) { Segments = [.. path] };
    }

    private static string RuleLabel(string[] keys, Func<string, string?> valueOf)
    {
        string key = keys.First(k => RuleKeys.Contains(k, StringComparer.Ordinal));
        string? value = valueOf(key);
        return value is null ? $"rule ({key})" : $"rule ({key}: {value})";
    }

    private static string KeysLabel(string[] keys)
    {
        if (keys.Length == 0)
        {
            return "{}";
        }

        string list = string.Join(", ", keys.Take(4));
        return keys.Length > 4 ? "{" + list + ", ...}" : "{" + list + "}";
    }

    private static string ItemsLabel(int count)
    {
        return $"[{count.ToString(CultureInfo.InvariantCulture)} item{(count == 1 ? string.Empty : "s")}]";
    }

    private static string TextLabel(string text)
    {
        string line = text.ReplaceLineEndings(" ");
        return line.Length > LabelLength ? $"\"{line[..LabelLength]}...\"" : $"\"{line}\"";
    }

    // Parses a path in which a segment can be a wildcard. A null segment is a wildcard.
    private static bool TryParsePattern(
        string pattern,
        out List<DocumentPath.Segment?> segments,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? problem
    )
    {
        segments = [];
        problem = null;
        string text = pattern.Trim();
        int position = text.StartsWith('$') ? 1 : 0;
        while (position < text.Length)
        {
            if (text.AsSpan(position).StartsWith(".*") || text.AsSpan(position).StartsWith("[*]"))
            {
                segments.Add(null);
                position += text[position] == '.' ? 2 : 3;
                continue;
            }

            // Parse one segment with the path grammar: take the text up to the next segment start.
            int next = position + 1;
            while (next < text.Length && text[next] is not ('.' or '['))
            {
                next++;
            }

            // A bracket segment ends at its closing bracket, which may hold a dot in a quoted key.
            if (text[position] == '[')
            {
                int close = text.IndexOf(']', position);
                next = close < 0 ? text.Length : close + 1;
            }

            string piece = text[position..next];
            string candidate = position == 0 && !piece.StartsWith('[') && !piece.StartsWith('.') ? "$." + piece : "$" + piece;
            if (!DocumentPath.TryParse(candidate, out DocumentPath? parsed, out problem) || parsed.Segments.Count != 1)
            {
                problem ??= $"The path '{pattern}' is not valid at character {position + 1}.";
                return false;
            }

            segments.Add(parsed.Segments[0]);
            position = next;
        }

        return true;
    }
}
