namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Steps from a node of a document tree to one of its children, for <see cref="DocumentPath.Locate{TNode}"/>. One
/// adapter exists for each tree type (<see cref="JsonNodePathNavigator"/>); a read-only tree refuses a replace.
/// </summary>
/// <typeparam name="TNode">The node type of the tree.</typeparam>
internal interface IPathNavigator<TNode>
    where TNode : class
{
    /// <summary>Gets the child of a node for a path segment.</summary>
    /// <param name="node">The node to step from.</param>
    /// <param name="segment">A key (a mapping member) or an index (an array item).</param>
    /// <param name="child">The child when this method returns <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the node has no such child, or is not of the kind the segment needs.</returns>
    public bool TryGetChild(TNode node, DocumentPath.Segment segment, [NotNullWhen(true)] out TNode? child);

    /// <summary>Puts another node in the place of a child.</summary>
    /// <param name="parent">The node that holds the child.</param>
    /// <param name="segment">The segment that names the child in <paramref name="parent"/>.</param>
    /// <param name="replacement">The new child.</param>
    /// <returns><see langword="false"/> when the tree is read-only or the child does not exist.</returns>
    public bool TryReplace(TNode parent, DocumentPath.Segment segment, TNode replacement);
}
