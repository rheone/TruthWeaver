namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The part of the rule a point is on: a node of the tree (by path) or a place in the editor text (by the start offset of
/// its part, from <c>InspectController.PartStartAt</c>). It has a node form and an offset form, and two targets of different
/// forms are never equal, so the hover never parses a string to tell the two apart or to compare one point to another.
/// </summary>
internal abstract record HoverTarget
{
    private HoverTarget() { }

    /// <summary>Gets the path of the tree node, or <see langword="null"/> when the target is a place in the editor text.</summary>
    public abstract string? Path { get; }

    /// <summary>Gets the start offset of the part of the editor text, or <see langword="null"/> when the target is a tree node.</summary>
    public abstract int? Offset { get; }

    /// <summary>Makes the target of a node of the tree.</summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The target.</returns>
    public static HoverTarget OfNode(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new NodeTarget(path);
    }

    /// <summary>Makes the target of a place in the editor text.</summary>
    /// <param name="offset">The start offset of the part of the editor text that covers the place.</param>
    /// <returns>The target.</returns>
    public static HoverTarget OfOffset(int offset)
    {
        return new OffsetTarget(offset);
    }

    // The node form.
    private sealed record NodeTarget(string NodePath) : HoverTarget
    {
        public override string? Path => this.NodePath;

        public override int? Offset => null;
    }

    // The offset form.
    private sealed record OffsetTarget(int PartStart) : HoverTarget
    {
        public override string? Path => null;

        public override int? Offset => this.PartStart;
    }
}
