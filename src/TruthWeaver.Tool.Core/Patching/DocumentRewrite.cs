namespace TruthWeaver.Tool.Core.Patching;

using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// The round-trip half of <see cref="RulePatcher"/>: reads the whole document into a document model, puts each edited rule
/// at its path, and serializes the whole document again. YAML comments and the document's layout are not kept.
/// </summary>
internal static class DocumentRewrite
{
    /// <summary>Writes a JSON document again with the edited rules, indented by two spaces.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="edits">Each rule as loaded from <paramref name="document"/>, with its edit.</param>
    /// <returns>The new document, or <see langword="null"/> when the document cannot be read into a model.</returns>
    public static string? Json(string document, IReadOnlyList<(LoadedRule Loaded, PrintedRule Rule)> edits)
    {
        try
        {
            JsonNode? root = JsonNode.Parse(document);
            foreach ((LoadedRule loaded, PrintedRule rule) in edits)
            {
                JsonNode replacement =
                    loaded.Format == RuleFormat.Text ? JsonValue.Create(rule.Text) : JsonNode.Parse(rule.Json)!;
                IReadOnlyList<DocumentPath.Segment> segments = SegmentsOf(loaded);
                if (segments.Count == 0)
                {
                    root = replacement;
                    continue;
                }

                JsonNode parent = segments.SkipLast(1).Aggregate(root!, (node, segment) => Child(node, segment));
                DocumentPath.Segment last = segments[^1];
                if (last.Name is { } name)
                {
                    parent.AsObject()[name] = replacement;
                }
                else
                {
                    parent.AsArray()[last.Position] = replacement;
                }
            }

            return PatchText.Indented(root!, 2).Replace("\n", PatchText.NewLineOf(document), StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            // A repeated key or another shape the model does not hold; the caller reports the write-back as failed.
            return null;
        }
    }

    /// <summary>Writes the first document of a YAML stream again with the edited rules.</summary>
    /// <param name="document">The document text.</param>
    /// <param name="edits">Each rule as loaded from <paramref name="document"/>, with its edit.</param>
    /// <returns>The new document, or <see langword="null"/> when the document cannot be read into a model.</returns>
    public static string? Yaml(string document, IReadOnlyList<(LoadedRule Loaded, PrintedRule Rule)> edits)
    {
        try
        {
            YamlStream stream = Load(document);
            foreach ((LoadedRule loaded, PrintedRule rule) in edits)
            {
                YamlNode replacement =
                    loaded.Format == RuleFormat.Text
                        ? new YamlScalarNode(rule.Text) { Style = ScalarStyle.DoubleQuoted }
                        : Load(rule.Yaml).Documents[0].RootNode;
                IReadOnlyList<DocumentPath.Segment> segments = SegmentsOf(loaded);
                if (segments.Count == 0)
                {
                    stream.Documents[0] = new YamlDocument(replacement);
                    continue;
                }

                YamlNode parent = segments.SkipLast(1).Aggregate(stream.Documents[0].RootNode, Child);
                DocumentPath.Segment last = segments[^1];
                if (last.Name is { } name)
                {
                    YamlMappingNode mapping = (YamlMappingNode)parent;
                    YamlNode key = mapping.Children.Keys.First(k => k is YamlScalarNode { Value: { } value } && value == name);
                    mapping.Children[key] = replacement;
                }
                else
                {
                    ((YamlSequenceNode)parent).Children[last.Position] = replacement;
                }
            }

            using StringWriter writer = new() { NewLine = "\n" };
            stream.Save(writer, assignAnchors: false);
            string text = writer.ToString().TrimEnd();
            if (text.EndsWith("\n...", StringComparison.Ordinal))
            {
                text = text[..^4];
            }

            return (text + "\n").Replace("\n", PatchText.NewLineOf(document), StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is YamlException or InvalidCastException or InvalidOperationException)
        {
            return null;
        }
    }

    private static YamlStream Load(string yaml)
    {
        YamlStream stream = [];
        using StringReader reader = new(yaml);
        stream.Load(reader);
        return stream;
    }

    private static IReadOnlyList<DocumentPath.Segment> SegmentsOf(LoadedRule loaded)
    {
        // The loader normalized the path, so it parses.
        return loaded.Path is null || !DocumentPath.TryParse(loaded.Path, out DocumentPath? path, out _) ? [] : path.Segments;
    }

    private static JsonNode Child(JsonNode node, DocumentPath.Segment segment)
    {
        return (segment.Name is { } name ? node.AsObject()[name] : node.AsArray()[segment.Position])!;
    }

    private static YamlNode Child(YamlNode node, DocumentPath.Segment segment)
    {
        return segment.Name is { } name
            ? ((YamlMappingNode)node)
                .Children.First(entry => entry.Key is YamlScalarNode { Value: { } key } && key == name)
                .Value
            : ((YamlSequenceNode)node).Children[segment.Position];
    }
}
