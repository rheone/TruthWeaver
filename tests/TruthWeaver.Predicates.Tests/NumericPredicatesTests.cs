namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using TruthWeaver.Abstractions;

/// <summary>Behavior of the <see cref="NumericPredicates"/> family over <c>Int64</c> and <c>Decimal</c> selections.</summary>
public class NumericPredicatesTests
{
    private delegate ValueTask<TruthValue> Evaluate(Numbers context, PredicateArguments args, CancellationToken token);

    /// <summary>An ordering predicate is correct just below, at and just above the argument.</summary>
    [Theory]
    [InlineData("IsLessThan", 4, TruthValue.True)]
    [InlineData("IsLessThan", 5, TruthValue.False)]
    [InlineData("IsLessThan", 6, TruthValue.False)]
    [InlineData("IsGreaterThan", 4, TruthValue.False)]
    [InlineData("IsGreaterThan", 5, TruthValue.False)]
    [InlineData("IsGreaterThan", 6, TruthValue.True)]
    [InlineData("IsLessThanOrEqualTo", 4, TruthValue.True)]
    [InlineData("IsLessThanOrEqualTo", 5, TruthValue.True)]
    [InlineData("IsLessThanOrEqualTo", 6, TruthValue.False)]
    [InlineData("IsGreaterThanOrEqualTo", 4, TruthValue.False)]
    [InlineData("IsGreaterThanOrEqualTo", 5, TruthValue.True)]
    [InlineData("IsGreaterThanOrEqualTo", 6, TruthValue.True)]
    [InlineData("IsEqualTo", 4, TruthValue.False)]
    [InlineData("IsEqualTo", 5, TruthValue.True)]
    [InlineData("IsNotEqualTo", 4, TruthValue.True)]
    [InlineData("IsNotEqualTo", 5, TruthValue.False)]
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
    [InlineData("IsLessThan", "1.49", TruthValue.True)]
    [InlineData("IsLessThan", "1.50", TruthValue.False)]
    [InlineData("IsGreaterThanOrEqualTo", "1.50", TruthValue.True)]
    [InlineData("IsGreaterThan", "1.51", TruthValue.True)]
    [InlineData("IsEqualTo", "1.5000", TruthValue.True)]
    [InlineData("IsNotEqualTo", "1.51", TruthValue.True)]
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
    [InlineData("IsEqualTo")]
    [InlineData("IsNotEqualTo")]
    [InlineData("IsLessThan")]
    [InlineData("IsGreaterThan")]
    [InlineData("IsLessThanOrEqualTo")]
    [InlineData("IsGreaterThanOrEqualTo")]
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
    public async Task IsLessThan_NullSelectionWithFalseBehavior_ReturnsFalse_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsLessThan<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False);

        TruthValue result = await evaluate(new Numbers(null, null), Int64Arg("value", 5), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>IsBetween is inclusive at both bounds and IsNotBetween is its exact complement.</summary>
    [Theory]
    [InlineData(9, TruthValue.False, TruthValue.True)]
    [InlineData(10, TruthValue.True, TruthValue.False)]
    [InlineData(15, TruthValue.True, TruthValue.False)]
    [InlineData(20, TruthValue.True, TruthValue.False)]
    [InlineData(21, TruthValue.False, TruthValue.True)]
    public async Task IsBetweenAndIsNotBetween_Int64AtEachBoundary_AreInclusiveComplements_Test(
        long selected,
        TruthValue between,
        TruthValue outside
    )
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> betweenEval) =
            NumericPredicates.IsBetween<Numbers>("b", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outsideEval) =
            NumericPredicates.IsNotBetween<Numbers>("o", c => c.Count);
        PredicateArguments args = RangeArgs(LiteralValue.OfInt64(10), LiteralValue.OfInt64(20));

        TruthValue b = await betweenEval(new Numbers(selected, null), args, CancellationToken.None);
        TruthValue o = await outsideEval(new Numbers(selected, null), args, CancellationToken.None);

        Assert.Equal(between, b);
        Assert.Equal(outside, o);
    }

    /// <summary>IsBetween and IsNotBetween work over decimals and answer Unknown for a null selection.</summary>
    [Fact]
    public async Task IsBetweenAndIsNotBetween_Decimal_AreInclusiveAndUnknownForNull_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> betweenEval) =
            NumericPredicates.IsBetween<Numbers>("b", c => c.Price);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outsideEval) =
            NumericPredicates.IsNotBetween<Numbers>("o", c => c.Price);
        PredicateArguments args = RangeArgs(LiteralValue.OfDecimal(0.5m), LiteralValue.OfDecimal(1.5m));

        Assert.Equal(TruthValue.True, await betweenEval(new Numbers(null, 1.5m), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await outsideEval(new Numbers(null, 0.5m), args, CancellationToken.None));
        Assert.Equal(TruthValue.True, await outsideEval(new Numbers(null, 1.51m), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await betweenEval(new Numbers(null, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await outsideEval(new Numbers(null, null), args, CancellationToken.None));
    }

    /// <summary>Equal bounds are a valid single-point range.</summary>
    [Fact]
    public async Task IsBetween_EqualBounds_IsASinglePointRange_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsBetween<Numbers>("b", c => c.Count);
        PredicateArguments args = RangeArgs(LiteralValue.OfInt64(7), LiteralValue.OfInt64(7));

        Assert.Equal(TruthValue.True, await evaluate(new Numbers(7, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await evaluate(new Numbers(8, null), args, CancellationToken.None));
    }

    /// <summary>Reversed bounds are an argument error, never swapped, even when the selection is null.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsBetweenAndIsNotBetween_ReversedBounds_ThrowArgumentError_Test(bool useNotBetween)
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = useNotBetween
            ? NumericPredicates.IsNotBetween<Numbers>("r", c => c.Count)
            : NumericPredicates.IsBetween<Numbers>("r", c => c.Count);
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
    public Task IsBetween_ReversedDecimalBounds_ThrowArgumentError_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsBetween<Numbers>("r", c => c.Price);
        PredicateArguments args = RangeArgs(LiteralValue.OfDecimal(2m), LiteralValue.OfDecimal(1m));

        return Assert.ThrowsAsync<ArgumentException>(async () =>
            await evaluate(new Numbers(null, 1.5m), args, CancellationToken.None)
        );
    }

    /// <summary>
    /// The reversed-bounds message prints decimal bounds with the invariant culture, so the text does not depend on the
    /// culture of the evaluating thread.
    /// </summary>
    [Fact]
    public async Task IsBetween_ReversedDecimalBoundsUnderNonInvariantCulture_PrintsInvariantBounds_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsBetween<Numbers>("r", c => c.Price);
        PredicateArguments args = RangeArgs(LiteralValue.OfDecimal(2.5m), LiteralValue.OfDecimal(1.5m));
        CultureInfo original = CultureInfo.CurrentCulture;
        ArgumentException error;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            error = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await evaluate(new Numbers(null, 1.5m), args, CancellationToken.None)
            );
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Contains("'lower' (2.5) is greater than 'upper' (1.5)", error.Message, StringComparison.Ordinal);
    }

    /// <summary>IsIn and IsNotIn are scalar membership, complements of each other, and Unknown for null.</summary>
    [Theory]
    [InlineData(1L, TruthValue.True, TruthValue.False)]
    [InlineData(3L, TruthValue.True, TruthValue.False)]
    [InlineData(2L, TruthValue.False, TruthValue.True)]
    public async Task IsInAndIsNotIn_Int64Membership_AreComplements_Test(
        long selected,
        TruthValue inResult,
        TruthValue notInResult
    )
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inEval) =
            NumericPredicates.IsIn<Numbers>("i", c => c.Count);
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInEval) =
            NumericPredicates.IsNotIn<Numbers>("n", c => c.Count);
        PredicateArguments args = ArrayArgs("values", LiteralKind.Int64, LiteralValue.OfInt64(1), LiteralValue.OfInt64(3));

        Assert.Equal(inResult, await inEval(new Numbers(selected, null), args, CancellationToken.None));
        Assert.Equal(notInResult, await notInEval(new Numbers(selected, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await inEval(new Numbers(null, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await notInEval(new Numbers(null, null), args, CancellationToken.None));
    }

    /// <summary>Decimal membership compares by value, so 1.0 equals 1.00.</summary>
    [Fact]
    public async Task IsIn_DecimalMembership_ComparesByValue_Test()
    {
        (_, Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsIn<Numbers>("i", c => c.Price);
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
    public void IsEqualTo_IntSelectorAndDecimalSelector_BindToMatchingArgumentKind_Test()
    {
        PredicateSchema fromInt = NumericPredicates.IsEqualTo<Numbers>("i", c => c.Small).Schema;
        PredicateSchema fromLong = NumericPredicates.IsEqualTo<Numbers>("l", c => c.Count).Schema;
        PredicateSchema fromDecimal = NumericPredicates.IsEqualTo<Numbers>("d", c => c.Price).Schema;

        Assert.Equal(LiteralKind.Int64, Assert.Single(fromInt.Arguments).Type);
        Assert.Equal(LiteralKind.Int64, Assert.Single(fromLong.Arguments).Type);
        Assert.Equal(LiteralKind.Decimal, Assert.Single(fromDecimal.Arguments).Type);
    }

    /// <summary>Range schemas declare two bounds and membership schemas declare one array argument of the kind.</summary>
    [Fact]
    public void Schemas_RangeAndMembership_DeclareExpectedArguments_Test()
    {
        PredicateSchema range = NumericPredicates.IsBetween<Numbers>("b", c => c.Price).Schema;
        PredicateSchema membership = NumericPredicates.IsNotIn<Numbers>("n", c => c.Count).Schema;

        Assert.Equal(["lower", "upper"], range.Arguments.Select(a => a.Name));
        Assert.All(range.Arguments, a => Assert.Equal(LiteralKind.Decimal, a.Type));
        Assert.Equal(LiteralKind.Int64Array, Assert.Single(membership.Arguments).Type);
        Assert.False(string.IsNullOrWhiteSpace(range.Description));
    }

    /// <summary>
    /// Each <c>Int64</c> ordering twin answers the Kleene complement of its positive form just below, at and just above
    /// the argument, and for a null selection.
    /// </summary>
    /// <param name="positive">The positive ordering member.</param>
    /// <param name="twin">The twin member.</param>
    [Theory]
    [InlineData("IsLessThan", "IsNotLessThan")]
    [InlineData("IsGreaterThan", "IsNotGreaterThan")]
    [InlineData("IsLessThanOrEqualTo", "IsNotLessThanOrEqualTo")]
    [InlineData("IsGreaterThanOrEqualTo", "IsNotGreaterThanOrEqualTo")]
    public async Task OrderingTwin_Int64AroundTheArgumentAndNull_IsKleeneComplementOfPositive_Test(string positive, string twin)
    {
        foreach (long? selected in new long?[] { 4, 5, 6, null })
        {
            Numbers context = new(selected, null);
            TruthValue expected = Complement(
                await Int64Comparison(positive)(context, Int64Arg("value", 5), CancellationToken.None)
            );

            TruthValue actual = await Int64Comparison(twin)(context, Int64Arg("value", 5), CancellationToken.None);

            Assert.Equal(expected, actual);
        }
    }

    /// <summary>
    /// Each <c>Decimal</c> ordering twin answers the Kleene complement of its positive form just below, at and just
    /// above the argument, and for a null selection.
    /// </summary>
    /// <param name="positive">The positive ordering member.</param>
    /// <param name="twin">The twin member.</param>
    [Theory]
    [InlineData("IsLessThan", "IsNotLessThan")]
    [InlineData("IsGreaterThan", "IsNotGreaterThan")]
    [InlineData("IsLessThanOrEqualTo", "IsNotLessThanOrEqualTo")]
    [InlineData("IsGreaterThanOrEqualTo", "IsNotGreaterThanOrEqualTo")]
    public async Task OrderingTwin_DecimalAroundTheArgumentAndNull_IsKleeneComplementOfPositive_Test(
        string positive,
        string twin
    )
    {
        foreach (decimal? selected in new decimal?[] { 4.5m, 5.5m, 6.5m, null })
        {
            Numbers context = new(null, selected);
            TruthValue expected = Complement(
                await DecimalComparison(positive)(context, DecimalArg("value", 5.5m), CancellationToken.None)
            );

            TruthValue actual = await DecimalComparison(twin)(context, DecimalArg("value", 5.5m), CancellationToken.None);

            Assert.Equal(expected, actual);
        }
    }

    /// <summary>
    /// Registered with <c>NullBehavior.False</c>, a positive ordering member answers False for a null selection and its
    /// twin answers True, including the "or equal" members.
    /// </summary>
    /// <param name="positive">The positive ordering member.</param>
    [Theory]
    [InlineData("IsLessThan")]
    [InlineData("IsGreaterThan")]
    [InlineData("IsLessThanOrEqualTo")]
    [InlineData("IsGreaterThanOrEqualTo")]
    public async Task OrderingTwin_NullSelectionWithFalseOption_PositiveIsFalseAndTwinIsTrue_Test(string positive)
    {
        Numbers context = new(null, null);
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> positiveEvaluate = positive switch
        {
            "IsLessThan" => NumericPredicates.IsLessThan<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False).Evaluate,
            "IsGreaterThan" => NumericPredicates
                .IsGreaterThan<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
            "IsLessThanOrEqualTo" => NumericPredicates
                .IsLessThanOrEqualTo<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
            _ => NumericPredicates
                .IsGreaterThanOrEqualTo<Numbers>("p", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
        };
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> twinEvaluate = positive switch
        {
            "IsLessThan" => NumericPredicates
                .IsNotLessThan<Numbers>("t", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
            "IsGreaterThan" => NumericPredicates
                .IsNotGreaterThan<Numbers>("t", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
            "IsLessThanOrEqualTo" => NumericPredicates
                .IsNotLessThanOrEqualTo<Numbers>("t", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
            _ => NumericPredicates
                .IsNotGreaterThanOrEqualTo<Numbers>("t", c => c.Count, nullBehavior: NullBehavior.False)
                .Evaluate,
        };

        TruthValue positiveResult = await positiveEvaluate(context, Int64Arg("value", 5), CancellationToken.None);
        TruthValue twinResult = await twinEvaluate(context, Int64Arg("value", 5), CancellationToken.None);

        Assert.Equal(TruthValue.False, positiveResult);
        Assert.Equal(TruthValue.True, twinResult);
    }

    /// <summary>
    /// Registered without a label, both overloads of <c>IsBetween</c> get the label <c>Is Between</c> and both overloads of
    /// <c>IsNotBetween</c> get <c>Is Not Between</c>, the spaced form the reference pages give.
    /// </summary>
    [Fact]
    public void IsBetweenAndIsNotBetween_NoLabel_UseTheSpacedDefaultLabels_Test()
    {
        string[] between =
        [
            NumericPredicates.IsBetween<Numbers>("b", c => c.Count).Schema.Label,
            NumericPredicates.IsBetween<Numbers>("b", c => c.Price).Schema.Label,
        ];
        string[] notBetween =
        [
            NumericPredicates.IsNotBetween<Numbers>("o", c => c.Count).Schema.Label,
            NumericPredicates.IsNotBetween<Numbers>("o", c => c.Price).Schema.Label,
        ];

        Assert.All(between, label => Assert.Equal("Is Between", label));
        Assert.All(notBetween, label => Assert.Equal("Is Not Between", label));
    }

    /// <summary>A label passed to <c>IsBetween</c> or <c>IsNotBetween</c> replaces the default.</summary>
    [Fact]
    public void IsBetweenAndIsNotBetween_ExplicitLabel_KeepsTheGivenLabel_Test()
    {
        string between = NumericPredicates.IsBetween<Numbers>("b", c => c.Count, label: "Quantity In Range").Schema.Label;
        string notBetween = NumericPredicates
            .IsNotBetween<Numbers>("o", c => c.Price, label: "Price Out Of Range")
            .Schema.Label;

        Assert.Equal("Quantity In Range", between);
        Assert.Equal("Price Out Of Range", notBetween);
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

    private static Evaluate Int64Comparison(string member)
    {
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "IsEqualTo" => NumericPredicates.IsEqualTo<Numbers>("p", c => c.Count).Evaluate,
            "IsNotEqualTo" => NumericPredicates.IsNotEqualTo<Numbers>("p", c => c.Count).Evaluate,
            "IsLessThan" => NumericPredicates.IsLessThan<Numbers>("p", c => c.Count).Evaluate,
            "IsGreaterThan" => NumericPredicates.IsGreaterThan<Numbers>("p", c => c.Count).Evaluate,
            "IsLessThanOrEqualTo" => NumericPredicates.IsLessThanOrEqualTo<Numbers>("p", c => c.Count).Evaluate,
            "IsGreaterThanOrEqualTo" => NumericPredicates.IsGreaterThanOrEqualTo<Numbers>("p", c => c.Count).Evaluate,
            "IsNotLessThan" => NumericPredicates.IsNotLessThan<Numbers>("p", c => c.Count).Evaluate,
            "IsNotGreaterThan" => NumericPredicates.IsNotGreaterThan<Numbers>("p", c => c.Count).Evaluate,
            "IsNotLessThanOrEqualTo" => NumericPredicates.IsNotLessThanOrEqualTo<Numbers>("p", c => c.Count).Evaluate,
            "IsNotGreaterThanOrEqualTo" => NumericPredicates.IsNotGreaterThanOrEqualTo<Numbers>("p", c => c.Count).Evaluate,
            _ => throw new ArgumentException(member),
        };
        return new Evaluate(f);
    }

    private static Evaluate DecimalComparison(string member)
    {
        Func<Numbers, PredicateArguments, CancellationToken, ValueTask<TruthValue>> f = member switch
        {
            "IsEqualTo" => NumericPredicates.IsEqualTo<Numbers>("p", c => c.Price).Evaluate,
            "IsNotEqualTo" => NumericPredicates.IsNotEqualTo<Numbers>("p", c => c.Price).Evaluate,
            "IsLessThan" => NumericPredicates.IsLessThan<Numbers>("p", c => c.Price).Evaluate,
            "IsGreaterThan" => NumericPredicates.IsGreaterThan<Numbers>("p", c => c.Price).Evaluate,
            "IsLessThanOrEqualTo" => NumericPredicates.IsLessThanOrEqualTo<Numbers>("p", c => c.Price).Evaluate,
            "IsGreaterThanOrEqualTo" => NumericPredicates.IsGreaterThanOrEqualTo<Numbers>("p", c => c.Price).Evaluate,
            "IsNotLessThan" => NumericPredicates.IsNotLessThan<Numbers>("p", c => c.Price).Evaluate,
            "IsNotGreaterThan" => NumericPredicates.IsNotGreaterThan<Numbers>("p", c => c.Price).Evaluate,
            "IsNotLessThanOrEqualTo" => NumericPredicates.IsNotLessThanOrEqualTo<Numbers>("p", c => c.Price).Evaluate,
            "IsNotGreaterThanOrEqualTo" => NumericPredicates.IsNotGreaterThanOrEqualTo<Numbers>("p", c => c.Price).Evaluate,
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
