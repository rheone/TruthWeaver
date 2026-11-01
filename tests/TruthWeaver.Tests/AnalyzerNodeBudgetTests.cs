namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;

/// <summary>
/// Ticket 24 (ADR-0009 decision 5): <see cref="Analyzer.Profile"/> and <see cref="Analyzer.BddNodeCount"/> each catch
/// <see cref="BddNodeBudgetExceededException"/> and return <see langword="null"/> when a tree's distinct-term count is
/// within the cap passed in, but its shared BDD would still exceed <see cref="BddManager.DefaultMaxBddNodes"/>.
/// </summary>
/// <remarks>
/// The analyzer assigns each term's BDD variable on first occurrence in the tree, so the first operand
/// of the top-level <c>AND</c> below (<c>x1 OR ... OR x17 OR y1 OR ... OR y17</c>) fixes the interleaved "every x
/// before every y" order before the second operand (<c>(x1 AND y1) OR ... OR (x17 AND y17)</c>) is built under it; that
/// order makes the second operand's reduced BDD exponential (about 2^17 nodes), past the default budget of 100,000,
/// with only 34 distinct terms (the same two shapes <see cref="BddNodeBudgetTests"/> uses, combined into one tree here
/// instead of compared as a pair, since <see cref="Analyzer.Profile"/> and <see cref="Analyzer.BddNodeCount"/> take one
/// tree, not two).
/// </remarks>
public sealed class AnalyzerNodeBudgetTests
{
    private const int PairCount = 17;
    private const int TermCap = 2 * PairCount;

    /// <summary>Returns null when the tree is within the term cap but its shared BDD would exceed the node budget.</summary>
    [Fact]
    public void Profile_TreePastTheNodeBudget_ReturnsNull_Test()
    {
        Expression tree = PathologicalTree();

        Assert.Null(Analyzer.Profile(tree, TermCap));
    }

    /// <summary>Returns null when the tree is within the term cap but its shared BDD would exceed the node budget.</summary>
    [Fact]
    public void BddNodeCount_TreePastTheNodeBudget_ReturnsNull_Test()
    {
        Expression tree = PathologicalTree();

        Assert.Null(Analyzer.BddNodeCount(tree, TermCap));
    }

    private static Expression PathologicalTree()
    {
        Expression[] xs = [.. Enumerable.Range(1, PairCount).Select(i => (Expression)Term($"x{i}"))];
        Expression[] ys = [.. Enumerable.Range(1, PairCount).Select(i => (Expression)Term($"y{i}"))];

        Expression badOrder = new OrExpression(new EquatableArray<Expression>([.. xs, .. ys]));
        Expression pairs = new OrExpression(
            new EquatableArray<Expression>([
                .. Enumerable
                    .Range(0, PairCount)
                    .Select(i => (Expression)new AndExpression(new EquatableArray<Expression>([xs[i], ys[i]]))),
            ])
        );

        return new AndExpression(new EquatableArray<Expression>([badOrder, pairs]));
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
