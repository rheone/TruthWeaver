namespace TruthWeaver.Tests;

using TruthWeaver.Parsing;

/// <summary>
/// The tracker that names the innermost container still open when a JSON or YAML document turns out to be malformed.
/// </summary>
public sealed class TreePathTrackerTests
{
    /// <summary>With nothing open the container path is the root.</summary>
    [Fact]
    public void ContainerPath_NothingOpen_IsTheRoot_Test()
    {
        Assert.Equal("$", new TreePathTracker().ContainerPath);
    }

    /// <summary>A scalar item counts toward the index of the next item in an array.</summary>
    [Fact]
    public void ContainerPath_ContainerAfterAScalarItem_UsesIndexOne_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: true);
        tracker.Scalar();
        tracker.Enter(isArray: true);

        Assert.Equal("$[1]", tracker.ContainerPath);
    }

    /// <summary>The first item of an array has index zero.</summary>
    [Fact]
    public void ContainerPath_ContainerAsFirstItem_UsesIndexZero_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: true);
        tracker.Enter(isArray: false);

        Assert.Equal("$[0]", tracker.ContainerPath);
    }

    /// <summary>A JSON key names the child that follows it.</summary>
    [Fact]
    public void ContainerPath_ContainerUnderAKey_NamesTheKey_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);
        tracker.Key("operands");
        tracker.Enter(isArray: true);
        tracker.Enter(isArray: false);

        Assert.Equal("$.operands[0]", tracker.ContainerPath);
    }

    /// <summary>A container that closes leaves the path of its parent.</summary>
    [Fact]
    public void ContainerPath_AfterAContainerCloses_IsTheParentContainer_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);
        tracker.Key("a");
        tracker.Enter(isArray: false);
        tracker.Exit();

        Assert.Equal("$", tracker.ContainerPath);
    }

    /// <summary>A map in a mapping that has no key yet stays at the path of the mapping.</summary>
    [Fact]
    public void ContainerPath_ContainerBeforeAnyKey_StaysAtTheParent_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);
        tracker.Enter(isArray: true);

        Assert.Equal("$", tracker.ContainerPath);
    }

    /// <summary>The events at the document root, before any container opens, are ignored.</summary>
    [Fact]
    public void Events_WithNoContainerOpen_AreIgnored_Test()
    {
        TreePathTracker tracker = new();

        tracker.Key("a");
        tracker.Exit();
        tracker.YamlScalar("a");
        tracker.YamlExit();
        tracker.Scalar();

        Assert.Equal("$", tracker.ContainerPath);
    }

    /// <summary>Closing the only open container is accepted and leaves the root.</summary>
    [Fact]
    public void YamlExit_ClosingTheOnlyContainer_LeavesTheRoot_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);

        tracker.YamlExit();

        Assert.Equal("$", tracker.ContainerPath);
    }

    /// <summary>In a YAML mapping the scalars alternate key and value, and only a key names the next child.</summary>
    [Fact]
    public void YamlScalar_KeyValueKeyValue_NamesTheLastKey_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);
        tracker.YamlScalar("a");
        tracker.YamlScalar("v");
        tracker.YamlScalar("b");
        tracker.YamlScalar("w");
        tracker.Enter(isArray: false);

        Assert.Equal("$.b", tracker.ContainerPath);
    }

    /// <summary>A container that completes a mapping value makes the next scalar a key.</summary>
    [Fact]
    public void YamlExit_CompletingAValue_MakesTheNextScalarAKey_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: false);
        tracker.YamlScalar("a");
        tracker.Enter(isArray: false);
        tracker.YamlExit();
        tracker.YamlScalar("b");
        tracker.Enter(isArray: false);

        Assert.Equal("$.b", tracker.ContainerPath);
    }

    /// <summary>A YAML sequence item advances the index and never names a key.</summary>
    [Fact]
    public void YamlScalar_InASequence_AdvancesTheIndex_Test()
    {
        TreePathTracker tracker = new();
        tracker.Enter(isArray: true);
        tracker.YamlScalar("x");
        tracker.YamlScalar("y");
        tracker.Enter(isArray: false);

        Assert.Equal("$[2]", tracker.ContainerPath);
    }
}
