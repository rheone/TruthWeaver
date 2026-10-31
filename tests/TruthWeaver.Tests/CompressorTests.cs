namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 12 (coverage-gaps-2026-10-25): <c>Compressor</c>'s <c>AtLeast</c>/<c>AtMost</c>-to-<c>BETWEEN</c> merge
/// when the pair sits inside a larger <c>AND</c> chain, not just when the pair is the chain's only two operands.
/// </summary>
public sealed class CompressorTests
{
    /// <summary>Every ordered combination of four <see cref="TruthValue"/>s, 81 in total.</summary>
    public static TheoryData<TruthValue, TruthValue, TruthValue, TruthValue> EveryAssignmentOfFour()
    {
        TruthValue[] values = [TruthValue.True, TruthValue.False, TruthValue.Unknown];
        TheoryData<TruthValue, TruthValue, TruthValue, TruthValue> data = [];
        foreach (TruthValue a in values)
        {
            foreach (TruthValue b in values)
            {
                foreach (TruthValue c in values)
                {
                    foreach (TruthValue d in values)
                    {
                        data.Add(a, b, c, d);
                    }
                }
            }
        }

        return data;
    }

    /// <summary>
    /// An <c>AtLeast</c>/<c>AtMost</c> pair over the same operands, alongside an unrelated operand in the same
    /// <c>AND</c>, compresses to the pair merged into one <c>BETWEEN</c> plus the unrelated operand preserved
    /// exactly once, rather than the direct two-operand shortcut.
    /// </summary>
    [Fact]
    public void Compress_AtLeastAtMostPairAlongsideAnUnrelatedOperand_MergesToBetweenKeepingTheOtherOperand_Test()
    {
        TermExpression a = Term("a");
        TermExpression b = Term("b");
        TermExpression c = Term("c");
        TermExpression d = Term("d");
        Expression tree = new AndExpression(
            new EquatableArray<Expression>([
                new ThresholdExpression(ThresholdComparison.AtLeast, 1, new EquatableArray<Expression>([a, b, c])),
                new ThresholdExpression(ThresholdComparison.AtMost, 2, new EquatableArray<Expression>([a, b, c])),
                d,
            ])
        );

        Expression compressed = Compressor.Compress(tree);

        Expression expected = new AndExpression(
            new EquatableArray<Expression>([new BetweenExpression(1, 2, new EquatableArray<Expression>([a, b, c])), d])
        );
        Assert.Equal(expected, compressed);
    }

    /// <summary>The compressed and pre-compression trees evaluate identically across every True/False/Unknown assignment.</summary>
    [Theory]
    [MemberData(nameof(EveryAssignmentOfFour))]
    public async Task Compress_AtLeastAtMostPairAlongsideAnUnrelatedOperand_EvaluatesIdenticallyToThePreCompressionTree_Test(
        TruthValue a,
        TruthValue b,
        TruthValue c,
        TruthValue d
    )
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddReadable("a", () => a)
            .AddReadable("b", () => b)
            .AddReadable("c", () => c)
            .AddReadable("d", () => d)
            .Build();
        CompiledRule<RuleTestContext> before = new RuleCompiler<RuleTestContext>(registry)
            .Compile("AtLeast(1, a, b, c) AND AtMost(2, a, b, c) AND d")
            .GetRuleOrThrow();
        Expression compressedTree = Compressor.Compress(before.Root);
        CompiledRule<RuleTestContext> after = new(compressedTree, registry);

        Decision beforeDecision = await before.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision afterDecision = await after.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(beforeDecision.Result, afterDecision.Result);
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }
}
