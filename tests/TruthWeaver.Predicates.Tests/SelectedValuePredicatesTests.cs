namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

public class SelectedValuePredicatesTests
{
    /// <summary>Create with select and test builds a schema that matches the supplied values.</summary>
    [Fact]
    public async Task Create_WithSelectAndTest_SchemaMatchesSuppliedValues_Test()
    {
        (PredicateSchema schema, _) = SelectedValuePredicates.Create<TestContext, decimal>(
            "isWithinBudget",
            "Is Within Budget",
            "Is the amount within the selected limit?",
            (_, args, _) =>
                ValueTask.FromResult(decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)),
            static selected => selected >= 100m ? TruthValue.True : TruthValue.False,
            new PredicateArgumentSchema("limit", "The limit to select.", LiteralKind.String)
        );

        Assert.Equal("isWithinBudget", schema.Name);
        Assert.Equal("Is Within Budget", schema.Label);
        Assert.Equal("Is the amount within the selected limit?", schema.Description);
        Assert.Equal("limit", Assert.Single(schema.Arguments).Name);
    }

    /// <summary>Create with select and test evaluates by composing select then test.</summary>
    [Fact]
    public async Task Create_WithSelectAndTest_EvaluatesByComposingSelectThenTest_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            SelectedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the selected limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static selected => selected >= 100m ? TruthValue.True : TruthValue.False,
                new PredicateArgumentSchema("limit", "The limit to select.", LiteralKind.String)
            );

        TruthValue result = await evaluate(new TestContext(null), Args("limit", "150"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>When test rejects the selected value, the predicate evaluates to false.</summary>
    [Fact]
    public async Task Create_WithSelectAndTest_TestRejectsSelectedValue_EvaluatesFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            SelectedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the selected limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static selected => selected >= 100m ? TruthValue.True : TruthValue.False,
                new PredicateArgumentSchema("limit", "The limit to select.", LiteralKind.String)
            );

        TruthValue result = await evaluate(new TestContext(null), Args("limit", "50"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>A test delegate that cannot decide from the selected value may answer Unknown, and that answer passes through unchanged.</summary>
    [Fact]
    public async Task Create_WithSelectAndTest_TestReturnsUnknown_EvaluatesUnknown_Test()
    {
        // Arrange
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            SelectedValuePredicates.Create<TestContext, decimal?>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the selected limit?",
                (_, _, _) => ValueTask.FromResult<decimal?>(null),
                static selected => selected is null ? TruthValue.Unknown : TruthValue.True
            );

        // Act
        TruthValue result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        // Assert
        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>The single value overload of Create evaluates to exactly what select returns.</summary>
    [Fact]
    public async Task Create_SingleValueOverload_EvaluatesToExactlyWhatSelectReturns_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            SelectedValuePredicates.Create<TestContext>(
                "isFeatureEnabled",
                "Is Feature Enabled",
                "Is the given feature flag enabled?",
                (_, args, _) =>
                    ValueTask.FromResult(args.GetString("flagKey") == "enabled-flag" ? TruthValue.True : TruthValue.False),
                new PredicateArgumentSchema("flagKey", "The flag key to look up.", LiteralKind.String)
            );

        TruthValue enabledResult = await evaluate(
            new TestContext(null),
            Args("flagKey", "enabled-flag"),
            CancellationToken.None
        );
        TruthValue disabledResult = await evaluate(
            new TestContext(null),
            Args("flagKey", "other-flag"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, enabledResult);
        Assert.Equal(TruthValue.False, disabledResult);
        Assert.Equal("isFeatureEnabled", schema.Name);
    }

    /// <summary>When select throws, the exception surfaces unwrapped.</summary>
    [Fact]
    public async Task Create_WithSelectAndTest_SelectThrows_ExceptionSurfacesUnwrapped_Test()
    {
        InvalidOperationException expected = new("lookup failed");
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            SelectedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the selected limit?",
                (_, _, _) => throw expected,
                static _ => TruthValue.True,
                new PredicateArgumentSchema("limit", "The limit to select.", LiteralKind.String)
            );

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(new TestContext(null), Args("limit", "1"), CancellationToken.None)
        );
        Assert.Same(expected, actual);
    }

    /// <summary>Two predicates with the same schema and different select closures stay independent.</summary>
    [Fact]
    public async Task Create_TwoPredicatesWithSameSchemaDifferentSelectClosures_RemainIndependent_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> first) =
            SelectedValuePredicates.Create<TestContext, string>(
                "sameName",
                "Same Name",
                "Same schema, different closures.",
                (_, _, _) => ValueTask.FromResult("first"),
                static selected => selected == "first" ? TruthValue.True : TruthValue.False
            );
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> second) =
            SelectedValuePredicates.Create<TestContext, string>(
                "sameName",
                "Same Name",
                "Same schema, different closures.",
                (_, _, _) => ValueTask.FromResult("second"),
                static selected => selected == "second" ? TruthValue.True : TruthValue.False
            );

        TruthValue firstResult = await first(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);
        TruthValue secondResult = await second(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, firstResult);
        Assert.Equal(TruthValue.True, secondResult);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
