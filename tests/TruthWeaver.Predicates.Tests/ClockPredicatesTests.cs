namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;
using Evaluator = System.Func<
    TruthWeaver.Predicates.Tests.DateContext,
    TruthWeaver.Abstractions.PredicateArguments,
    System.Threading.CancellationToken,
    System.Threading.Tasks.ValueTask<TruthWeaver.Abstractions.TruthValue>
>;

/// <summary>
/// Pins the clock members of <see cref="DateTimePredicates"/>: <c>AfterNow</c> and <c>BeforeNow</c> with their
/// <c>NotAfterNow</c> and <c>NotBeforeNow</c> Strong Kleene twins, driven by a host-supplied
/// <see cref="TimeProvider"/>.
/// </summary>
public class ClockPredicatesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 10, 0, 0, TimeSpan.Zero);

    /// <summary>AfterNow is a strict greater-than against the provider's now; the twin complements it, also at the boundary instant.</summary>
    [Theory]
    [InlineData(-1, TruthValue.False)]
    [InlineData(0, TruthValue.False)]
    [InlineData(1, TruthValue.True)]
    public async Task AfterNow_TicksAroundNow_IsStrict_Test(long offsetTicks, TruthValue expected)
    {
        ManualClock clock = new(Now);
        Evaluator after = DateTimePredicates.AfterNow<DateContext>("after", c => c.When, clock).Evaluate;
        Evaluator notAfter = DateTimePredicates.NotAfterNow<DateContext>("notAfter", c => c.When, clock).Evaluate;
        DateContext context = new(Now.AddTicks(offsetTicks));

        Assert.Equal(expected, await RunAsync(after, context));
        Assert.Equal(Complement(expected), await RunAsync(notAfter, context));
    }

    /// <summary>BeforeNow is a strict less-than against the provider's now; the twin complements it, also at the boundary instant.</summary>
    [Theory]
    [InlineData(-1, TruthValue.True)]
    [InlineData(0, TruthValue.False)]
    [InlineData(1, TruthValue.False)]
    public async Task BeforeNow_TicksAroundNow_IsStrict_Test(long offsetTicks, TruthValue expected)
    {
        ManualClock clock = new(Now);
        Evaluator before = DateTimePredicates.BeforeNow<DateContext>("before", c => c.When, clock).Evaluate;
        Evaluator notBefore = DateTimePredicates.NotBeforeNow<DateContext>("notBefore", c => c.When, clock).Evaluate;
        DateContext context = new(Now.AddTicks(offsetTicks));

        Assert.Equal(expected, await RunAsync(before, context));
        Assert.Equal(Complement(expected), await RunAsync(notBefore, context));
    }

    /// <summary>At the exact now instant AfterNow and BeforeNow are both False, so neither is the complement of the other and each needs its own twin.</summary>
    [Fact]
    public async Task AfterNowAndBeforeNow_AtNowInstant_AreBothFalseAndTwinsBothTrue_Test()
    {
        ManualClock clock = new(Now);
        DateContext context = new(Now);

        Assert.Equal(
            TruthValue.False,
            await RunAsync(DateTimePredicates.AfterNow<DateContext>("a", c => c.When, clock).Evaluate, context)
        );
        Assert.Equal(
            TruthValue.False,
            await RunAsync(DateTimePredicates.BeforeNow<DateContext>("b", c => c.When, clock).Evaluate, context)
        );
        Assert.Equal(
            TruthValue.True,
            await RunAsync(DateTimePredicates.NotAfterNow<DateContext>("na", c => c.When, clock).Evaluate, context)
        );
        Assert.Equal(
            TruthValue.True,
            await RunAsync(DateTimePredicates.NotBeforeNow<DateContext>("nb", c => c.When, clock).Evaluate, context)
        );
    }

    /// <summary>The provider is read on every evaluation, so advancing the clock changes the answer; nothing is cached across calls.</summary>
    [Fact]
    public async Task AfterNow_ClockAdvances_AnswerFollowsTheClock_Test()
    {
        ManualClock clock = new(Now);
        Evaluator after = DateTimePredicates.AfterNow<DateContext>("after", c => c.When, clock).Evaluate;
        DateContext context = new(Now.AddMinutes(5));

        Assert.Equal(TruthValue.True, await RunAsync(after, context));

        clock.Set(Now.AddMinutes(10));

        Assert.Equal(TruthValue.False, await RunAsync(after, context));
    }

    /// <summary>Each evaluation of a predicate reads the clock once, and the read happens at call time, not at registration.</summary>
    [Fact]
    public async Task AfterNow_OneEvaluation_ReadsTheClockOnceAtCallTime_Test()
    {
        ManualClock clock = new(Now);
        Evaluator after = DateTimePredicates.AfterNow<DateContext>("after", c => c.When, clock).Evaluate;

        Assert.Equal(0, clock.Reads);

        await RunAsync(after, new DateContext(Now));

        Assert.Equal(1, clock.Reads);
    }

    /// <summary>A null selection is Unknown for all four members; NullBehavior.False makes a positive False and its twin True.</summary>
    [Fact]
    public async Task ClockPredicates_NullSelection_FollowNullBehavior_Test()
    {
        ManualClock clock = new(Now);
        DateContext context = new(null);

        List<Evaluator> unknown =
        [
            DateTimePredicates.AfterNow<DateContext>("p", c => c.When, clock).Evaluate,
            DateTimePredicates.NotAfterNow<DateContext>("p", c => c.When, clock).Evaluate,
            DateTimePredicates.BeforeNow<DateContext>("p", c => c.When, clock).Evaluate,
            DateTimePredicates.NotBeforeNow<DateContext>("p", c => c.When, clock).Evaluate,
        ];
        foreach (Evaluator evaluator in unknown)
        {
            Assert.Equal(TruthValue.Unknown, await RunAsync(evaluator, context));
        }

        Evaluator afterFalse = DateTimePredicates
            .AfterNow<DateContext>("p", c => c.When, clock, nullBehavior: NullBehavior.False)
            .Evaluate;
        Evaluator notBeforeFalse = DateTimePredicates
            .NotBeforeNow<DateContext>("p", c => c.When, clock, nullBehavior: NullBehavior.False)
            .Evaluate;

        Assert.Equal(TruthValue.False, await RunAsync(afterFalse, context));
        Assert.Equal(TruthValue.True, await RunAsync(notBeforeFalse, context));
    }

    /// <summary>The factories require a non-null TimeProvider; there is no ambient default.</summary>
    [Fact]
    public void AfterNow_NullTimeProvider_Throws_Test()
    {
        Assert.Throws<ArgumentNullException>(() => DateTimePredicates.AfterNow<DateContext>("p", c => c.When, null!));
        Assert.Throws<ArgumentNullException>(() => DateTimePredicates.NotBeforeNow<DateContext>("p", c => c.When, null!));
    }

    /// <summary>The clock predicates take no rule-text arguments and their schema says the instant comes from the host clock.</summary>
    [Fact]
    public void Schema_AfterNow_HasNoArgumentsAndDescribesTheClock_Test()
    {
        (PredicateSchema schema, _) = DateTimePredicates.AfterNow<DateContext>("p", c => c.When, new ManualClock(Now));

        Assert.Empty(schema.Arguments);
        Assert.Contains("TimeProvider", schema.Description, StringComparison.Ordinal);
    }

    private static ValueTask<TruthValue> RunAsync(Evaluator evaluator, DateContext context)
    {
        return evaluator(context, new PredicateArguments(new Dictionary<string, LiteralValue>()), CancellationToken.None);
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

    /// <summary>A settable clock that counts how often the predicates read it.</summary>
    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public int Reads { get; private set; }

        public void Set(DateTimeOffset value)
        {
            this.current = value;
        }

        public override DateTimeOffset GetUtcNow()
        {
            this.Reads++;
            return this.current;
        }
    }
}
