namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.OptimizeOrder</c> (ticket 03) stable-sorts <c>AND</c>/<c>OR</c> operands cheapest first, by the
/// <see cref="PredicateSchema.Cost"/> hint or an opt-in measured override, so the evaluator's left-to-right
/// short-circuit reaches a deciding cheap predicate before an expensive one it can then skip.
/// </summary>
public sealed class EvaluationOrderOptimizationTests
{
    /// <summary>An <c>AND</c> whose expensive operand is written first is reordered to cheap first.</summary>
    [Fact]
    public void OptimizeOrder_AndWithExpensiveOperandFirst_SortsCheapestFirst_Test()
    {
        CompiledRule<object?> rule = Compile(
            RuleBuilder.And(RuleBuilder.Predicate("expensive"), RuleBuilder.Predicate("cheap")),
            ("expensive", 10.0),
            ("cheap", 1.0)
        );

        CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();

        Assert.Equal("cheap AND expensive", optimized.CanonicalText);
    }

    /// <summary>An <c>OR</c> whose expensive operand is written first is reordered to cheap first.</summary>
    [Fact]
    public void OptimizeOrder_OrWithExpensiveOperandFirst_SortsCheapestFirst_Test()
    {
        CompiledRule<object?> rule = Compile(
            RuleBuilder.Or(RuleBuilder.Predicate("expensive"), RuleBuilder.Predicate("cheap")),
            ("expensive", 10.0),
            ("cheap", 1.0)
        );

        CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();

        Assert.Equal("cheap OR expensive", optimized.CanonicalText);
    }

    /// <summary>No predicate in the rule sets a Cost hint, so every operand sits at the same neutral baseline and the stable sort keeps the written order.</summary>
    [Fact]
    public void OptimizeOrder_NoPredicateSetsACostHint_LeavesOperandOrderUnchanged_Test()
    {
        CompiledRule<object?> rule = Compile(RuleBuilder.And(RuleBuilder.Predicate("z"), RuleBuilder.Predicate("a")));

        CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();

        Assert.Equal(rule.CanonicalText, optimized.CanonicalText);
    }

    /// <summary><c>COALESCE</c>'s priority order, <c>IMPLIES</c>'s antecedent-first order and <c>If</c>'s condition-first order are never reordered, even when their operands have very different costs.</summary>
    [Fact]
    public void OptimizeOrder_CoalesceImpliesAndIf_KeepTheirOperandOrder_Test()
    {
        RuleBuilder coalesce = RuleBuilder.Coalesce(RuleBuilder.Predicate("expensive"), RuleBuilder.Predicate("cheap"));
        RuleBuilder implies = RuleBuilder.Implies(RuleBuilder.Predicate("expensive"), RuleBuilder.Predicate("cheap"));
        RuleBuilder ifExpr = RuleBuilder.If(
            RuleBuilder.Predicate("expensive"),
            RuleBuilder.Predicate("cheap"),
            RuleBuilder.Predicate("cheap")
        );

        foreach (RuleBuilder builder in new[] { coalesce, implies, ifExpr })
        {
            CompiledRule<object?> rule = Compile(builder, ("expensive", 10.0), ("cheap", 1.0));
            CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();
            Assert.Equal(rule.CanonicalText, optimized.CanonicalText);
        }
    }

    /// <summary>
    /// A term reachable through two paths inside the same operand (here, an <c>AND</c> of the same term with itself)
    /// counts once toward that operand's cost, matching the evaluator's per-evaluation term memoization (ADR-0002): the
    /// doubled term's operand (cost 5, not 10) sorts ahead of a single, slightly pricier term (cost 6).
    /// </summary>
    [Fact]
    public void OptimizeOrder_TermSharedWithinOneOperand_CountsOnceTowardThatOperandsCost_Test()
    {
        RuleBuilder doubledTerm = RuleBuilder.And(RuleBuilder.Predicate("x"), RuleBuilder.Predicate("x"));
        CompiledRule<object?> rule = Compile(RuleBuilder.Or(doubledTerm, RuleBuilder.Predicate("z")), ("x", 5.0), ("z", 6.0));

        CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();

        Assert.Equal("(x AND x) OR z", optimized.CanonicalText);
    }

    /// <summary>The same rule and registry give the same order on every call: no randomness, no hidden state.</summary>
    [Fact]
    public void OptimizeOrder_CalledTwiceOnTheSameRule_GivesTheSameOrderEveryTime_Test()
    {
        CompiledRule<object?> rule = Compile(
            RuleBuilder.And(
                RuleBuilder.Predicate("expensive"),
                RuleBuilder.Predicate("medium"),
                RuleBuilder.Predicate("cheap")
            ),
            ("expensive", 10.0),
            ("medium", 5.0),
            ("cheap", 1.0)
        );

        string first = rule.OptimizeOrder().GetRuleOrThrow().CanonicalText;
        string second = rule.OptimizeOrder().GetRuleOrThrow().CanonicalText;

        Assert.Equal(first, second);
    }

