namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The shared internal seam (<see cref="ExpressionShape.Of"/>) that every consumer walking an
/// <see cref="Expression"/> tree's operator nodes now uses instead of re-deriving op-name/K/operands
/// with its own independent switch.
/// </summary>
public sealed class ExpressionShapeTests
{
    private static readonly TermExpression TermA = new(new TermIdentity("a", []));
    private static readonly TermExpression TermB = new(new TermIdentity("b", []));

    [Fact]
    public void Not_shape_has_a_single_operand_and_no_k()
    {
        NotExpression node = new(TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Not", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA], shape.Operands);
    }

    [Fact]
    public void And_shape_carries_its_operands_in_order()
    {
        AndExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("And", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Or_shape_carries_its_operands_in_order()
    {
        OrExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Or", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Xor_shape_exposes_left_then_right_as_operands()
    {
        XorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Xor", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Equivalent</c> shape exposes its operands in left-then-right order.</summary>
    [Fact]
    public void EquivalentShape_Operands_ExposeLeftThenRight_Test()
    {
        EquivalentExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Equivalent", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Nand</c> shape exposes its operands in left-then-right order.</summary>
    [Fact]
    public void NandShape_Operands_ExposeLeftThenRight_Test()
    {
        NandExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nand", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Nor</c> shape exposes its operands in left-then-right order.</summary>
    [Fact]
    public void NorShape_Operands_ExposeLeftThenRight_Test()
    {
        NorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nor", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Implies</c> shape exposes its operands as antecedent then consequent.</summary>
    [Fact]
    public void ImpliesShape_Operands_ExposeAntecedentThenConsequent_Test()
    {
        ImpliesExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Implies", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Parity</c> shape exposes its operands in order.</summary>
    [Fact]
    public void ParityShape_Operands_AreInOrder_Test()
    {
        ParityExpression node = new(new([TermA, TermB, TermA]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Parity", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    /// <summary>The <c>Any</c> shape exposes its operands in order.</summary>
    [Fact]
    public void AnyShape_Operands_AreInOrder_Test()
    {
        AnyExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Any", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>All</c> shape exposes its operands in order.</summary>
    [Fact]
    public void AllShape_Operands_AreInOrder_Test()
    {
        AllExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("All", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>None</c> shape exposes its operands in order.</summary>
    [Fact]
    public void NoneShape_Operands_AreInOrder_Test()
    {
        NoneExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("None", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    /// <summary>The <c>Between</c> shape exposes bounds and operands in order.</summary>
    [Fact]
    public void BetweenShape_BoundsAndOperands_AreInOrder_Test()
    {
        BetweenExpression node = new(1, 2, new([TermA, TermB, TermA]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Between", shape.OpName);
        Assert.Equal(1, shape.K);
        Assert.Equal(2, shape.Max);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    /// <summary>The <c>If</c> shape exposes its operands as condition then both branches in order.</summary>
    [Fact]
    public void IfShape_Operands_AreConditionThenBothBranchesInOrder_Test()
    {
        IfExpression node = new(TermA, TermB, TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("If", shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    [Theory]
    [InlineData(InspectionKind.IsTrue, "IsTrue")]
    [InlineData(InspectionKind.IsFalse, "IsFalse")]
    [InlineData(InspectionKind.IsUnknown, "IsUnknown")]
    [InlineData(InspectionKind.IsKnown, "IsKnown")]
    public void InspectionShape_Name_MatchesKindAndCarriesOneOperand_Test(InspectionKind kind, string opName)
    {
        InspectionExpression node = new(kind, TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal(opName, shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA], shape.Operands);
    }

    /// <summary>The <c>Coalesce</c> shape exposes its operands in order.</summary>
    [Fact]
    public void CoalesceShape_Operands_AreInOrder_Test()
    {
        CoalesceExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Coalesce", shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void ExactlyOne_shape_carries_its_operands_in_order()
    {
        ExactlyOneExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("ExactlyOne", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Theory]
    [InlineData(ThresholdComparison.AtLeast)]
    [InlineData(ThresholdComparison.AtMost)]
    [InlineData(ThresholdComparison.GreaterThan)]
    [InlineData(ThresholdComparison.LessThan)]
    [InlineData(ThresholdComparison.Exactly)]
    public void Threshold_shape_op_name_matches_its_comparison_and_carries_k(ThresholdComparison comparison)
    {
        ThresholdExpression node = new(comparison, 2, new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal(comparison.ToString(), shape.OpName);
        Assert.Equal(2, shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Constant_nodes_have_no_operand_shape()
    {
        ConstantExpression node = new(TruthValue.True);

        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(node));
    }

    [Fact]
    public void Term_nodes_have_no_operand_shape()
    {
        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(TermA));
    }
}
