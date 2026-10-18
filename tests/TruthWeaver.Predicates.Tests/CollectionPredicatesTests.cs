namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

public class CollectionPredicatesTests
{
    /// <summary>SetEquals returns true for the same elements in a different order.</summary>
    [Fact]
    public async Task SetEquals_SameElementsDifferentOrder_ReturnsTrue_Test()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(
            new TestContext(null, ["b", "a", "c"]),
            Args("values", "a", "b", "c"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("setEquals", schema.Name);
    }

    /// <summary>SetEquals ignores duplicate elements and returns true.</summary>
    [Fact]
    public async Task SetEquals_DuplicatesIgnored_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(
            new TestContext(null, ["a", "a", "b"]),
            Args("values", "a", "b", "b"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>SetEquals returns false when the elements differ.</summary>
    [Fact]
    public async Task SetEquals_DifferingElements_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, ["a", "b"]), Args("values", "a", "c"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>SetEquals returns false when the elements differ only in case.</summary>
    [Fact]
    public async Task SetEquals_DifferingCase_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, ["Alice"]), Args("values", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>SetEquals returns true when both the selected and literal sets are empty.</summary>
    [Fact]
    public async Task SetEquals_EmptySelectedAndEmptyLiteral_ReturnsTrue_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, []), Args("values"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>
    /// Registered with no <c>NullBehavior</c>, <c>SetEquals</c> answers Unknown for a null selected collection, even
    /// against an empty literal.
    /// </summary>
    [Fact]
    public async Task SetEquals_NullSelectedCollection_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, null), Args("values"), CancellationToken.None);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>
    /// Under <c>NullBehavior.False</c>, <c>SetEquals</c> reads a null selected collection as the empty set, so it
    /// matches an empty literal.
    /// </summary>
    [Fact]
    public async Task SetEquals_NullSelectedCollectionWithFalseBehavior_MatchesEmptyLiteral_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values, nullBehavior: NullBehavior.False);

        TruthValue result = await evaluate(new TestContext(null, null), Args("values"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary>
    /// Under <c>NullBehavior.False</c>, <c>SetEquals</c> reads a null selected collection as the empty set, so it does
    /// not match a non-empty literal.
    /// </summary>
    [Fact]
    public async Task SetEquals_NullSelectedCollectionWithFalseBehavior_DoesNotMatchNonEmptyLiteral_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values, nullBehavior: NullBehavior.False);

        TruthValue result = await evaluate(new TestContext(null, null), Args("values", "a"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>
    /// <c>IsNullOrEmpty</c> and <c>IsNotNullOrEmpty</c> answer a definite value for null, empty and non-empty collections.
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public async Task IsNullOrEmpty_NullEmptyAndNonEmpty_AnswerDefinitely_Test(int? count, bool expectedNullOrEmpty)
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> positive) =
            CollectionPredicates.IsNullOrEmpty<TestContext>("p", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> twin) =
            CollectionPredicates.IsNotNullOrEmpty<TestContext>("t", c => c.Values);
        TestContext context = new(null, count is null ? null : Enumerable.Repeat("a", count.Value).ToArray());

        TruthValue positiveResult = await positive(context, PredicateArguments.Empty, CancellationToken.None);
        TruthValue twinResult = await twin(context, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(expectedNullOrEmpty ? TruthValue.True : TruthValue.False, positiveResult);
        Assert.Equal(expectedNullOrEmpty ? TruthValue.False : TruthValue.True, twinResult);
        Assert.Empty(schema.Arguments);
    }

    /// <summary>A selector that throws propagates the exception, so the evaluator records a fault and the answer is Unknown.</summary>
    [Fact]
    public async Task IsNullOrEmpty_FaultingSelector_Throws_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> positive) =
            CollectionPredicates.IsNullOrEmpty<TestContext>("p", _ => throw new InvalidOperationException("selector failed"));
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> twin) =
            CollectionPredicates.IsNotNullOrEmpty<TestContext>(
                "t",
                _ => throw new InvalidOperationException("selector failed")
            );

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await positive(new TestContext(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await twin(new TestContext(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
    }

    private static PredicateArguments Args(string name, params string[] values)
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = array });
    }
}
