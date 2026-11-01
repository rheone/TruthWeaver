namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;
using CountTest = (
    TruthWeaver.Abstractions.PredicateSchema Schema,
    System.Func<
        TruthWeaver.Predicates.Tests.TestContext,
        TruthWeaver.Abstractions.PredicateArguments,
        System.Threading.CancellationToken,
        System.Threading.Tasks.ValueTask<TruthWeaver.Abstractions.TruthValue>
    > Evaluate
);

public class CollectionPredicatesTests
{
    /// <summary>Gets the names of the ten <c>HasCount</c> factories and their twins.</summary>
    public static TheoryData<string> CountFactories =>
        [
            "HasCountEqualTo",
            "NotHasCountEqualTo",
            "HasCountLessThan",
            "NotHasCountLessThan",
            "HasCountGreaterThan",
            "NotHasCountGreaterThan",
            "HasCountLessThanOrEqualTo",
            "NotHasCountLessThanOrEqualTo",
            "HasCountGreaterThanOrEqualTo",
            "NotHasCountGreaterThanOrEqualTo",
        ];

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

    /// <summary>
    /// A <c>null</c> element is an extra member for <c>IsSubsetOf</c> and <c>SetEquals</c>, so both answer False for
    /// <c>["admin", null]</c> against <c>["admin"]</c>. It never equals a string, so <c>Contains</c>, <c>ContainsAny</c> and
    /// <c>ContainsAll</c> answer as they do for <c>["admin"]</c>, and it makes the collection not empty.
    /// </summary>
    [Fact]
    public async Task CollectionPredicates_NullElement_IsAnExtraMemberOnlyForSubsetAndSetEquality_Test()
    {
        TestContext context = new(null, ["admin", null!]);
        PredicateArguments admin = Args("values", "admin");

        TruthValue subset = await CollectionPredicates
            .IsSubsetOf<TestContext>("p", c => c.Values)
            .Evaluate(context, admin, CancellationToken.None);
        TruthValue setEquals = await CollectionPredicates
            .SetEquals<TestContext>("p", c => c.Values)
            .Evaluate(context, admin, CancellationToken.None);
        TruthValue containsAny = await CollectionPredicates
            .ContainsAny<TestContext>("p", c => c.Values)
            .Evaluate(context, admin, CancellationToken.None);
        TruthValue containsAll = await CollectionPredicates
            .ContainsAll<TestContext>("p", c => c.Values)
            .Evaluate(context, admin, CancellationToken.None);
        TruthValue contains = await CollectionPredicates
            .Contains<TestContext>("p", c => c.Values)
            .Evaluate(
                context,
                new PredicateArguments(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfString("admin") }),
                CancellationToken.None
            );
        TruthValue isEmpty = await CollectionPredicates
            .IsEmpty<TestContext>("p", c => c.Values)
            .Evaluate(context, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.False, subset);
        Assert.Equal(TruthValue.False, setEquals);
        Assert.Equal(TruthValue.True, containsAny);
        Assert.Equal(TruthValue.True, containsAll);
        Assert.Equal(TruthValue.True, contains);
        Assert.Equal(TruthValue.False, isEmpty);
    }

    /// <summary>
    /// A negative literal count is a compile-time problem for every <c>HasCount</c> predicate: the count of a collection is
    /// never negative, so the term could only ever answer one way.
    /// </summary>
    /// <param name="factory">The factory name.</param>
    [Theory]
    [MemberData(nameof(CountFactories))]
    public void ArgumentValidator_NegativeLiteralCount_ReturnsOneProblem_Test(string factory)
    {
        PredicateSchema schema = CountFactory(factory).Schema;

        IReadOnlyList<PredicateArgumentProblem> problems = schema.ArgumentValidator!(CountArgs(-1));

        PredicateArgumentProblem problem = Assert.Single(problems);
        Assert.Contains("'count'", problem.Message, StringComparison.Ordinal);
        Assert.Contains("-1", problem.Found, StringComparison.Ordinal);
    }

    /// <summary>A count of zero or more is not a problem, and neither is a count that a data source supplies.</summary>
    /// <param name="factory">The factory name.</param>
    [Theory]
    [MemberData(nameof(CountFactories))]
    public void ArgumentValidator_NonNegativeOrDeferredCount_ReturnsNoProblem_Test(string factory)
    {
        PredicateSchema schema = CountFactory(factory).Schema;
        Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>> validate = schema.ArgumentValidator!;

        Assert.Empty(validate(CountArgs(0)));
        Assert.Empty(validate(CountArgs(3)));
        Assert.Empty(validate(PredicateArguments.Empty));
    }

    /// <summary>
    /// A negative count that reaches evaluation (from a data source) throws <see cref="ArgumentException"/>, so the engine
    /// records a fault. The check runs before the selector, so a null selected collection does not hide it.
    /// </summary>
    /// <param name="factory">The factory name.</param>
    [Theory]
    [MemberData(nameof(CountFactories))]
    public async Task Evaluate_NegativeCount_ThrowsArgumentException_Test(string factory)
    {
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate = CountFactory(
            factory
        ).Evaluate;

        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new TestContext(null, ["a"]), CountArgs(-1), CancellationToken.None)
        );
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new TestContext(null, null), CountArgs(-1), CancellationToken.None)
        );
    }

    private static CountTest CountFactory(string factory)
    {
        return factory switch
        {
            "HasCountEqualTo" => CollectionPredicates.HasCountEqualTo<TestContext>("p", c => c.Values),
            "NotHasCountEqualTo" => CollectionPredicates.NotHasCountEqualTo<TestContext>("p", c => c.Values),
            "HasCountLessThan" => CollectionPredicates.HasCountLessThan<TestContext>("p", c => c.Values),
            "NotHasCountLessThan" => CollectionPredicates.NotHasCountLessThan<TestContext>("p", c => c.Values),
            "HasCountGreaterThan" => CollectionPredicates.HasCountGreaterThan<TestContext>("p", c => c.Values),
            "NotHasCountGreaterThan" => CollectionPredicates.NotHasCountGreaterThan<TestContext>("p", c => c.Values),
            "HasCountLessThanOrEqualTo" => CollectionPredicates.HasCountLessThanOrEqualTo<TestContext>("p", c => c.Values),
            "NotHasCountLessThanOrEqualTo" => CollectionPredicates.NotHasCountLessThanOrEqualTo<TestContext>(
                "p",
                c => c.Values
            ),
            "HasCountGreaterThanOrEqualTo" => CollectionPredicates.HasCountGreaterThanOrEqualTo<TestContext>(
                "p",
                c => c.Values
            ),
            "NotHasCountGreaterThanOrEqualTo" => CollectionPredicates.NotHasCountGreaterThanOrEqualTo<TestContext>(
                "p",
                c => c.Values
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(factory), factory, "Not a HasCount factory."),
        };
    }

    private static PredicateArguments CountArgs(long count)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { ["count"] = LiteralValue.OfInt64(count) });
    }

    private static PredicateArguments Args(string name, params string[] values)
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = array });
    }
}
