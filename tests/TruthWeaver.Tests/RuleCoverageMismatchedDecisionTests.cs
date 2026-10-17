namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;

/// <summary>
/// <see cref="RuleCoverage.FromDecisions{TContext}"/> rejects a decision whose trace tree does not belong to the rule,
/// and counts quantifier bodies that hold an operator, a constant or another quantifier.
/// </summary>
public sealed class RuleCoverageMismatchedDecisionTests
{
    /// <summary>A trace node that is marked evaluated but holds no result cannot be counted, so the decision is rejected at that node.</summary>
    [Fact]
    public async Task FromDecisions_EvaluatedNodeWithoutAResult_ThrowsAtThatNode_Test()
    {
        CompiledRule<ListContext> rule = Compile("a AND b");
        Decision decision = await EvaluateAsync(rule, new ListContext());
        Decision tampered = decision with { TraceTree = decision.TraceTree! with { Result = null } };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A trace node with fewer children than the rule has operands is rejected at that node.</summary>
    [Fact]
    public async Task FromDecisions_NodeWithMissingChildren_ThrowsAtThatNode_Test()
    {
        CompiledRule<ListContext> rule = Compile("a AND b");
        Decision decision = await EvaluateAsync(rule, new ListContext());
        TraceNode root = decision.TraceTree!;
        Decision tampered = decision with
        {
            TraceTree = root with { Children = new EquatableArray<TraceNode>(root.Children.Take(1)) },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A decision whose recorded result differs from the value of its root trace node is rejected at the root.</summary>
    [Fact]
    public async Task FromDecisions_ResultDifferentFromTheRootTrace_ThrowsAtTheRoot_Test()
    {
        CompiledRule<ListContext> rule = Compile("a AND b");
        Decision decision = await EvaluateAsync(rule, new ListContext());
        Decision tampered = decision with { Result = TruthValue.False };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>An operator whose recorded value does not follow from its operands is rejected, because the decision cannot be from this rule.</summary>
    [Fact]
    public async Task FromDecisions_OperatorValueNotFollowingFromItsOperands_ThrowsAtThatOperator_Test()
    {
        CompiledRule<ListContext> rule = Compile("a AND b");
        Decision decision = await EvaluateAsync(rule, new ListContext());
        Decision tampered = decision with
        {
            Result = TruthValue.False,
            TraceTree = decision.TraceTree! with { Result = TruthValue.False },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A quantifier element whose trace holds no single body evaluation is rejected at the quantifier.</summary>
    [Fact]
    public async Task FromDecisions_QuantifierElementWithoutABodyTrace_ThrowsAtTheQuantifier_Test()
    {
        CompiledRule<ListContext> rule = Compile("FORALL(x IN items, elementTrue)");
        Decision decision = await EvaluateAsync(rule, new ListContext { Items = [TruthValue.True] });
        TraceNode root = decision.TraceTree!;
        TraceNode emptyElement = root.Children[0] with { Children = EquatableArray<TraceNode>.Empty };
        Decision tampered = decision with
        {
            TraceTree = root with { Children = new EquatableArray<TraceNode>([emptyElement]) },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A quantifier body operator whose recorded value does not follow from its operands is rejected at that operator.</summary>
    [Fact]
    public async Task FromDecisions_QuantifierBodyOperatorValueNotFollowingFromItsOperands_ThrowsAtThatOperator_Test()
    {
        CompiledRule<ListContext> rule = Compile("FORALL(x IN items, elementTrue AND elementTrue)");
        Decision decision = await EvaluateAsync(rule, new ListContext { Items = [TruthValue.True] });
        TraceNode root = decision.TraceTree!;
        TraceNode element = root.Children[0];
        TraceNode wrongBody = element.Children[0] with { Result = TruthValue.False };
        Decision tampered = decision with
        {
            TraceTree = root with
            {
                Children = new EquatableArray<TraceNode>([
                    element with
                    {
                        Children = new EquatableArray<TraceNode>([wrongBody]),
                    },
                ]),
            },
        };

        ArgumentException failure = Assert.Throws<ArgumentException>(() => RuleCoverage.FromDecisions(rule, [tampered]));

        Assert.Contains("'$.operands[0]'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A quantifier body that holds an operator and a constant is counted per element, and an operand that decides the body is marked decided.</summary>
    [Fact]
    public async Task FromDecisions_QuantifierBodyWithOperatorAndConstant_CountsEachNodePerElement_Test()
    {
        CompiledRule<ListContext> rule = Compile("FORALL(x IN items, True AND elementTrue)");
        Decision decision = await EvaluateAsync(
            rule,
            new ListContext { Items = [TruthValue.True, TruthValue.False, TruthValue.Unknown] }
        );

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        // The FORALL stops at the False element, so the Unknown element is never evaluated.
        NodeCoverage and = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[0]");
        Assert.Equal(
            (2, 1, 1, 1, 0),
            (and.EvaluatedCount, and.NotEvaluatedCount, and.TrueCount, and.FalseCount, and.UnknownCount)
        );
        NodeCoverage constant = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[0].operands[0]");
        Assert.Equal((2, 1, 2), (constant.EvaluatedCount, constant.NotEvaluatedCount, constant.TrueCount));
        NodeCoverage element = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[0].operands[1]");
        Assert.Equal(2, element.EvaluatedCount);
        Assert.Equal(2, element.DecidedCount);
    }

    /// <summary>A quantifier nested in the body of another is counted per outer element without being rejected.</summary>
    [Fact]
    public async Task FromDecisions_QuantifierNestedInAQuantifierBody_CountsBothLevels_Test()
    {
        PredicateRegistryBuilder<NestedContext> builder = PredicateRegistry<NestedContext>
            .CreateBuilder()
            .AddCollection(
                "groups",
                ctx => ctx.Groups,
                groupElements =>
                    groupElements.AddCollection(
                        "items",
                        group => group.Item,
                        itemElements =>
                            itemElements.Add(
                                PredicateSchema.NoArguments(
                                    "elementTrue",
                                    "elementTrue",
                                    "True iff the element itself is True."
                                ),
                                (e, _, _) => ValueTask.FromResult(e.Item)
                            )
                    )
            );
        CompiledRule<NestedContext> rule = new RuleCompiler<NestedContext>(builder.Build())
            .Compile("FORALL(g IN groups, EXISTS(x IN items, elementTrue))")
            .GetRuleOrThrow();
        Decision decision = await rule.EvaluateAsync(
            new NestedContext
            {
                Groups =
                [
                    [TruthValue.False, TruthValue.True],
                    [TruthValue.True],
                ],
            },
            cancellationToken: TestContext.Current.CancellationToken
        );

        RuleCoverage coverage = RuleCoverage.FromDecisions(rule, [decision]);

        Assert.Equal(1, coverage.TrueCount);
        NodeCoverage inner = Assert.Single(coverage.Nodes, node => node.Path == "$.operands[0]");
        Assert.Equal(2, inner.EvaluatedCount);
    }

    private static Task<Decision> EvaluateAsync(CompiledRule<ListContext> rule, ListContext context)
    {
        return rule.EvaluateAsync(context, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static CompiledRule<ListContext> Compile(string text)
    {
        PredicateRegistryBuilder<ListContext> builder = PredicateRegistry<ListContext>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("a", "a", "Always true."), (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .Add(PredicateSchema.NoArguments("b", "b", "Always true."), (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .AddCollection(
                "items",
                ctx => ctx.Items,
                elements =>
                    elements.Add(
                        PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                        (e, _, _) => ValueTask.FromResult(e.Item)
                    )
            );
        return new RuleCompiler<ListContext>(builder.Build()).Compile(text).GetRuleOrThrow();
    }

    /// <summary>A context holding a list of K3 values that the element predicate reads back.</summary>
    private sealed class ListContext
    {
        public IReadOnlyList<TruthValue> Items { get; init; } = [];
    }

    /// <summary>A context holding groups of K3 values for the nested quantifier test.</summary>
    private sealed class NestedContext
    {
        public IReadOnlyList<IReadOnlyList<TruthValue>> Groups { get; init; } = [];
    }
}
