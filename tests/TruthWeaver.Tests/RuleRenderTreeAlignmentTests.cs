namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;

/// <summary>
/// <see cref="RuleRenderTree.Build(OutlineNode, TraceNode, OperatorStyle, bool)"/> zips a <see cref="OutlineNode"/>
/// and an <see cref="TraceNode"/> positionally, on the assumption that both trees were built from the
/// same operand order. This guards that invariant: a deliberately mismatched pair of trees should fail
/// loudly (debug-only) instead of silently mislabeling evaluation state.
/// </summary>
public sealed class RuleRenderTreeAlignmentTests
{
    [Fact]
    public void Build_throws_when_evaluated_children_count_does_not_match_description_operand_count()
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
            [new TraceNode("a", TruthValue.True, false, [])]
        );

        Assert.Throws<InvalidOperationException>(() => RuleRenderTree.Build(description, evaluated));
    }

    [Fact]
    public void Build_does_not_throw_when_evaluated_children_count_matches_description_operand_count()
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
            [new TraceNode("a", TruthValue.True, false, []), new TraceNode("b", TruthValue.True, false, [])]
        );

        RenderNode render = RuleRenderTree.Build(description, evaluated);

        Assert.Equal(RenderState.True, render.State);
    }
}
