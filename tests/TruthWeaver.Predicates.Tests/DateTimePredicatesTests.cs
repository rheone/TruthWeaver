namespace TruthWeaver.Predicates.Tests;

using System.Globalization;
using TruthWeaver.Abstractions;

/// <summary>
/// Pins <see cref="DateTimePredicates"/>: <c>After</c>, <c>Before</c> and <c>Between</c> over
/// <see cref="DateTimeOffset"/> literals, with the <c>NotAfter</c>, <c>NotBefore</c> and <c>Outside</c>
/// Strong Kleene twins.
/// </summary>
public class DateTimePredicatesTests
{
    private static readonly DateTimeOffset Bound = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);

    /// <summary>After is a strict greater-than at, just before and just after the bound; its twin complements it.</summary>
    [Theory]
    [InlineData(-1, TruthValue.False)]
    [InlineData(0, TruthValue.False)]
    [InlineData(1, TruthValue.True)]
    public async Task After_TicksAroundBound_IsStrict_Test(long offsetTicks, TruthValue expected)
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> after) =
            DateTimePredicates.After<DateContext>("after", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notAfter) =
            DateTimePredicates.NotAfter<DateContext>("notAfter", c => c.When);
        DateContext context = new(Bound.AddTicks(offsetTicks));
        PredicateArguments args = Single("value", Bound);

        Assert.Equal(expected, await RunAsync(after, context, args));
        Assert.Equal(Complement(expected), await RunAsync(notAfter, context, args));
    }

    /// <summary>Before is a strict less-than at, just before and just after the bound; its twin complements it.</summary>
    [Theory]
    [InlineData(-1, TruthValue.True)]
    [InlineData(0, TruthValue.False)]
    [InlineData(1, TruthValue.False)]
    public async Task Before_TicksAroundBound_IsStrict_Test(long offsetTicks, TruthValue expected)
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> before) =
            DateTimePredicates.Before<DateContext>("before", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notBefore) =
            DateTimePredicates.NotBefore<DateContext>("notBefore", c => c.When);
        DateContext context = new(Bound.AddTicks(offsetTicks));
        PredicateArguments args = Single("value", Bound);

        Assert.Equal(expected, await RunAsync(before, context, args));
        Assert.Equal(Complement(expected), await RunAsync(notBefore, context, args));
    }

    /// <summary>Comparison is by instant, so the same moment written with another offset is equal, not after or before.</summary>
    [Fact]
    public async Task After_SameInstantInOtherOffset_ReturnsFalse_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> after) =
            DateTimePredicates.After<DateContext>("after", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> before) =
            DateTimePredicates.Before<DateContext>("before", c => c.When);
        DateContext sameInstant = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.FromHours(2)));
        DateContext laterByWallClockOnly = new(new DateTimeOffset(2026, 10, 4, 11, 0, 0, TimeSpan.FromHours(2)));

        Assert.Equal(TruthValue.False, await RunAsync(after, sameInstant, Single("value", Bound)));
        Assert.Equal(TruthValue.False, await RunAsync(before, sameInstant, Single("value", Bound)));
        Assert.Equal(TruthValue.True, await RunAsync(before, laterByWallClockOnly, Single("value", Bound)));
    }

    /// <summary>Between is inclusive on both bounds, and Outside is its exact complement.</summary>
    [Theory]
    [InlineData(-1, TruthValue.False)]
    [InlineData(0, TruthValue.True)]
    [InlineData(500, TruthValue.True)]
    [InlineData(1000, TruthValue.True)]
    [InlineData(1001, TruthValue.False)]
    public async Task Between_TicksAroundBounds_IsInclusive_Test(long offsetTicks, TruthValue expected)
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> between) =
            DateTimePredicates.Between<DateContext>("between", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outside) =
            DateTimePredicates.Outside<DateContext>("outside", c => c.When);
        DateContext context = new(Bound.AddTicks(offsetTicks));
        PredicateArguments args = Range(Bound, Bound.AddTicks(1000));

        Assert.Equal(expected, await RunAsync(between, context, args));
        Assert.Equal(Complement(expected), await RunAsync(outside, context, args));
    }

    /// <summary>Between compares instants across offsets.</summary>
    [Fact]
    public async Task Between_BoundsInOtherOffsets_ComparesInstants_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> between) =
            DateTimePredicates.Between<DateContext>("between", c => c.When);
        DateTimeOffset lower = new(2026, 10, 4, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset upper = new(2026, 10, 4, 5, 0, 0, TimeSpan.FromHours(-5));

        TruthValue atLower = await RunAsync(between, new DateContext(Bound), Range(lower, upper));
        TruthValue atUpper = await RunAsync(between, new DateContext(upper), Range(lower, upper));

        Assert.Equal(TruthValue.True, atLower);
        Assert.Equal(TruthValue.True, atUpper);
    }

    /// <summary>Reversed bounds are an authoring error: the predicate throws and never swaps the bounds.</summary>
    [Fact]
    public async Task Between_ReversedBounds_ThrowsArgumentException_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> between) =
            DateTimePredicates.Between<DateContext>("between", c => c.When);
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outside) =
            DateTimePredicates.Outside<DateContext>("outside", c => c.When);
        PredicateArguments reversed = Range(Bound.AddDays(1), Bound);

        await Assert.ThrowsAsync<ArgumentException>(async () => await RunAsync(between, new DateContext(Bound), reversed));
        await Assert.ThrowsAsync<ArgumentException>(async () => await RunAsync(outside, new DateContext(Bound), reversed));
    }

    /// <summary>
    /// Reversed bounds throw even for a null selection, and the exception has the same message shape and parameter name
    /// as the numeric range predicates: it names the predicate and both bounds, and the parameter is <c>args</c>.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BetweenAndOutside_ReversedBoundsWithNullSelection_ThrowSharedMessageAndParamName_Test(bool useOutside)
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = useOutside
            ? DateTimePredicates.Outside<DateContext>("range", c => c.When)
            : DateTimePredicates.Between<DateContext>("range", c => c.When);
        PredicateArguments reversed = Range(Bound.AddDays(1), Bound);

        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await RunAsync(evaluate, new DateContext((DateTimeOffset?)null), reversed)
        );

        Assert.Equal("args", error.ParamName);
        Assert.Contains("Predicate 'range' has reversed bounds", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The reversed-bounds message prints both bounds in the round-trip <c>O</c> format with the invariant culture, so the
    /// text does not depend on the culture of the evaluating thread.
    /// </summary>
    [Fact]
    public async Task Between_ReversedBoundsUnderNonInvariantCulture_PrintsInvariantRoundTripBounds_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> between) =
            DateTimePredicates.Between<DateContext>("range", c => c.When);
        DateTimeOffset lower = Bound.AddDays(1);
        CultureInfo original = CultureInfo.CurrentCulture;
        ArgumentException error;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            error = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await RunAsync(between, new DateContext(Bound), Range(lower, Bound))
            );
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.Contains(
            $"'lower' ({lower.ToString("O", CultureInfo.InvariantCulture)}) is greater than "
                + $"'upper' ({Bound.ToString("O", CultureInfo.InvariantCulture)})",
            error.Message,
            StringComparison.Ordinal
        );
    }

    /// <summary>A null selection is Unknown by default for every predicate, and the twins keep it Unknown.</summary>
    [Fact]
    public async Task Evaluate_NullSelectionByDefault_ReturnsUnknown_Test()
    {
        DateContext context = new((DateTimeOffset?)null);
        List<Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>>> evaluators =
        [
            DateTimePredicates.After<DateContext>("p", c => c.When).Evaluate,
            DateTimePredicates.NotAfter<DateContext>("p", c => c.When).Evaluate,
            DateTimePredicates.Before<DateContext>("p", c => c.When).Evaluate,
            DateTimePredicates.NotBefore<DateContext>("p", c => c.When).Evaluate,
        ];

        foreach (Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluator in evaluators)
        {
            Assert.Equal(TruthValue.Unknown, await RunAsync(evaluator, context, Single("value", Bound)));
        }

        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> between = DateTimePredicates
            .Between<DateContext>("p", c => c.When)
            .Evaluate;
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> outside = DateTimePredicates
            .Outside<DateContext>("p", c => c.When)
            .Evaluate;
        Assert.Equal(TruthValue.Unknown, await RunAsync(between, context, Range(Bound, Bound.AddDays(1))));
        Assert.Equal(TruthValue.Unknown, await RunAsync(outside, context, Range(Bound, Bound.AddDays(1))));
    }

    /// <summary>The host can choose NullBehavior.False: a null selection then answers a definite False.</summary>
    [Fact]
    public async Task After_NullSelectionWithFalseOption_ReturnsFalse_Test()
    {
        (_, Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> after) =
            DateTimePredicates.After<DateContext>("after", c => c.When, nullBehavior: NullBehavior.False);

        TruthValue result = await RunAsync(after, new DateContext(null), Single("value", Bound));

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>The schemas declare DateTimeOffset literal arguments and describe the host-side DateTime conversion.</summary>
    [Fact]
    public void Schema_AfterAndBetween_DeclareDateTimeOffsetArgumentsAndExplainConversion_Test()
    {
        (PredicateSchema after, _) = DateTimePredicates.After<DateContext>("after", c => c.When);
        (PredicateSchema between, _) = DateTimePredicates.Between<DateContext>("between", c => c.When);

        Assert.Equal(LiteralKind.DateTimeOffset, Assert.Single(after.Arguments).Type);
        Assert.Equal(2, between.Arguments.Count);
        Assert.All(between.Arguments, a => Assert.Equal(LiteralKind.DateTimeOffset, a.Type));
        Assert.Contains("DateTime", after.Description, StringComparison.Ordinal);
    }

    private static ValueTask<TruthValue> RunAsync(
        Func<DateContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluator,
        DateContext context,
        PredicateArguments args
    )
    {
        return evaluator(context, args, CancellationToken.None);
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

    private static PredicateArguments Single(string name, DateTimeOffset value)
    {
        return new(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfDateTimeOffset(value) });
    }

    private static PredicateArguments Range(DateTimeOffset lower, DateTimeOffset upper)
    {
        return new(
            new Dictionary<string, LiteralValue>
            {
                ["lower"] = LiteralValue.OfDateTimeOffset(lower),
                ["upper"] = LiteralValue.OfDateTimeOffset(upper),
            }
        );
    }
}
