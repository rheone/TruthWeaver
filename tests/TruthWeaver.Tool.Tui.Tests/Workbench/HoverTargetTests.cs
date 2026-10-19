namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// <see cref="HoverTarget"/>: the typed value the hover compares a new point against the one it rests on, so the move
/// logic never parses a string to tell a tree node from an editor offset.
/// </summary>
public sealed class HoverTargetTests
{
    /// <summary>Two targets of the same node path are equal, so resting inside the same node restarts nothing.</summary>
    [Fact]
    public void Equality_SameNodePath_AreEqual_Test()
    {
        Assert.Equal(HoverTarget.OfNode("$.operands[0]"), HoverTarget.OfNode("$.operands[0]"));
    }

    /// <summary>Two targets of the same offset are equal, so resting inside the same editor part restarts nothing.</summary>
    [Fact]
    public void Equality_SameOffset_AreEqual_Test()
    {
        Assert.Equal(HoverTarget.OfOffset(3), HoverTarget.OfOffset(3));
    }

    /// <summary>
    /// A node target and an offset target are never equal, even when the node path looks like the offset's text; the kind
    /// of target is part of its identity, not something a shared string format could blur.
    /// </summary>
    [Fact]
    public void Equality_ANodeAndAnOffset_AreNeverEqual_Test()
    {
        Assert.NotEqual(HoverTarget.OfNode("3"), HoverTarget.OfOffset(3));
    }

    /// <summary>A node target carries its path and no offset; an offset target carries its offset and no path.</summary>
    [Fact]
    public void Properties_EachKind_CarriesOnlyItsOwnValue_Test()
    {
        HoverTarget node = HoverTarget.OfNode("$");
        HoverTarget offset = HoverTarget.OfOffset(5);

        Assert.Equal("$", node.Path);
        Assert.Null(node.Offset);
        Assert.Null(offset.Path);
        Assert.Equal(5, offset.Offset);
    }
}
