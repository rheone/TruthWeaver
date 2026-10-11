namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Rewriting;

/// <summary>
/// <see cref="ExpressionTools.MapChildren"/> takes its child list from <see cref="ExpressionShape"/> and rebuilds
/// each operator with its own parameters.
/// </summary>
public sealed class MapChildrenRebuildTests
{
    private static readonly TermExpression A = Term("a");
    private static readonly TermExpression B = Term("b");
    private static readonly TermExpression C = Term("c");
    private static readonly TermExpression Z = Term("z");

    private static readonly Expression[] Samples =
    [
        new NotExpression(A),
        new AndExpression(new([A, B, C])),
        new OrExpression(new([A, B, C])),
        new XorExpression(A, B),
        new EquivalentExpression(A, B),
        new NandExpression(A, B),
        new NorExpression(A, B),
        new ImpliesExpression(A, B),
        new ParityExpression(new([A, B, C])),
        new AnyExpression(new([A, B, C])),
        new AllExpression(new([A, B, C])),
        new NoneExpression(new([A, B, C])),
        new ExactlyOneExpression(new([A, B, C])),
        new CoalesceExpression(new([A, B, C])),
        new ThresholdExpression(ThresholdComparison.AtMost, 2, new([A, B, C])),
        new BetweenExpression(1, 2, new([A, B, C])),
        new InspectionExpression(InspectionKind.IsUnknown, A),
        new IfExpression(A, B, C),
    ];

    /// <summary>Gets the index of each sample so failures name the operator kind.</summary>
    public static TheoryData<int> SampleIndexes => [.. Enumerable.Range(0, Samples.Length)];

    /// <summary>An identity map returns the very same node instance for every operator kind.</summary>
    [Theory]
    [MemberData(nameof(SampleIndexes))]
    public void MapChildren_IdentityMap_ReturnsTheSameNode_Test(int index)
    {
        Expression node = Samples[index];

        Expression result = ExpressionTools.MapChildren(node, static child => child);

        Assert.Same(node, result);
    }

    /// <summary>
    /// Replacing every child rebuilds the same operator, keeps its parameters, and visits the children in operand order.
    /// </summary>
    [Theory]
    [MemberData(nameof(SampleIndexes))]
    public void MapChildren_ReplacedChildren_RebuildsSameOperatorWithNewOperands_Test(int index)
    {
        Expression node = Samples[index];
        NodeShape before = ExpressionShape.Of(node);
        List<Expression> visited = [];

        Expression result = ExpressionTools.MapChildren(
            node,
            child =>
            {
                visited.Add(child);
                return Z;
            }
        );

        NodeShape after = ExpressionShape.Of(result);
        Assert.Equal(node.GetType(), result.GetType());
        Assert.Equal(before.OpName, after.OpName);
        Assert.Equal(before.K, after.K);
        Assert.Equal(before.Max, after.Max);
        Assert.Equal(before.Operands, visited);
        Assert.All(after.Operands, operand => Assert.Same(Z, operand));
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
