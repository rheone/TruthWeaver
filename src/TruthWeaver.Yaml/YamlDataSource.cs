namespace TruthWeaver.Yaml;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.DataSources.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

/// <summary>
/// An <see cref="IDataSource"/> over a YAML document (ADR-0006 decisions 5 and 14). The document is read into the same data
/// model as JSON and queried with the same JSONPath engine as <see cref="JsonDataSource"/>, so one query gives the same result
/// against equivalent JSON and YAML, and <see cref="JsonQueryValidator"/> validates its queries at compile time.
/// </summary>
/// <remarks>
/// Scalars read as the YAML 1.2 core schema does, restricted to what JSON can express: a quoted, block or <c>!!str</c> scalar
/// is always a string; a plain <c>true</c>/<c>false</c> (any case) is a boolean; a plain number written in JSON's number
/// grammar is a number; a plain <c>null</c>, <c>~</c> or empty value is null; any other plain scalar is a string (so
/// <c>yes</c>, <c>007</c> and <c>2026-10-03</c> stay strings). A mapping key is read as its text. An alias reads as a copy of
/// its anchor. Only the first document of a stream is read.
/// </remarks>
public sealed partial class YamlDataSource : IDataSource
{
    private readonly JsonDataSource inner;

    private YamlDataSource(JsonDataSource inner)
    {
        this.inner = inner;
    }

    /// <summary>Creates a source over YAML text.</summary>
    /// <param name="yaml">The YAML document. An empty stream is a document with no data.</param>
    /// <returns>A source whose queries are absolute within the first document of <paramref name="yaml"/>.</returns>
    /// <exception cref="YamlException">Thrown when <paramref name="yaml"/> is not well-formed YAML (including duplicate mapping keys) or an alias refers to its own ancestor. The document comes from the host, not from a rule, so a malformed one is a configuration error rather than data.</exception>
    public static YamlDataSource Parse(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        YamlStream stream = [];
        using StringReader reader = new(yaml);
        stream.Load(reader);
        return stream.Documents.Count == 0
            ? new YamlDataSource(JsonDataSource.Create(null))
            : Create(stream.Documents[0].RootNode);
    }

    /// <summary>Creates a source over an already-parsed YAML node, for example one entry of a larger configuration document.</summary>
    /// <param name="root">The node to treat as the document root.</param>
    /// <returns>A source whose queries are absolute within <paramref name="root"/>.</returns>
    /// <exception cref="YamlException">Thrown when an alias refers to its own ancestor, which has no finite JSON equivalent.</exception>
    public static YamlDataSource Create(YamlNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return new YamlDataSource(
            JsonDataSource.Create(ToJson(root, new HashSet<YamlNode>(ReferenceEqualityComparer.Instance)))
        );
    }

    /// <inheritdoc />
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
    {
        return this.inner.QueryAsync(query, cancellationToken);
    }

    /// <summary>Returns a new source rooted at the single node <paramref name="query"/> matches.</summary>
    /// <param name="query">A JSONPath query that matches exactly one node.</param>
    /// <param name="cancellationToken">A token to honour.</param>
    /// <returns>The narrowed source, whose queries are absolute within the matched node.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="query"/> is not valid JSONPath.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="query"/> matches no node or more than one.</exception>
    public ValueTask<IDataSource> ScopeAsync(string query, CancellationToken cancellationToken)
    {
        return this.inner.ScopeAsync(query, cancellationToken);
    }

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex JsonNumber();

    // Converts a node to the JSON model. 'ancestors' holds the nodes being converted right now, so an alias that points back
    // at one of them (an infinite structure) is reported instead of recursing until the stack is gone. The set compares by
    // reference: YamlNode equality and hash codes are structural, and recurse forever on exactly the cyclic input being detected.
    private static JsonNode? ToJson(YamlNode node, HashSet<YamlNode> ancestors)
    {
        if (node is YamlScalarNode scalar)
        {
            return ScalarToJson(scalar);
        }

        if (!ancestors.Add(node))
        {
            throw new YamlException(
                node.Start,
                node.End,
                "An alias refers to its own ancestor, which cannot be read as JSON data."
            );
        }

        try
        {
            if (node is YamlSequenceNode sequence)
            {
                JsonArray array = [];
                foreach (YamlNode item in sequence.Children)
                {
                    array.Add(ToJson(item, ancestors));
                }

                return array;
            }

            JsonObject obj = [];
            foreach (KeyValuePair<YamlNode, YamlNode> entry in ((YamlMappingNode)node).Children)
            {
                // JSON keys are strings, so a key that is not a plain scalar is read as its text; a later duplicate wins.
                string key = entry.Key is YamlScalarNode { Value: { } text } ? text : string.Empty;
                obj[key] = ToJson(entry.Value, ancestors);
            }

            return obj;
        }
        finally
        {
            ancestors.Remove(node);
        }
    }

    private static JsonNode? ScalarToJson(YamlScalarNode scalar)
    {
        string text = scalar.Value ?? string.Empty;

        // A quoted or block scalar, or one tagged !!str, is the author's explicit string whatever it looks like.
        bool explicitString =
            scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted or ScalarStyle.Literal or ScalarStyle.Folded
            || scalar.Tag == "tag:yaml.org,2002:str";
        if (explicitString)
        {
            return JsonValue.Create(text);
        }

        if (text.Length == 0 || text == "~" || text.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(true);
        }

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(false);
        }

        // The number keeps its written text, so 18.0 stays a decimal and 18 an integer exactly as the same text in JSON would.
        return JsonNumber().IsMatch(text) ? JsonNode.Parse(text) : JsonValue.Create(text);
    }
}
