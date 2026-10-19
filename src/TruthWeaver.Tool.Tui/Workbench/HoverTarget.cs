namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The part of the rule a point is on: a node of the tree (by path) or a place in the editor text (by the start offset of
/// its part, from <c>InspectController.PartStartAt</c>). A typed value, so the hover never parses a string to tell the two
/// apart or to compare one point to another.
/// </summary>
internal readonly record struct HoverTarget
{
    private HoverTarget(string? path, int? offset)
    {
        this.Path = path;
        this.Offset = offset;
    }

    /// <summary>Gets the path of the tree node, or <see langword="null"/> when the target is a place in the editor text.</summary>
    public string? Path { get; }

    /// <summary>Gets the start offset of the part of the editor text, or <see langword="null"/> when the target is a tree node.</summary>
    public int? Offset { get; }

    /// <summary>Makes the target of a node of the tree.</summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The target.</returns>
    public static HoverTarget OfNode(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new HoverTarget(path, null);
    }

    /// <summary>Makes the target of a place in the editor text.</summary>
    /// <param name="offset">The start offset of the part of the editor text that covers the place.</param>
    /// <returns>The target.</returns>
    public static HoverTarget OfOffset(int offset)
    {
        return new HoverTarget(null, offset);
    }
}