    /// <summary>A measured-cost override replaces the schema's static Cost hint for the predicates it names, flipping the order the static hints alone would give.</summary>
    [Fact]
    public void OptimizeOrder_MeasuredCostOverride_ReplacesTheSchemaHintForThatCall_Test()
    {
        CompiledRule<object?> rule = Compile(
            RuleBuilder.And(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b")),
            ("a", 1.0),
            ("b", 10.0)
        );

        CompiledRule<object?> defaultOrder = rule.OptimizeOrder().GetRuleOrThrow();
        Assert.Equal("a AND b", defaultOrder.CanonicalText);

        CompiledRule<object?> measuredOrder = rule.OptimizeOrder(
                new OrderOptimizationOptions(new Dictionary<string, double> { ["a"] = 100.0, ["b"] = 1.0 })
            )
            .GetRuleOrThrow();
        Assert.Equal("b AND a", measuredOrder.CanonicalText);
    }

    /// <summary>
    /// Reordering a gate rule so the deciding cheap predicate comes first lets the evaluator's short-circuit skip the
    /// expensive predicate entirely; with the expensive predicate first, it always runs.
    /// </summary>
    [Fact]
    public async Task OptimizeOrder_DecidingCheapPredicateMovedFirst_LetsShortCircuitSkipTheExpensiveOne_Test()
    {
        List<string> invocations = [];
        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        builder.Add(
            PredicateSchema.NoArguments("expensive", "expensive", "Expensive.") with
            {
                Cost = 10.0,
            },
            (_, _, _) =>
            {
                invocations.Add("expensive");
                return ValueTask.FromResult(TruthValue.True);
            }
        );
        builder.Add(
            PredicateSchema.NoArguments("decidingCheap", "decidingCheap", "Cheap, decides the AND to false.") with
            {
                Cost = 1.0,
            },
            (_, _, _) =>
            {
                invocations.Add("decidingCheap");
                return ValueTask.FromResult(TruthValue.False);
            }
        );

        CompilationResult<object?> compiled = new RuleCompiler<object?>(builder.Build()).Compile("expensive AND decidingCheap");
        CompiledRule<object?> rule = compiled.GetRuleOrThrow();
        CompiledRule<object?> optimized = rule.OptimizeOrder().GetRuleOrThrow();

        await rule.EvaluateAsync(null, EmptyServiceProvider.Instance, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(["expensive", "decidingCheap"], invocations);

        invocations.Clear();
        await optimized.EvaluateAsync(
            null,
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(["decidingCheap"], invocations);
    }

    /// <summary>OptimizeOrder is K3-sound, exactly the same size, and idempotent across a handful of mixed-cost rules.</summary>
    [Theory]
    [InlineData("a AND b AND c")]
    [InlineData("a OR b OR c")]
    [InlineData("(a AND b) OR (c AND a)")]
    [InlineData("a IMPLIES (b OR c)")]
    public void OptimizeOrder_MixedCostRules_IsSoundSameSizeAndIdempotent_Test(string text)
    {
        RewriteAssertions.AssertSound(
            Compile(text, ("a", 10.0), ("b", 1.0), ("c", 5.0)),
            r => r.OptimizeOrder(),
            RewriteExpectations.All
        );
    }

    /// <summary>The <c>Try</c> form returns true with the reordered rule.</summary>
    [Fact]
    public void TryOptimizeOrder_OnSuccess_ReturnsTrueWithTheReorderedRule_Test()
    {
        CompiledRule<object?> rule = Compile(
            RuleBuilder.And(RuleBuilder.Predicate("expensive"), RuleBuilder.Predicate("cheap")),
            ("expensive", 10.0),
            ("cheap", 1.0)
        );

        bool succeeded = rule.TryOptimizeOrder(
            out CompiledRule<object?>? optimized,
            out EquatableArray<Diagnostics.Diagnostic> diagnostics
        );

        Assert.True(succeeded);
        Assert.Equal("cheap AND expensive", optimized!.CanonicalText);
        Assert.Empty(diagnostics);
    }

    private static CompiledRule<object?> Compile(RuleBuilder builder, params (string Name, double Cost)[] costs)
    {
        PredicateRegistryBuilder<object?> registryBuilder = PredicateRegistry<object?>.CreateBuilder();
        Dictionary<string, double> costsByName = costs.ToDictionary(c => c.Name, c => c.Cost, StringComparer.OrdinalIgnoreCase);
        foreach (string name in new[] { "a", "b", "c", "expensive", "cheap", "medium", "x", "z" })
        {
            PredicateSchema schema = PredicateSchema.NoArguments(name, name, $"Test predicate '{name}'.");
            if (costsByName.TryGetValue(name, out double cost))
            {
                schema = schema with { Cost = cost };
            }

            (PredicateSchema _, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
                FakePredicates.Returning<object?>(name, true);
            registryBuilder.Add(schema, evaluate);
        }

        return builder.Compile(new RuleCompiler<object?>(registryBuilder.Build())).GetRuleOrThrow();
    }

    private static CompiledRule<object?> Compile(string text, params (string Name, double Cost)[] costs)
    {
        PredicateRegistryBuilder<object?> registryBuilder = PredicateRegistry<object?>.CreateBuilder();
        Dictionary<string, double> costsByName = costs.ToDictionary(c => c.Name, c => c.Cost, StringComparer.OrdinalIgnoreCase);
        foreach (string name in new[] { "a", "b", "c" })
        {
            PredicateSchema schema = PredicateSchema.NoArguments(name, name, $"Test predicate '{name}'.");
            if (costsByName.TryGetValue(name, out double cost))
            {
                schema = schema with { Cost = cost };
            }

            (PredicateSchema _, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
                FakePredicates.Returning<object?>(name, true);
            registryBuilder.Add(schema, evaluate);
        }

        return new RuleCompiler<object?>(registryBuilder.Build()).Compile(text).GetRuleOrThrow();
    }
}
