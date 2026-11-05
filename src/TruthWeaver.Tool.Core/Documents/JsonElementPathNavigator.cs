namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

/// <summary>
/// The <see cref="IPathNavigator{TNode}"/> of a parsed, read-only <see cref="JsonElement"/> tree. A <see cref="JsonElement"/>
/// is a struct, so the navigator walks <see cref="JsonElementNode"/> wrappers; a replace is always refused.
/// </summary>
internal sealed class JsonElementPathNavigator : IPathNavigator<JsonElementNode>
{
    /// <summary>Gets the shared instance; the navigator holds no state.</summary>
    public static JsonElementPathNavigator Instance { get; } = new();

    /// <inheritdoc/>
    public bool TryGetChild(JsonElementNode node, DocumentPath.Segment segment, [NotNullWhen(true)] out JsonElementNode? child)
    {
        JsonElement element = node.Element;
        child = null;
        if (segment.Name is { } name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement member))
            {
                child = new JsonElementNode(member);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array && segment.Position < element.GetArrayLength())
        {
            child = new JsonElementNode(element[segment.Position]);
        }

        return child is not null;
    }

    /// <inheritdoc/>
    public bool TryReplace(JsonElementNode parent, DocumentPath.Segment segment, JsonElementNode replacement)
    {
        return false;
    }
}
