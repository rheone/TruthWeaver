namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A combinatorial sweep of <c>FORALL</c> and <c>EXISTS</c> against <see cref="K3Oracle"/>: every quantifier kind,
/// crossed with every outer operator, both operand positions, each constant outer operand (<c>t</c>, <c>f</c>, <c>u</c>)
/// and every element assignment for 0 to 3 elements. The empty collection is the vacuous case: <c>FORALL</c> is
/// <c>True</c> and <c>EXISTS</c> is <c>False</c>. <see cref="QuantifierLayerAgreementTests"/> holds the fixed list of
/// composed rules; this class is the exhaustive counterpart that the other operator families already have, and it
/// sweeps every shape that list held: the three body shapes, terms that take every K3 value, two quantifiers in one rule
/// and <c>AtLeast</c> with a threshold of 2.
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
        ("AtLeast(2, {Q}, {X}, {X})", (q, x) => K3Oracle.Cardinality(c => c >= 2, [q, x, x])),
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

    private static readonly (string Text, Func<TruthValue, TruthValue> Map)[] Bodies =
    [
        ("elementTrue", value => value),
        ("NOT elementTrue", K3Oracle.Not),
        ("IsUnknown(elementTrue)", K3Oracle.IsUnknown),
    ];

    // Operators over two quantifiers ({Q} and {R}) and two terms ({A} and {B}) that take every K3 value, with the oracle
    // value of the operator given the values of the four.
    private static readonly (
        string Template,
        Func<TruthValue, TruthValue, TruthValue, TruthValue, TruthValue> Expected
    )[] Combined =
    [
        ("If({A}, {Q}, {R})", (q, r, a, _) => K3Oracle.If(a, q, r)),
        ("{Q} NAND {R}", (q, r, _, _) => K3Oracle.Nand(q, r)),
        ("{Q} IMPLIES {R}", (q, r, _, _) => K3Oracle.Implies(q, r)),
        ("{Q} XOR {R}", (q, r, _, _) => K3Oracle.Xor(q, r)),
        ("{A} IMPLIES {Q}", (q, _, a, _) => K3Oracle.Implies(a, q)),
        ("AtLeast(2, {A}, {B}, {Q})", (q, _, a, b) => K3Oracle.Cardinality(c => c >= 2, [a, b, q])),
        ("ANY({A}, {B}, {Q})", (q, _, a, b) => K3Oracle.Any([a, b, q])),
        ("COALESCE({Q}, {R}, {A})", (q, r, a, _) => K3Oracle.Coalesce([q, r, a])),
        ("{A} OR NOT {R}", (_, r, a, _) => K3Oracle.Or([a, K3Oracle.Not(r)])),
        ("({Q} AND {R}) OR {B}", (q, r, _, b) => K3Oracle.Or([K3Oracle.And([q, r]), b])),
        ("PARITY({Q}, {R}, {A}, {B})", (q, r, a, b) => K3Oracle.Parity([q, r, a, b])),
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

    /// <summary>
    /// Every quantifier kind with each body shape (the element, its negation and <c>IsUnknown</c> of it), inside every
    /// outer operator with a term that takes every K3 value, over every element list of 0 to 3 elements, evaluates to the
    /// oracle value.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_EveryBodyAndOuterOperatorWithATermOfAnyValue_MatchesTheOracle_Test()
    {
        RuleCompiler<SweepContext> compiler = CreateSweepCompiler();
        List<string> mismatches = [];

        foreach ((string quantifier, Func<IReadOnlyList<TruthValue>, TruthValue> fold) in Quantifiers)
        {
            foreach ((string body, Func<TruthValue, TruthValue> map) in Bodies)
            {
                string quantified = $"{quantifier}(x IN items, {body})";
                foreach ((string template, Func<TruthValue, TruthValue, TruthValue> outer) in Outers)
                {
                    string text = template
                        .Replace("{Q}", quantified, StringComparison.Ordinal)
                        .Replace("{X}", "a", StringComparison.Ordinal);
                    await CollectMismatchesAsync(
                        Compile(compiler, text),
                        text,
                        mismatches,
                        context => outer(fold([.. context.Items.Select(map)]), context.A)
                    );
                }
            }
        }

        Assert.Empty(mismatches);
    }

    /// <summary>
    /// Two quantifiers in one rule, each of either kind with each body shape, combined with two terms that take every K3
    /// value in the operators that read both, over every element list of 0 to 3 elements, evaluate to the oracle value.
    /// This includes <c>AtLeast</c> with a threshold of 2 over three operands.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_TwoQuantifiersAndTwoTermsOfAnyValue_MatchesTheOracle_Test()
    {
        RuleCompiler<SweepContext> compiler = CreateSweepCompiler();
        List<string> mismatches = [];

        foreach ((string firstKind, Func<IReadOnlyList<TruthValue>, TruthValue> firstFold) in Quantifiers)
        {
            foreach ((string firstBody, Func<TruthValue, TruthValue> firstMap) in Bodies)
            {
                foreach ((string secondKind, Func<IReadOnlyList<TruthValue>, TruthValue> secondFold) in Quantifiers)
                {
                    foreach ((string secondBody, Func<TruthValue, TruthValue> secondMap) in Bodies)
                    {
                        foreach (
                            (
                                string template,
                                Func<TruthValue, TruthValue, TruthValue, TruthValue, TruthValue> outer
                            ) in Combined
                        )
                        {
                            string text = template
                                .Replace("{Q}", $"{firstKind}(x IN items, {firstBody})", StringComparison.Ordinal)
                                .Replace("{R}", $"{secondKind}(y IN items, {secondBody})", StringComparison.Ordinal)
                                .Replace("{A}", "a", StringComparison.Ordinal)
                                .Replace("{B}", "b", StringComparison.Ordinal);
                            await CollectMismatchesAsync(
                                Compile(compiler, text),
                                text,
                                mismatches,
                                context =>
                                    outer(
                                        firstFold([.. context.Items.Select(firstMap)]),
                                        secondFold([.. context.Items.Select(secondMap)]),
                                        context.A,
                                        context.B
                                    )
                            );
                        }
                    }
                }
            }
        }

        Assert.Empty(mismatches);
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

    private static async Task CollectMismatchesAsync(
        CompiledRule<SweepContext> rule,
        string text,
        List<string> mismatches,
        Func<SweepContext, TruthValue> expected
    )
    {
        // Every K3 value of the two terms, with every element list of up to MaxElements elements (including none).
        foreach (TruthValue a in K3Oracle.Values)
        {
            foreach (TruthValue b in K3Oracle.Values)
            {
                for (int count = 0; count <= MaxElements; count++)
                {
                    foreach (TruthValue[] items in K3Oracle.Assignments(count))
                    {
                        if (mismatches.Count >= MaxReported)
                        {
                            return;
                        }

                        SweepContext context = new()
                        {
                            A = a,
                            B = b,
                            Items = items,
                        };
                        Decision decision = await rule.EvaluateAsync(
                            context,
                            cancellationToken: TestContext.Current.CancellationToken
                        );
                        TruthValue want = expected(context);
                        if (decision.Result != want)
                        {
                            mismatches.Add(
                                $"{text} a={a} b={b} items=[{string.Join(",", items)}]: got {decision.Result}, oracle {want}"
                            );
                        }
                    }
                }
            }
        }
    }

    private static CompiledRule<SweepContext> Compile(RuleCompiler<SweepContext> compiler, string text)
    {
        CompilationResult<SweepContext> result = compiler.Compile(text);
        Assert.True(result.Succeeded, $"{text}: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
        return result.CompiledRule!;
    }

    private static RuleCompiler<SweepContext> CreateSweepCompiler()
    {
        PredicateRegistryBuilder<SweepContext> builder = PredicateRegistry<SweepContext>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("a", "a", "The A input."), (context, _, _) => ValueTask.FromResult(context.A))
            .Add(PredicateSchema.NoArguments("b", "b", "The B input."), (context, _, _) => ValueTask.FromResult(context.B))
            .AddCollection(
                "items",
                context => context.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "The element itself, as a K3 value."),
                        (element, _, _) => ValueTask.FromResult(element.Item)
                    )
            );
        return new RuleCompiler<SweepContext>(builder.Build());
    }

    /// <summary>The two K3 inputs and the element list that the rules read.</summary>
    private sealed class SweepContext
    {
        public TruthValue A { get; init; }

        public TruthValue B { get; init; }

        public IReadOnlyList<TruthValue> Items { get; init; } = [];
    }
}
