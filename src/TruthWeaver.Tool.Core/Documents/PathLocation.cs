namespace TruthWeaver.Tool.Core.Documents;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The result of <see cref="DocumentPath.Locate{TNode}"/>: the node a path names with its parent, or the segment at which
/// the walk stopped.
/// </summary>
/// <typeparam name="TNode">The node type of the tree.</typeparam>
internal sealed class PathLocation<TNode>
    where TNode : class
{
    private readonly IPathNavigator<TNode> navigator;

    private PathLocation(
        IPathNavigator<TNode> navigator,
        TNode? node,
        TNode? parent,
        DocumentPath.Segment? last,
        DocumentPath.Segment? stopped,
        int stoppedIndex
    )
    {
        this.navigator = navigator;
        this.Node = node;
        this.Parent = parent;
        this.Last = last;
        this.Stopped = stopped;
        this.StoppedIndex = stoppedIndex;
    }

    /// <summary>Gets the node the path names, or <see langword="null"/> when the walk stopped.</summary>
    public TNode? Node { get; }

    /// <summary>Gets the node that holds <see cref="Node"/>, or <see langword="null"/> for the root and when the walk stopped.</summary>
    public TNode? Parent { get; }

    /// <summary>Gets the segment that names <see cref="Node"/> in <see cref="Parent"/>, or <see langword="null"/> for the root.</summary>
    public DocumentPath.Segment? Last { get; }

    /// <summary>Gets the first segment that names no child, or <see langword="null"/> when the walk reached the node.</summary>
    public DocumentPath.Segment? Stopped { get; }

    /// <summary>Gets the index in the path of <see cref="Stopped"/>, or -1 when the walk reached the node.</summary>
    public int StoppedIndex { get; }

    /// <summary>Gets a value indicating whether the path names a node.</summary>
    [MemberNotNullWhen(true, nameof(Node))]
    public bool Found => this.Node is not null;

    /// <summary>Puts another node in the place of the located node, in its parent.</summary>
    /// <param name="replacement">The new node.</param>
    /// <returns>
    /// <see langword="false"/> for the root (it has no parent; the caller decides what replacing it means), when the walk
    /// stopped, or when the tree refuses the change.
    /// </returns>
    public bool Replace(TNode replacement)
    {
        return this.Parent is not null
            && this.Last is not null
            && this.navigator.TryReplace(this.Parent, this.Last, replacement);
    }

    internal static PathLocation<TNode> Reached(
        IPathNavigator<TNode> navigator,
        TNode node,
        TNode? parent,
        DocumentPath.Segment? last
    )
    {
        return new PathLocation<TNode>(navigator, node, parent, last, null, -1);
    }

    internal static PathLocation<TNode> Stop(IPathNavigator<TNode> navigator, DocumentPath.Segment stopped, int index)
    {
        return new PathLocation<TNode>(navigator, null, null, null, stopped, index);
    }
}
