namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>Behavior of the <see cref="NumericPredicates"/> family over <c>Int64</c> and <c>Decimal</c> selections.</summary>
public class NumericPredicatesTests
{
    private delegate ValueTask<TruthValue> Evaluate(Numbers context, PredicateArguments args, CancellationToken token);

    /// <summary>An ordering predicate is correct just below, at and just above the argument.</summary>
    [Theory]
    [InlineData("LessThan", 4, TruthValue.True)]
    [InlineData("LessThan", 5, TruthValue.False)]
    [InlineData("LessThan", 6, TruthValue.False)]
    [InlineData("GreaterThan", 4, TruthValue.False)]
    [InlineData("GreaterThan", 5, TruthValue.False)]
    [InlineData("GreaterThan", 6, TruthValue.True)]
    [InlineData("LessThanOrEqual", 4, TruthValue.True)]
    [InlineData("LessThanOrEqual", 5, TruthValue.True)]
    [InlineData("LessThanOrEqual", 6, TruthValue.False)]
    [InlineData("GreaterThanOrEqual", 4, TruthValue.False)]
    [InlineData("GreaterThanOrEqual", 5, TruthValue.True)]
    [InlineData("GreaterThanOrEqual", 6, TruthValue.True)]
    [InlineData("Equal", 4, TruthValue.False)]
    [InlineData("Equal", 5, TruthValue.True)]
    [InlineData("NotEqual", 4, TruthValue.True)]
    [InlineData("NotEqual", 5, TruthValue.False)]
    public async Task Comparison_Int64AtAndAroundTheArgument_ReturnsOrderedResult_Test(
        string member,
        long selected,
        TruthValue expected
    )
    {
        TruthValue result = await Int64Comparison(member)(
            new Numbers(selected, null),
            Int64Arg("value", 5),
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    /// <summary>The same members work over a decimal selection, including fractional values.</summary>
    [Theory]
    [InlineData("LessThan", "1.49", TruthValue.True)]
    [InlineData("LessThan", "1.50", TruthValue.False)]
    [InlineData("GreaterThanOrEqual", "1.50", TruthValue.True)]
    [InlineData("GreaterThan", "1.51", TruthValue.True)]
    [InlineData("Equal", "1.5000", TruthValue.True)]
    [InlineData("NotEqual", "1.51", TruthValue.True)]
    public async Task Comparison_DecimalAtAndAroundTheArgument_ReturnsOrderedResult_Test(
        string member,
        string selected,
        TruthValue expected
    )
    {
        TruthValue result = await DecimalComparison(member)(
            new Numbers(null, decimal.Parse(selected, System.Globalization.CultureInfo.InvariantCulture)),
            DecimalArg("value", 1.5m),
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    /// <summary>A null selection is Unknown by default for every comparison, range and membership member, never a fault.</summary>
    [Theory]
    [InlineData("Equal")]
    [InlineData("NotEqual")]
    [InlineData("LessThan")]
    [InlineData("GreaterThan")]
    [InlineData("LessThanOrEqual")]
    [InlineData("GreaterThanOrEqual")]
    public async Task Comparison_NullSelection_ReturnsUnknownByDefault_Test(string member)
    {
        TruthValue result = await Int64Comparison(member)(
            new Numbers(null, null),
            Int64Arg("value", 5),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>The host can choose a definite False for a null selection.</summary>
    [Fact]
    public async Task LessThan_NullSelectionWithFalseBehavior_ReturnsFalse_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.LessThan<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False);

        TruthValue result = await evaluate(new Numbers(null, null), Int64Arg("value", 5), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Between is inclusive at both bounds and Outside is its exact complement.</summary>
    [Theory]
    [InlineData(9, TruthValue.False, TruthValue.True)]
    [InlineData(10, TruthValue.True, TruthValue.False)]
    [InlineData(15, TruthValue.True, TruthValue.False)]
    [InlineData(20, TruthValue.True, TruthValue.False)]
    [InlineData(21, TruthValue.False, TruthValue.True)]
    public async Task BetweenAndOutside_Int64AtEachBoundary_AreInclusiveComplements_Test(
        long selected,
        TruthValue between,
        TruthValue outside
    )
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> betweenEval) =
            NumericPredicates.Between<Numbers>("b", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outsideEval) =
            NumericPredicates.Outside<Numbers>("o", c => c.Count);
        PredicateArguments args = RangeArgs(LiteralValue.OfInt64(10), LiteralValue.OfInt64(20));

        TruthValue b = await betweenEval(new Numbers(selected, null), args, CancellationToken.None);
        TruthValue o = await outsideEval(new Numbers(selected, null), args, CancellationToken.None);

        Assert.Equal(between, b);
        Assert.Equal(outside, o);
    }

    /// <summary>Between and Outside work over decimals and answer Unknown for a null selection.</summary>
    [Fact]
    public async Task BetweenAndOutside_Decimal_AreInclusiveAndUnknownForNull_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> betweenEval) =
            NumericPredicates.Between<Numbers>("b", c => c.Price);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outsideEval) =
            NumericPredicates.Outside<Numbers>("o", c => c.Price);
        PredicateArguments args = RangeArgs(LiteralValue.OfDecimal(0.5m), LiteralValue.OfDecimal(1.5m));

        Assert.Equal(TruthValue.True, await betweenEval(new Numbers(null, 1.5m), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await outsideEval(new Numbers(null, 0.5m), args, CancellationToken.None));
        Assert.Equal(TruthValue.True, await outsideEval(new Numbers(null, 1.51m), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await betweenEval(new Numbers(null, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await outsideEval(new Numbers(null, null), args, CancellationToken.None));
    }

    /// <summary>Equal bounds are a valid single-point range.</summary>
    [Fact]
    public async Task Between_EqualBounds_IsASinglePointRange_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.Between<Numbers>("b", c => c.Count);
        PredicateArguments args = RangeArgs(LiteralValue.OfInt64(7), LiteralValue.OfInt64(7));

        Assert.Equal(TruthValue.True, await evaluate(new Numbers(7, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await evaluate(new Numbers(8, null), args, CancellationToken.None));
    }

    /// <summary>Reversed bounds are an argument error, never swapped, even when the selection is null.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BetweenAndOutside_ReversedBounds_ThrowArgumentError_Test(bool useOutside)
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = useOutside
            ? NumericPredicates.Outside<Numbers>("r", c => c.Count)
            : NumericPredicates.Between<Numbers>("r", c => c.Count);
        PredicateArguments args = RangeArgs(LiteralValue.OfInt64(20), LiteralValue.OfInt64(10));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new Numbers(15, null), args, CancellationToken.None)
        );
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new Numbers(null, null), args, CancellationToken.None)
        );
    }

    /// <summary>Reversed decimal bounds are an argument error too.</summary>
    [Fact]
    public Task Between_ReversedDecimalBounds_ThrowArgumentError_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.Between<Numbers>("r", c => c.Price);
        PredicateArguments args = RangeArgs(LiteralValue.OfDecimal(2m), LiteralValue.OfDecimal(1m));

        return Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new Numbers(null, 1.5m), args, CancellationToken.None)
        );
    }

    /// <summary>In and NotIn are scalar membership, complements of each other, and Unknown for null.</summary>
    [Theory]
    [InlineData(1L, TruthValue.True, TruthValue.False)]
    [InlineData(3L, TruthValue.True, TruthValue.False)]
    [InlineData(2L, TruthValue.False, TruthValue.True)]
    public async Task InAndNotIn_Int64Membership_AreComplements_Test(long selected, TruthValue inResult, TruthValue notInResult)
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inEval) = NumericPredicates.In<Numbers>(
            "i",
            c => c.Count
        );
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInEval) =
            NumericPredicates.NotIn<Numbers>("n", c => c.Count);
        PredicateArguments args = ArrayArgs("values", LiteralKind.Int64, LiteralValue.OfInt64(1), LiteralValue.OfInt64(3));

        Assert.Equal(inResult, await inEval(new Numbers(selected, null), args, CancellationToken.None));
        Assert.Equal(notInResult, await notInEval(new Numbers(selected, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await inEval(new Numbers(null, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await notInEval(new Numbers(null, null), args, CancellationToken.None));
    }

    /// <summary>Decimal membership compares by value, so 1.0 equals 1.00.</summary>
    [Fact]
    public async Task In_DecimalMembership_ComparesByValue_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.In<Numbers>("i", c => c.Price);
        PredicateArguments args = ArrayArgs("values", LiteralKind.Decimal, LiteralValue.OfDecimal(1.00m));

        Assert.Equal(TruthValue.True, await evaluate(new Numbers(null, 1.0m), args, CancellationToken.None));
    }

    /// <summary>IsNull and IsNotNull are definite: null is True and False, never Unknown.</summary>
    [Fact]
    public async Task IsNullAndIsNotNull_NullAndValue_AreDefinite_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isNull) =
            NumericPredicates.IsNull<Numbers>("n", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isNotNull) =
            NumericPredicates.IsNotNull<Numbers>("nn", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> decimalIsNull) =
            NumericPredicates.IsNull<Numbers>("dn", c => c.Price);

        Assert.Equal(TruthValue.True, await isNull(new Numbers(null, null), PredicateArguments.Empty, CancellationToken.None));
        Assert.Equal(TruthValue.False, await isNull(new Numbers(0, null), PredicateArguments.Empty, CancellationToken.None));
        Assert.Equal(
            TruthValue.False,
            await isNotNull(new Numbers(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(TruthValue.True, await isNotNull(new Numbers(0, null), PredicateArguments.Empty, CancellationToken.None));
        Assert.Equal(
            TruthValue.True,
            await decimalIsNull(new Numbers(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
    }

    /// <summary>IsDefault tests for zero, IsNotDefault is its complement, and a null selection is Unknown.</summary>
    [Fact]
    public async Task IsDefaultAndIsNotDefault_ZeroNonZeroAndNull_FollowComplement_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isDefault) =
            NumericPredicates.IsDefault<Numbers>("d", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> isNotDefault) =
            NumericPredicates.IsNotDefault<Numbers>("nd", c => c.Count);

        Assert.Equal(TruthValue.True, await isDefault(new Numbers(0, null), PredicateArguments.Empty, CancellationToken.None));
        Assert.Equal(TruthValue.False, await isDefault(new Numbers(3, null), PredicateArguments.Empty, CancellationToken.None));
        Assert.Equal(
            TruthValue.Unknown,
            await isDefault(new Numbers(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await isNotDefault(new Numbers(0, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.True,
            await isNotDefault(new Numbers(3, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.Unknown,
            await isNotDefault(new Numbers(null, null), PredicateArguments.Empty, CancellationToken.None)
        );
    }

    /// <summary>
    /// A selector over an <c>int</c> property binds to the <c>Int64</c> overload, so an integer selector needs
    /// an integer literal and no cast. A decimal selection takes the decimal overload and a decimal literal.
    /// </summary>
    [Fact]
    public void Equal_IntSelectorAndDecimalSelector_BindToMatchingArgumentKind_Test()
    {
        PredicateSchema fromInt = NumericPredicates.Equal<Numbers>("i", c => c.Small).Schema;
        PredicateSchema fromLong = NumericPredicates.Equal<Numbers>("l", c => c.Count).Schema;
        PredicateSchema fromDecimal = NumericPredicates.Equal<Numbers>("d", c => c.Price).Schema;

        Assert.Equal(LiteralKind.Int64, Assert.Single(fromInt.Arguments).Type);
        Assert.Equal(LiteralKind.Int64, Assert.Single(fromLong.Arguments).Type);
        Assert.Equal(LiteralKind.Decimal, Assert.Single(fromDecimal.Arguments).Type);
    }

    /// <summary>Range schemas declare two bounds and membership schemas declare one array argument of the kind.</summary>
    [Fact]
    public void Schemas_RangeAndMembership_DeclareExpectedArguments_Test()
    {
        PredicateSchema range = NumericPredicates.Between<Numbers>("b", c => c.Price).Schema;
        PredicateSchema membership = NumericPredicates.NotIn<Numbers>("n", c => c.Count).Schema;

        Assert.Equal(["lower", "upper"], range.Arguments.Select(a => a.Name));
        Assert.All(range.Arguments, a => Assert.Equal(LiteralKind.Decimal, a.Type));
        Assert.Equal(LiteralKind.Int64Array, Assert.Single(membership.Arguments).Type);
        Assert.False(string.IsNullOrWhiteSpace(range.Description));
    }

    private static Evaluate Int64Comparison(string member)
    {
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "Equal" => NumericPredicates.Equal<Numbers>("p", c => c.Count).Evaluate,
            "NotEqual" => NumericPredicates.NotEqual<Numbers>("p", c => c.Count).Evaluate,
            "LessThan" => NumericPredicates.LessThan<Numbers>("p", c => c.Count).Evaluate,
            "GreaterThan" => NumericPredicates.GreaterThan<Numbers>("p", c => c.Count).Evaluate,
            "LessThanOrEqual" => NumericPredicates.LessThanOrEqual<Numbers>("p", c => c.Count).Evaluate,
            "GreaterThanOrEqual" => NumericPredicates.GreaterThanOrEqual<Numbers>("p", c => c.Count).Evaluate,
            _ => throw new ArgumentException(member),
        };
        return new Evaluate(f);
    }

    private static Evaluate DecimalComparison(string member)
    {
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "Equal" => NumericPredicates.Equal<Numbers>("p", c => c.Price).Evaluate,
            "NotEqual" => NumericPredicates.NotEqual<Numbers>("p", c => c.Price).Evaluate,
            "LessThan" => NumericPredicates.LessThan<Numbers>("p", c => c.Price).Evaluate,
            "GreaterThan" => NumericPredicates.GreaterThan<Numbers>("p", c => c.Price).Evaluate,
            "LessThanOrEqual" => NumericPredicates.LessThanOrEqual<Numbers>("p", c => c.Price).Evaluate,
            "GreaterThanOrEqual" => NumericPredicates.GreaterThanOrEqual<Numbers>("p", c => c.Price).Evaluate,
            _ => throw new ArgumentException(member),
        };
        return new Evaluate(f);
    }

    private static PredicateArguments Int64Arg(string name, long value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfInt64(value) });
    }

    private static PredicateArguments DecimalArg(string name, decimal value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfDecimal(value) });
    }

    private static PredicateArguments RangeArgs(LiteralValue lower, LiteralValue upper)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { ["lower"] = lower, ["upper"] = upper });
    }

    private static PredicateArguments ArrayArgs(string name, LiteralKind elementKind, params LiteralValue[] elements)
    {
        return new PredicateArguments(
            new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfArray(elementKind, elements) }
        );
    }

    private sealed record Numbers(long? Count, decimal? Price, int Small = 0);
}
