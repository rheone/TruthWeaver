namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// Pins the collection family of <see cref="CollectionPredicates"/>: emptiness, membership, the
/// containment predicates, scalar <c>In</c>/<c>NotIn</c> and the count comparisons, each with its
/// Strong Kleene <c>NotX</c> twin, for a null, empty, one-item and many-item collection.
/// </summary>
public class CollectionFamilyPredicatesTests
{
    /// <summary>The emptiness tests are definite for every collection shape; a null collection counts as empty.</summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    public async Task IsEmpty_CollectionShape_ReturnsDefiniteAnswer_Test(int? shape, bool expectedEmpty)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isEmpty) =
            CollectionPredicates.IsEmpty<TestContext>("isEmpty", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isNotEmpty) =
            CollectionPredicates.IsNotEmpty<TestContext>("isNotEmpty", c => c.Values);
        TestContext context = new(null, Shape(shape));

        Assert.Equal(expectedEmpty ? TruthValue.True : TruthValue.False, await RunAsync(isEmpty, context));
        Assert.Equal(expectedEmpty ? TruthValue.False : TruthValue.True, await RunAsync(isNotEmpty, context));
    }

    /// <summary>Contains tests membership of one literal value, case-sensitively, and its twin complements it.</summary>
    [Theory]
    [InlineData(0, "a", TruthValue.False)]
    [InlineData(1, "a", TruthValue.True)]
    [InlineData(1, "A", TruthValue.False)]
    [InlineData(3, "c", TruthValue.True)]
    [InlineData(3, "z", TruthValue.False)]
    public async Task Contains_CollectionAndValue_ReturnsMembership_Test(int shape, string value, TruthValue expected)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> contains) =
            CollectionPredicates.Contains<TestContext>("contains", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notContains) =
            CollectionPredicates.NotContains<TestContext>("notContains", c => c.Values);
        TestContext context = new(null, Shape(shape));

        Assert.Equal(expected, await RunAsync(contains, context, Scalar("value", value)));
        Assert.Equal(Complement(expected), await RunAsync(notContains, context, Scalar("value", value)));
    }

    /// <summary>ContainsAny is true when at least one element is a candidate; an empty collection has none.</summary>
    [Theory]
    [InlineData(0, new[] { "a" }, TruthValue.False)]
    [InlineData(1, new[] { "a", "z" }, TruthValue.True)]
    [InlineData(3, new[] { "z" }, TruthValue.False)]
    [InlineData(3, new string[0], TruthValue.False)]
    public async Task ContainsAny_CollectionAndCandidates_ReturnsIntersection_Test(
        int shape,
        string[] candidates,
        TruthValue expected
    )
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> any) =
            CollectionPredicates.ContainsAny<TestContext>("any", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notAny) =
            CollectionPredicates.NotContainsAny<TestContext>("notAny", c => c.Values);
        TestContext context = new(null, Shape(shape));

        Assert.Equal(expected, await RunAsync(any, context, Array("values", candidates)));
        Assert.Equal(Complement(expected), await RunAsync(notAny, context, Array("values", candidates)));
    }

    /// <summary>ContainsAll is true when every candidate is an element; no candidates is vacuously true.</summary>
    [Theory]
    [InlineData(0, new[] { "a" }, TruthValue.False)]
    [InlineData(0, new string[0], TruthValue.True)]
    [InlineData(1, new[] { "a" }, TruthValue.True)]
    [InlineData(3, new[] { "a", "c" }, TruthValue.True)]
    [InlineData(3, new[] { "a", "z" }, TruthValue.False)]
    public async Task ContainsAll_CollectionAndCandidates_ReturnsSuperset_Test(
        int shape,
        string[] candidates,
        TruthValue expected
    )
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> all) =
            CollectionPredicates.ContainsAll<TestContext>("all", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notAll) =
            CollectionPredicates.NotContainsAll<TestContext>("notAll", c => c.Values);
        TestContext context = new(null, Shape(shape));

        Assert.Equal(expected, await RunAsync(all, context, Array("values", candidates)));
        Assert.Equal(Complement(expected), await RunAsync(notAll, context, Array("values", candidates)));
    }

    /// <summary>IsSubsetOf is true when every element is a candidate; an empty collection is a subset of anything.</summary>
    [Theory]
    [InlineData(0, new string[0], TruthValue.True)]
    [InlineData(1, new[] { "a", "b" }, TruthValue.True)]
    [InlineData(3, new[] { "a", "b" }, TruthValue.False)]
    [InlineData(3, new[] { "c", "b", "a", "d" }, TruthValue.True)]
    public async Task IsSubsetOf_CollectionAndCandidates_ReturnsSubset_Test(int shape, string[] candidates, TruthValue expected)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> subset) =
            CollectionPredicates.IsSubsetOf<TestContext>("subset", c => c.Values);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notSubset) =
            CollectionPredicates.IsNotSubsetOf<TestContext>("notSubset", c => c.Values);
        TestContext context = new(null, Shape(shape));

        Assert.Equal(expected, await RunAsync(subset, context, Array("values", candidates)));
        Assert.Equal(Complement(expected), await RunAsync(notSubset, context, Array("values", candidates)));
    }

    /// <summary>
    /// In and NotIn read a scalar string selector, so a collection selector does not compile: that is
    /// how a collection selector is rejected. The scalar is tested against the candidate array.
    /// </summary>
    [Theory]
    [InlineData("a", TruthValue.True)]
    [InlineData("A", TruthValue.False)]
    [InlineData("z", TruthValue.False)]
    public async Task In_ScalarSelector_ReturnsMembership_Test(string value, TruthValue expected)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isIn) =
            CollectionPredicates.In<TestContext>("in", c => c.Value);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notIn) =
            CollectionPredicates.NotIn<TestContext>("notIn", c => c.Value);
        TestContext context = new(value);

        Assert.Equal(expected, await RunAsync(isIn, context, Array("values", "a", "b")));
        Assert.Equal(Complement(expected), await RunAsync(notIn, context, Array("values", "a", "b")));
    }

    /// <summary>Each count comparison is correct below, at and above the literal count, and its twin complements it.</summary>
    [Theory]
    [InlineData("Equal", 1, 3, TruthValue.False)]
    [InlineData("Equal", 3, 3, TruthValue.True)]
    [InlineData("Equal", 0, 0, TruthValue.True)]
    [InlineData("LessThan", 2, 3, TruthValue.True)]
    [InlineData("LessThan", 3, 3, TruthValue.False)]
    [InlineData("GreaterThan", 3, 3, TruthValue.False)]
    [InlineData("GreaterThan", 3, 1, TruthValue.True)]
    [InlineData("LessThanOrEqual", 3, 3, TruthValue.True)]
    [InlineData("LessThanOrEqual", 3, 2, TruthValue.False)]
    [InlineData("GreaterThanOrEqual", 3, 3, TruthValue.True)]
    [InlineData("GreaterThanOrEqual", 1, 3, TruthValue.False)]
    public async Task Count_CollectionSizeAgainstLiteral_ReturnsComparison_Test(
        string kind,
        int size,
        long literal,
        TruthValue expected
    )
    {
        (
            Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> positive,
            Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> twin
        ) = CountPair(kind);
        TestContext context = new(null, Shape(size));
        PredicateArguments args = Scalar("count", literal);

        Assert.Equal(expected, await RunAsync(positive, context, args));
        Assert.Equal(Complement(expected), await RunAsync(twin, context, args));
    }

    /// <summary>A null collection is Unknown for the count and membership families by default, and the twins keep it Unknown.</summary>
    [Fact]
    public async Task Evaluate_NullCollectionByDefault_ReturnsUnknownForComparisonFamilies_Test()
    {
        TestContext context = new(null, null);
        List<Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>>> evaluators =
        [
            CollectionPredicates.Contains<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.NotContains<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.ContainsAny<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.NotContainsAll<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.IsSubsetOf<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.IsNotSubsetOf<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.CountEqual<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.NotCountEqual<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.CountLessThan<TestContext>("p", c => c.Values).Evaluate,
            CollectionPredicates.CountGreaterThanOrEqual<TestContext>("p", c => c.Values).Evaluate,
        ];
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["value"] = LiteralValue.OfString("a"),
                ["values"] = LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a")]),
                ["count"] = LiteralValue.OfInt64(1),
            }
        );

        foreach (Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluator in evaluators)
        {
            Assert.Equal(TruthValue.Unknown, await RunAsync(evaluator, context, args));
        }
    }

    /// <summary>A null scalar is Unknown for In and NotIn by default.</summary>
    [Fact]
    public async Task In_NullScalarByDefault_ReturnsUnknown_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isIn) =
            CollectionPredicates.In<TestContext>("in", c => c.Value);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notIn) =
            CollectionPredicates.NotIn<TestContext>("notIn", c => c.Value);

        Assert.Equal(TruthValue.Unknown, await RunAsync(isIn, new TestContext(null), Array("values", "a")));
        Assert.Equal(TruthValue.Unknown, await RunAsync(notIn, new TestContext(null), Array("values", "a")));
    }

    /// <summary>The host can choose NullBehavior.False: the positive predicate then answers a definite False.</summary>
    [Fact]
    public async Task Count_NullCollectionWithFalseOption_ReturnsFalse_Test()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> equal) =
            CollectionPredicates.CountEqual<TestContext>("countEqual", c => c.Values, nullBehavior: NullBehavior.False);

        TruthValue result = await RunAsync(equal, new TestContext(null, null), Scalar("count", 0L));

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Each new predicate carries its own schema with a description and the argument kind it reads.</summary>
    [Fact]
    public void Schema_CountContainsAnyContainsAndIn_DeclareLiteralKindsAndDescription_Test()
    {
        (PredicateSchema count, _) = CollectionPredicates.CountEqual<TestContext>("countEqual", c => c.Values);
        (PredicateSchema any, _) = CollectionPredicates.ContainsAny<TestContext>("any", c => c.Values);
        (PredicateSchema contains, _) = CollectionPredicates.Contains<TestContext>("contains", c => c.Values);
        (PredicateSchema isIn, _) = CollectionPredicates.In<TestContext>("in", c => c.Value);

        Assert.Equal(LiteralKind.Int64, Assert.Single(count.Arguments).Type);
        Assert.Equal(LiteralKind.StringArray, Assert.Single(any.Arguments).Type);
        Assert.Equal(LiteralKind.String, Assert.Single(contains.Arguments).Type);
        Assert.Equal(LiteralKind.StringArray, Assert.Single(isIn.Arguments).Type);
        Assert.All([count, any, contains, isIn], s => Assert.False(string.IsNullOrWhiteSpace(s.Description)));
    }

    private static (
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Positive,
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Twin
    ) CountPair(string kind)
    {
        return kind switch
        {
            "Equal" => (
                CollectionPredicates.CountEqual<TestContext>("p", c => c.Values).Evaluate,
                CollectionPredicates.NotCountEqual<TestContext>("t", c => c.Values).Evaluate
            ),
            "LessThan" => (
                CollectionPredicates.CountLessThan<TestContext>("p", c => c.Values).Evaluate,
                CollectionPredicates.NotCountLessThan<TestContext>("t", c => c.Values).Evaluate
            ),
            "GreaterThan" => (
                CollectionPredicates.CountGreaterThan<TestContext>("p", c => c.Values).Evaluate,
                CollectionPredicates.NotCountGreaterThan<TestContext>("t", c => c.Values).Evaluate
            ),
            "LessThanOrEqual" => (
                CollectionPredicates.CountLessThanOrEqual<TestContext>("p", c => c.Values).Evaluate,
                CollectionPredicates.NotCountLessThanOrEqual<TestContext>("t", c => c.Values).Evaluate
            ),
            _ => (
                CollectionPredicates.CountGreaterThanOrEqual<TestContext>("p", c => c.Values).Evaluate,
                CollectionPredicates.NotCountGreaterThanOrEqual<TestContext>("t", c => c.Values).Evaluate
            ),
        };
    }

    private static string[]? Shape(int? size)
    {
        return size switch
        {
            null => null,
            0 => [],
            1 => ["a"],
            2 => ["a", "b"],
            _ => ["a", "b", "c"],
        };
    }

    private static TruthValue Complement(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static ValueTask<TruthValue> RunAsync(
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluator,
        TestContext context,
        PredicateArguments? args = null
    )
    {
        return evaluator(context, args ?? PredicateArguments.Empty, CancellationToken.None);
    }

    private static PredicateArguments Scalar(string name, string value)
    {
        return new(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }

    private static PredicateArguments Scalar(string name, long value)
    {
        return new(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfInt64(value) });
    }

    private static PredicateArguments Array(string name, params string[] values)
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
        return new(new Dictionary<string, LiteralValue> { [name] = array });
    }
}
