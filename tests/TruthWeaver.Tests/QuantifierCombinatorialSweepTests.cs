namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A combinatorial sweep of <c>FORALL</c> and <c>EXISTS</c> against <see cref="K3Oracle"/>: every quantifier kind,
/// crossed with every outer operator, both operand positions, each constant outer operand (<c>t</c>, <c>f</c>, <c>u</c>)
/// and every element assignment for 0 to 3 elements. The empty collection is the vacuous case: <c>FORALL</c> is
/// <c>True</c> and <c>EXISTS</c> is <c>False</c>. <see cref="QuantifierLayerAgreementTests"/> holds the fixed list of
/// composed rules; this class is the exhaustive counterpart that the other operator families already have.
/// </summary>
public sealed class QuantifierCombinatorialSweepTests
{
    private const int MaxElements = 3;
    private const int MaxReported = 12;

    // Each outer operator is a text template ({Q} is the quantifier, {X} is the constant operand) and the oracle value
    // of the operator given the quantifier's value and the constant's value. Both operand orders are swept.
    private static readonly (string Template, Func<TruthValue, TruthValue, TruthValue> Expected)[] Outers =
    [
        ("{Q}", (q, _) => q),
        ("NOT {Q}", (q, _) => K3Oracle.Not(q)),
        ("{Q} AND {X}", (q, x) => K3Oracle.And([q, x])),
        ("{X} AND {Q}", (q, x) => K3Oracle.And([x, q])),
        ("{Q} OR {X}", (q, x) => K3Oracle.Or([q, x])),
        ("{X} OR {Q}", (q, x) => K3Oracle.Or([x, q])),
        ("{Q} XOR {X}", (q, x) => K3Oracle.Xor(q, x)),
        ("{X} XOR {Q}", (q, x) => K3Oracle.Xor(x, q)),
        ("{Q} IMPLIES {X}", (q, x) => K3Oracle.Implies(q, x)),
        ("{X} IMPLIES {Q}", (q, x) => K3Oracle.Implies(x, q)),
        ("{Q} EQUIVALENT {X}", (q, x) => K3Oracle.Equivalent(q, x)),
        ("{X} EQUIVALENT {Q}", (q, x) => K3Oracle.Equivalent(x, q)),
        ("{Q} NAND {X}", (q, x) => K3Oracle.Nand(q, x)),
        ("{X} NAND {Q}", (q, x) => K3Oracle.Nand(x, q)),
        ("{Q} NOR {X}", (q, x) => K3Oracle.Nor(q, x)),
        ("{X} NOR {Q}", (q, x) => K3Oracle.Nor(x, q)),
        ("PARITY({Q}, {X}, {X})", (q, x) => K3Oracle.Parity([q, x, x])),
        ("If({Q}, {X}, NOT {X})", (q, x) => K3Oracle.If(q, x, K3Oracle.Not(x))),
        ("If({X}, {Q}, NOT {Q})", (q, x) => K3Oracle.If(x, q, K3Oracle.Not(q))),
        ("ANY({Q}, {X})", (q, x) => K3Oracle.Any([q, x])),
        ("ALL({Q}, {X})", (q, x) => K3Oracle.All([q, x])),
        ("NONE({Q}, {X})", (q, x) => K3Oracle.None([q, x])),
        ("AtLeast(1, {Q}, {X})", (q, x) => K3Oracle.Cardinality(c => c >= 1, [q, x])),
        ("AtMost(1, {Q}, {X})", (q, x) => K3Oracle.Cardinality(c => c <= 1, [q, x])),
        ("Exactly(1, {Q}, {X})", (q, x) => K3Oracle.Cardinality(c => c == 1, [q, x])),
        ("ExactlyOne({Q}, {X})", (q, x) => K3Oracle.ExactlyOne([q, x])),
        ("BETWEEN(1, 2, {Q}, {X}, {X})", (q, x) => K3Oracle.Between(1, 2, [q, x, x])),
        ("COALESCE({Q}, {X})", (q, x) => K3Oracle.Coalesce([q, x])),
        ("COALESCE({X}, {Q})", (q, x) => K3Oracle.Coalesce([x, q])),
        ("IsTrue({Q})", (q, _) => K3Oracle.IsTrue(q)),
        ("IsFalse({Q})", (q, _) => K3Oracle.IsFalse(q)),
        ("IsUnknown({Q})", (q, _) => K3Oracle.IsUnknown(q)),
        ("IsKnown({Q})", (q, _) => K3Oracle.IsKnown(q)),
    ];

    private static readonly (string Name, Func<IReadOnlyList<TruthValue>, TruthValue> Expected)[] Quantifiers =
    [
        ("FORALL", K3Oracle.And),
        ("EXISTS", K3Oracle.Or),
    ];

    private static readonly (string Text, TruthValue Value)[] Constants =
    [
        ("t", TruthValue.True),
        ("f", TruthValue.False),
        ("u", TruthValue.Unknown),
    ];

    /// <summary>
    /// Every quantifier kind, outer operator, operand position, constant operand and element list of 0 to 3 elements
    /// evaluates to the value the oracle gives for the quantifier's folded value inside the outer operator.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_EveryQuantifierAndOuterOperatorOverEveryElementList_MatchesTheOracle_Test()
    {
        RuleCompiler<CollectionContext> compiler = CollectionContext.CreateCompiler();
        List<string> mismatches = [];
        int swept = 0;

        foreach ((string quantifier, Func<IReadOnlyList<TruthValue>, TruthValue> fold) in Quantifiers)
        {
            string quantified = $"{quantifier}(x IN items, elementTrue)";
            foreach ((string template, Func<TruthValue, TruthValue, TruthValue> outer) in Outers)
            {
                foreach ((string constant, TruthValue constantValue) in Constants)
                {
                    string text = template
                        .Replace("{Q}", quantified, StringComparison.Ordinal)
                        .Replace("{X}", constant, StringComparison.Ordinal);
                    CompilationResult<CollectionContext> result = compiler.Compile(text);
                    Assert.True(result.Succeeded, $"{text}: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");

                    for (int count = 0; count <= MaxElements; count++)
                    {
                        foreach (TruthValue[] items in K3Oracle.Assignments(count))
                        {
                            Decision decision = await result.CompiledRule!.EvaluateAsync(
                                new CollectionContext { Items = items },
                                cancellationToken: TestContext.Current.CancellationToken
                            );
                            TruthValue want = outer(fold(items), constantValue);
                            swept++;
                            if (decision.Result != want && mismatches.Count < MaxReported)
                            {
                                mismatches.Add(
                                    $"{text} items=[{string.Join(",", items)}]: got {decision.Result}, oracle {want}"
                                );
                            }
                        }
                    }
                }
            }
        }

        Assert.Empty(mismatches);
        Assert.True(swept > 0);
    }

    /// <summary>An empty collection is the vacuous case: <c>FORALL</c> is <c>True</c> and <c>EXISTS</c> is <c>False</c>.</summary>
    [Theory]
    [InlineData("FORALL(x IN items, elementTrue)", TruthValue.True)]
    [InlineData("EXISTS(x IN items, elementTrue)", TruthValue.False)]
    public async Task EvaluateAsync_QuantifierOverNoElements_IsVacuous_Test(string text, TruthValue expected)
    {
        CompilationResult<CollectionContext> result = CollectionContext.CreateCompiler().Compile(text);

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new CollectionContext { Items = [] },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Result);
    }
}
