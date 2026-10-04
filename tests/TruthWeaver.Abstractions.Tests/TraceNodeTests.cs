namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TraceNodeTests
{
    [Fact]
    public void An_evaluated_leaf_carries_its_result_and_no_children()
    {
        TraceNode leaf = new("isManager", TruthValue.True, NotEvaluated: false, Children: []);

        Assert.Equal(TruthValue.True, leaf.Result);
        Assert.Empty(leaf.Children);
    }

    [Fact]
    public void An_interior_node_carries_its_operands_result_mirror()
    {
        TraceNode left = new("isManager", TruthValue.True, false, []);
        TraceNode right = new("isSuspended", TruthValue.False, false, []);

        TraceNode and = new("AND", TruthValue.False, NotEvaluated: false, Children: [left, right]);

        Assert.Equal([left, right], and.Children);
    }

    [Fact]
    public void A_short_circuited_node_has_no_result_and_no_children()
    {
        TraceNode skipped = new("isSuspended", Result: null, NotEvaluated: true, Children: []);

        Assert.Null(skipped.Result);
        Assert.True(skipped.NotEvaluated);
        Assert.Empty(skipped.Children);
    }
}
