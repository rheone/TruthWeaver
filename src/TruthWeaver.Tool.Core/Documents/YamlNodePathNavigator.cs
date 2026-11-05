namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;
using YamlDotNet.RepresentationModel;

/// <summary>The <see cref="IPathNavigator{TNode}"/> of a <see cref="YamlNode"/> representation tree.</summary>
internal sealed class YamlNodePathNavigator : IPathNavigator<YamlNode>
{
    /// <summary>Gets the shared instance; the navigator holds no state.</summary>
    public static YamlNodePathNavigator Instance { get; } = new();

    /// <inheritdoc/>
    public bool TryGetChild(YamlNode node, DocumentPath.Segment segment, [NotNullWhen(true)] out YamlNode? child)
    {
        child = null;
        if (segment.Name is { } name)
        {
            if (node is YamlMappingNode mapping)
            {
                child = mapping.Children.FirstOrDefault(entry => IsKey(entry.Key, name)).Value;
            }
        }
        else if (node is YamlSequenceNode sequence && segment.Position < sequence.Children.Count)
        {
            child = sequence.Children[segment.Position];
        }

        return child is not null;
    }

    /// <inheritdoc/>
    public bool TryReplace(YamlNode parent, DocumentPath.Segment segment, YamlNode replacement)
    {
        if (segment.Name is { } name)
        {
            if (
                parent is not YamlMappingNode mapping
                || mapping.Children.Keys.FirstOrDefault(key => IsKey(key, name)) is not { } key
            )
            {
                return false;
            }

            mapping.Children[key] = replacement;
            return true;
        }

        if (parent is not YamlSequenceNode sequence || segment.Position >= sequence.Children.Count)
        {
            return false;
        }

        sequence.Children[segment.Position] = replacement;
        return true;
    }

    private static bool IsKey(YamlNode node, string name)
    {
        return node is YamlScalarNode { Value: { } value } && value == name;
    }
}
