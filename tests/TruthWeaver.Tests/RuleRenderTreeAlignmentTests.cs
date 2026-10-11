namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// <see cref="RuleRenderTree.Build(OutlineNode, TraceNode, OperatorStyle, bool, ConstantFormat?)"/> zips a <see cref="OutlineNode"/>
/// and an <see cref="TraceNode"/> positionally, on the assumption that both trees were built from the
/// same operand order. This guards that invariant: a deliberately mismatched pair of trees should fail
/// loudly (debug-only) instead of silently mislabeling evaluation state.
/// </summary>
public sealed class RuleRenderTreeAlignmentTests
{
    /// <summary>Build throws when the evaluated child count does not match the description operand count.</summary>
    [Fact]
    public void Build_ChildCountMismatch_Throws_Test()
    {
        OutlineNode description = new(
            "AND",
            "Both operands must be true.",
            [new OutlineNode("a", "a", []), new OutlineNode("b", "b", [])]
        );
        TraceNode evaluated = new(
            "AND",
            TruthValue.True,
            NotEvaluated: false,
            new EquatableArray<TraceNode>([new TraceNode("a", TruthValue.True, false, EquatableArray<TraceNode>.Empty)])
        );

        Assert.Throws<InvalidOperationException>(() => RuleRenderTree.Build(description, evaluated));
    }

    /// <summary>Build does not throw when the evaluated child count matches the description operand count.</summary>
    [Fact]
    public void Build_ChildCountMatchesOperandCount_DoesNotThrow_Test()
    {
        OutlineNode description = new(
            "AND",
            "Both operands must be true.",
            [new OutlineNode("a", "a", []), new OutlineNode("b", "b", [])]
        );
        TraceNode evaluated = new(
            "AND",
            TruthValue.True,
            NotEvaluated: false,
            new EquatableArray<TraceNode>([
                new TraceNode("a", TruthValue.True, false, EquatableArray<TraceNode>.Empty),
                new TraceNode("b", TruthValue.True, false, EquatableArray<TraceNode>.Empty),
            ])
        );

        RenderNode render = RuleRenderTree.Build(description, evaluated);

        Assert.Equal(RenderState.True, render.State);
    }
}
