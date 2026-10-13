namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TraceNodeTests
{
    /// <summary>An evaluated leaf carries its result and no children.</summary>
    [Fact]
    public void Construction_EvaluatedLeaf_CarriesResultAndNoChildren_Test()
    {
        TraceNode leaf = new("isManager", TruthValue.True, NotEvaluated: false, Children: []);

        Assert.Equal(TruthValue.True, leaf.Result);
        Assert.Empty(leaf.Children);
    }

    /// <summary>An interior node mirrors its operands results.</summary>
    [Fact]
    public void Construction_InteriorNode_MirrorsOperandResults_Test()
    {
        TraceNode left = new("isManager", TruthValue.True, false, []);
        TraceNode right = new("isSuspended", TruthValue.False, false, []);

        TraceNode and = new(
            "AND",
            TruthValue.False,
            NotEvaluated: false,
            Children: new EquatableArray<TraceNode>([left, right])
        );

        Assert.Equal([left, right], and.Children);
    }

    /// <summary>A short circuited node has no result and no children.</summary>
    [Fact]
    public void Construction_ShortCircuitedNode_HasNoResultOrChildren_Test()
    {
        TraceNode skipped = new("isSuspended", Result: null, NotEvaluated: true, Children: []);

        Assert.Null(skipped.Result);
        Assert.True(skipped.NotEvaluated);
        Assert.Empty(skipped.Children);
    }

    /// <summary>
    /// <see cref="TraceNode.Children"/> is an <see cref="EquatableArray{T}"/> (ticket 28), so two separately-built
    /// trees whose nodes and children are element-wise equal compare equal and share a hash code all the way down,
    /// instead of the reference equality a plain <see cref="IReadOnlyList{T}"/> member would give two different lists.
    /// </summary>
    [Fact]
    public void TraceNode_TwoTreesWithEqualChildren_AreEqual_Test()
    {
        TraceNode first = new(
            "AND",
            TruthValue.False,
            false,
            new EquatableArray<TraceNode>([
                new TraceNode("isManager", TruthValue.True, false, EquatableArray<TraceNode>.Empty),
                new TraceNode("isSuspended", TruthValue.False, false, EquatableArray<TraceNode>.Empty),
            ])
        );
        TraceNode second = new(
            "AND",
            TruthValue.False,
            false,
            new EquatableArray<TraceNode>([
                new TraceNode("isManager", TruthValue.True, false, EquatableArray<TraceNode>.Empty),
                new TraceNode("isSuspended", TruthValue.False, false, EquatableArray<TraceNode>.Empty),
            ])
        );

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
