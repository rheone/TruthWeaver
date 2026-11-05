namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

/// <summary>The <see cref="IPathNavigator{TNode}"/> of a mutable <see cref="JsonNode"/> tree.</summary>
internal sealed class JsonNodePathNavigator : IPathNavigator<JsonNode>
{
    /// <summary>Gets the shared instance; the navigator holds no state.</summary>
    public static JsonNodePathNavigator Instance { get; } = new();

    /// <inheritdoc/>
    public bool TryGetChild(JsonNode node, DocumentPath.Segment segment, [NotNullWhen(true)] out JsonNode? child)
    {
        child = null;
        if (segment.Name is { } key)
        {
            child = (node as JsonObject)?[key];
        }
        else if (node is JsonArray items && segment.Position < items.Count)
        {
            child = items[segment.Position];
        }

        return child is not null;
    }

    /// <inheritdoc/>
    public bool TryReplace(JsonNode parent, DocumentPath.Segment segment, JsonNode replacement)
    {
        if (!this.TryGetChild(parent, segment, out _))
        {
            return false;
        }

        if (segment.Name is { } key)
        {
            ((JsonObject)parent)[key] = replacement;
        }
        else
        {
            ((JsonArray)parent)[segment.Position] = replacement;
        }

        return true;
    }
}
