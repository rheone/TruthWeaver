namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;
using Registration = (
    TruthWeaver.Abstractions.PredicateSchema Schema,
    System.Func<
        TruthWeaver.Predicates.Tests.Selections,
        TruthWeaver.Abstractions.PredicateArguments,
        System.Threading.CancellationToken,
        System.Threading.Tasks.ValueTask<TruthWeaver.Abstractions.TruthValue>
    > Evaluate
);

/// <summary>
/// Pins the null answer of every <c>NotX</c> twin that takes a <see cref="NullBehavior"/>: for a null selection the twin
/// answers the Strong Kleene complement of its positive predicate under both settings. Under
/// <see cref="NullBehavior.False"/> the positive predicate answers False and the twin True; under
/// <see cref="NullBehavior.Unknown"/> both answer Unknown.
/// </summary>
public sealed class TwinNullComplementTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeProvider Clock = new FixedTimeProvider(T0);

    private static readonly Selections Missing = new();

    /// <summary>Gets the name of every pair whose members take a <see cref="NullBehavior"/>.</summary>
    public static TheoryData<string> PairNames => [.. Pairs.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Gets the name of every pair of definite null tests.</summary>
    public static TheoryData<string> DefinitePairNames => [.. DefinitePairs.Keys.Order(StringComparer.Ordinal)];

    private static Dictionary<string, Pair> Pairs { get; } =
        new(StringComparer.Ordinal)
        {
            ["String.Equals/NotEqual"] = new(
                nb => StringPredicates.Equals<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotEqual<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["String.Contains/NotContains"] = new(
                nb => StringPredicates.Contains<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotContains<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["String.EqualsIgnoreCase/NotEqualsIgnoreCase"] = new(
                nb => StringPredicates.EqualsIgnoreCase<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotEqualsIgnoreCase<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["String.StartsWith/NotStartsWith"] = new(
                nb => StringPredicates.StartsWith<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotStartsWith<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["String.EndsWith/NotEndsWith"] = new(
                nb => StringPredicates.EndsWith<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotEndsWith<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["String.EqualsConfigurable/NotEqualsConfigurable"] = new(
                nb => StringPredicates.EqualsConfigurable<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.NotEqualsConfigurable<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(
                    ("value", LiteralValue.OfString("a")),
                    ("ignoreCase", LiteralValue.OfBoolean(true)),
                    ("trim", LiteralValue.OfBoolean(false))
                )
            ),
            ["Collection.SetEquals/NotSetEquals"] = new(
                nb => CollectionPredicates.SetEquals<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotSetEquals<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("values", Strings("a")))
            ),
            ["String.IsEmpty/IsNotEmpty"] = new(
                nb => StringPredicates.IsEmpty<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => StringPredicates.IsNotEmpty<Selections>("t", c => c.Text, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["Regex.Matches/NotMatches"] = new(
                nb => RegexPredicates.Matches<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => RegexPredicates.NotMatches<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("pattern", LiteralValue.OfString("^a")))
            ),
            ["Collection.Contains/NotContains"] = new(
                nb => CollectionPredicates.Contains<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotContains<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("value", LiteralValue.OfString("a")))
            ),
            ["Collection.ContainsAny/NotContainsAny"] = new(
                nb => CollectionPredicates.ContainsAny<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotContainsAny<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("values", Strings("a")))
            ),
            ["Collection.ContainsAll/NotContainsAll"] = new(
                nb => CollectionPredicates.ContainsAll<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotContainsAll<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("values", Strings("a")))
            ),
            ["Collection.IsSubsetOf/IsNotSubsetOf"] = new(
                nb => CollectionPredicates.IsSubsetOf<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.IsNotSubsetOf<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("values", Strings("a")))
            ),
            ["Collection.In/NotIn"] = new(
                nb => CollectionPredicates.In<Selections>("p", c => c.Text, nullBehavior: nb),
                nb => CollectionPredicates.NotIn<Selections>("t", c => c.Text, nullBehavior: nb),
                Args(("values", Strings("a")))
            ),
            ["Collection.CountEqual/NotCountEqual"] = new(
                nb => CollectionPredicates.CountEqual<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotCountEqual<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("count", LiteralValue.OfInt64(1)))
            ),
            ["Collection.CountLessThan/NotCountLessThan"] = new(
                nb => CollectionPredicates.CountLessThan<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotCountLessThan<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("count", LiteralValue.OfInt64(1)))
            ),
            ["Collection.CountGreaterThan/NotCountGreaterThan"] = new(
                nb => CollectionPredicates.CountGreaterThan<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotCountGreaterThan<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("count", LiteralValue.OfInt64(1)))
            ),
            ["Collection.CountLessThanOrEqual/NotCountLessThanOrEqual"] = new(
                nb => CollectionPredicates.CountLessThanOrEqual<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotCountLessThanOrEqual<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("count", LiteralValue.OfInt64(1)))
            ),
            ["Collection.CountGreaterThanOrEqual/NotCountGreaterThanOrEqual"] = new(
                nb => CollectionPredicates.CountGreaterThanOrEqual<Selections>("p", c => c.Items, nullBehavior: nb),
                nb => CollectionPredicates.NotCountGreaterThanOrEqual<Selections>("t", c => c.Items, nullBehavior: nb),
                Args(("count", LiteralValue.OfInt64(1)))
            ),
            ["DateTime.After/NotAfter"] = new(
                nb => DateTimePredicates.After<Selections>("p", c => c.At, nullBehavior: nb),
                nb => DateTimePredicates.NotAfter<Selections>("t", c => c.At, nullBehavior: nb),
                Args(("value", LiteralValue.OfDateTimeOffset(T0)))
            ),
            ["DateTime.Before/NotBefore"] = new(
                nb => DateTimePredicates.Before<Selections>("p", c => c.At, nullBehavior: nb),
                nb => DateTimePredicates.NotBefore<Selections>("t", c => c.At, nullBehavior: nb),
                Args(("value", LiteralValue.OfDateTimeOffset(T0)))
            ),
            ["DateTime.Between/Outside"] = new(
                nb => DateTimePredicates.Between<Selections>("p", c => c.At, nullBehavior: nb),
                nb => DateTimePredicates.Outside<Selections>("t", c => c.At, nullBehavior: nb),
                Args(("lower", LiteralValue.OfDateTimeOffset(T0)), ("upper", LiteralValue.OfDateTimeOffset(T0.AddDays(1))))
            ),
            ["DateTime.AfterNow/NotAfterNow"] = new(
                nb => DateTimePredicates.AfterNow<Selections>("p", c => c.At, Clock, nullBehavior: nb),
                nb => DateTimePredicates.NotAfterNow<Selections>("t", c => c.At, Clock, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["DateTime.BeforeNow/NotBeforeNow"] = new(
                nb => DateTimePredicates.BeforeNow<Selections>("p", c => c.At, Clock, nullBehavior: nb),
                nb => DateTimePredicates.NotBeforeNow<Selections>("t", c => c.At, Clock, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["Int64.Equal/NotEqual"] = new(
                nb => NumericPredicates.Equal<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.NotEqual<Selections>("t", c => c.Int64, nullBehavior: nb),
                Args(("value", LiteralValue.OfInt64(5)))
            ),
            ["Int64.LessThan/GreaterThanOrEqual"] = new(
                nb => NumericPredicates.LessThan<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.GreaterThanOrEqual<Selections>("t", c => c.Int64, nullBehavior: nb),
                Args(("value", LiteralValue.OfInt64(5)))
            ),
            ["Int64.GreaterThan/LessThanOrEqual"] = new(
                nb => NumericPredicates.GreaterThan<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.LessThanOrEqual<Selections>("t", c => c.Int64, nullBehavior: nb),
                Args(("value", LiteralValue.OfInt64(5)))
            ),
            ["Int64.Between/Outside"] = new(
                nb => NumericPredicates.Between<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.Outside<Selections>("t", c => c.Int64, nullBehavior: nb),
                Args(("lower", LiteralValue.OfInt64(1)), ("upper", LiteralValue.OfInt64(5)))
            ),
            ["Int64.In/NotIn"] = new(
                nb => NumericPredicates.In<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.NotIn<Selections>("t", c => c.Int64, nullBehavior: nb),
                Args(("values", LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1)])))
            ),
            ["Int64.IsDefault/IsNotDefault"] = new(
                nb => NumericPredicates.IsDefault<Selections>("p", c => c.Int64, nullBehavior: nb),
                nb => NumericPredicates.IsNotDefault<Selections>("t", c => c.Int64, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["Decimal.Equal/NotEqual"] = new(
                nb => NumericPredicates.Equal<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.NotEqual<Selections>("t", c => c.Decimal, nullBehavior: nb),
                Args(("value", LiteralValue.OfDecimal(5.5m)))
            ),
            ["Decimal.LessThan/GreaterThanOrEqual"] = new(
                nb => NumericPredicates.LessThan<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.GreaterThanOrEqual<Selections>("t", c => c.Decimal, nullBehavior: nb),
                Args(("value", LiteralValue.OfDecimal(5.5m)))
            ),
            ["Decimal.GreaterThan/LessThanOrEqual"] = new(
                nb => NumericPredicates.GreaterThan<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.LessThanOrEqual<Selections>("t", c => c.Decimal, nullBehavior: nb),
                Args(("value", LiteralValue.OfDecimal(5.5m)))
            ),
            ["Decimal.Between/Outside"] = new(
                nb => NumericPredicates.Between<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.Outside<Selections>("t", c => c.Decimal, nullBehavior: nb),
                Args(("lower", LiteralValue.OfDecimal(1.5m)), ("upper", LiteralValue.OfDecimal(5.5m)))
            ),
            ["Decimal.In/NotIn"] = new(
                nb => NumericPredicates.In<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.NotIn<Selections>("t", c => c.Decimal, nullBehavior: nb),
                Args(("values", LiteralValue.OfArray(LiteralKind.Decimal, [LiteralValue.OfDecimal(1.5m)])))
            ),
            ["Decimal.IsDefault/IsNotDefault"] = new(
                nb => NumericPredicates.IsDefault<Selections>("p", c => c.Decimal, nullBehavior: nb),
                nb => NumericPredicates.IsNotDefault<Selections>("t", c => c.Decimal, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["Boolean.Equal/NotEqual"] = new(
                nb => ScalarPredicates.Equal<Selections>("p", c => c.Flag, nullBehavior: nb),
                nb => ScalarPredicates.NotEqual<Selections>("t", c => c.Flag, nullBehavior: nb),
                Args(("value", LiteralValue.OfBoolean(true)))
            ),
            ["Boolean.In/NotIn"] = new(
                nb => ScalarPredicates.In<Selections>("p", c => c.Flag, nullBehavior: nb),
                nb => ScalarPredicates.NotIn<Selections>("t", c => c.Flag, nullBehavior: nb),
                Args(("values", LiteralValue.OfArray(LiteralKind.Boolean, [LiteralValue.OfBoolean(true)])))
            ),
            ["Boolean.IsDefault/IsNotDefault"] = new(
                nb => ScalarPredicates.IsDefault<Selections>("p", c => c.Flag, nullBehavior: nb),
                nb => ScalarPredicates.IsNotDefault<Selections>("t", c => c.Flag, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["Guid.Equal/NotEqual"] = new(
                nb => ScalarPredicates.Equal<Selections>("p", c => c.Id, nullBehavior: nb),
                nb => ScalarPredicates.NotEqual<Selections>("t", c => c.Id, nullBehavior: nb),
                Args(("value", LiteralValue.OfGuid(Guid.Empty)))
            ),
            ["Guid.In/NotIn"] = new(
                nb => ScalarPredicates.In<Selections>("p", c => c.Id, nullBehavior: nb),
                nb => ScalarPredicates.NotIn<Selections>("t", c => c.Id, nullBehavior: nb),
                Args(("values", LiteralValue.OfArray(LiteralKind.Guid, [LiteralValue.OfGuid(Guid.Empty)])))
            ),
            ["Guid.IsDefault/IsNotDefault"] = new(
                nb => ScalarPredicates.IsDefault<Selections>("p", c => c.Id, nullBehavior: nb),
                nb => ScalarPredicates.IsNotDefault<Selections>("t", c => c.Id, nullBehavior: nb),
                PredicateArguments.Empty
            ),
            ["DateTimeOffset.Equal/NotEqual"] = new(
                nb => ScalarPredicates.Equal<Selections>("p", c => c.At, nullBehavior: nb),
                nb => ScalarPredicates.NotEqual<Selections>("t", c => c.At, nullBehavior: nb),
                Args(("value", LiteralValue.OfDateTimeOffset(T0)))
            ),
            ["DateTimeOffset.In/NotIn"] = new(
                nb => ScalarPredicates.In<Selections>("p", c => c.At, nullBehavior: nb),
                nb => ScalarPredicates.NotIn<Selections>("t", c => c.At, nullBehavior: nb),
                Args(("values", LiteralValue.OfArray(LiteralKind.DateTimeOffset, [LiteralValue.OfDateTimeOffset(T0)])))
            ),
            ["DateTimeOffset.IsDefault/IsNotDefault"] = new(
                nb => ScalarPredicates.IsDefault<Selections>("p", c => c.At, nullBehavior: nb),
                nb => ScalarPredicates.IsNotDefault<Selections>("t", c => c.At, nullBehavior: nb),
                PredicateArguments.Empty
            ),
        };

    private static Dictionary<string, (Registration Positive, Registration Twin)> DefinitePairs { get; } =
        new(StringComparer.Ordinal)
        {
            ["String.IsNullOrEmpty/IsNotNullOrEmpty"] = (
                StringPredicates.IsNullOrEmpty<Selections>("p", c => c.Text),
                StringPredicates.IsNotNullOrEmpty<Selections>("t", c => c.Text)
            ),
            ["String.IsNullOrWhiteSpace/IsNotNullOrWhiteSpace"] = (
                StringPredicates.IsNullOrWhiteSpace<Selections>("p", c => c.Text),
                StringPredicates.IsNotNullOrWhiteSpace<Selections>("t", c => c.Text)
            ),
            ["Collection.IsEmpty/IsNotEmpty"] = (
                CollectionPredicates.IsEmpty<Selections>("p", c => c.Items),
                CollectionPredicates.IsNotEmpty<Selections>("t", c => c.Items)
            ),
            ["Int64.IsNull/IsNotNull"] = (
                NumericPredicates.IsNull<Selections>("p", c => c.Int64),
                NumericPredicates.IsNotNull<Selections>("t", c => c.Int64)
            ),
            ["Decimal.IsNull/IsNotNull"] = (
                NumericPredicates.IsNull<Selections>("p", c => c.Decimal),
                NumericPredicates.IsNotNull<Selections>("t", c => c.Decimal)
            ),
            ["Boolean.IsNull/IsNotNull"] = (
                ScalarPredicates.IsNull<Selections>("p", c => c.Flag),
                ScalarPredicates.IsNotNull<Selections>("t", c => c.Flag)
            ),
            ["Guid.IsNull/IsNotNull"] = (
                ScalarPredicates.IsNull<Selections>("p", c => c.Id),
                ScalarPredicates.IsNotNull<Selections>("t", c => c.Id)
            ),
            ["DateTimeOffset.IsNull/IsNotNull"] = (
                ScalarPredicates.IsNull<Selections>("p", c => c.At),
                ScalarPredicates.IsNotNull<Selections>("t", c => c.At)
            ),
        };

    /// <summary>
    /// Under <see cref="NullBehavior.False"/> a null selection makes the positive predicate a definite False and its twin
    /// the complement, a definite True.
    /// </summary>
    /// <param name="pair">The name of the pair.</param>
    [Theory]
    [MemberData(nameof(PairNames))]
    public async Task Twin_NullSelectionWithFalseBehavior_IsTrueComplementOfFalsePositive_Test(string pair)
    {
        (TruthValue positive, TruthValue twin) = await EvaluateNullAsync(Pairs[pair], NullBehavior.False);

        Assert.Equal((TruthValue.False, TruthValue.True), (positive, twin));
    }

    /// <summary>Under <see cref="NullBehavior.Unknown"/> a null selection makes both members of a pair Unknown.</summary>
    /// <param name="pair">The name of the pair.</param>
    [Theory]
    [MemberData(nameof(PairNames))]
    public async Task Twin_NullSelectionWithUnknownBehavior_StaysUnknown_Test(string pair)
    {
        (TruthValue positive, TruthValue twin) = await EvaluateNullAsync(Pairs[pair], NullBehavior.Unknown);

        Assert.Equal((TruthValue.Unknown, TruthValue.Unknown), (positive, twin));
    }

    /// <summary>A pair of definite null tests stays definite for a null selection, and the two answers are complements.</summary>
    /// <param name="pair">The name of the pair.</param>
    [Theory]
    [MemberData(nameof(DefinitePairNames))]
    public async Task DefiniteTwin_NullSelection_IsDefiniteComplement_Test(string pair)
    {
        (Registration positive, Registration twin) = DefinitePairs[pair];

        TruthValue positiveAnswer = await positive.Evaluate(Missing, PredicateArguments.Empty, CancellationToken.None);
        TruthValue twinAnswer = await twin.Evaluate(Missing, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal((TruthValue.True, TruthValue.False), (positiveAnswer, twinAnswer));
    }

    private static async Task<(TruthValue Positive, TruthValue Twin)> EvaluateNullAsync(Pair pair, NullBehavior nullBehavior)
    {
        TruthValue positive = await pair.Positive(nullBehavior).Evaluate(Missing, pair.Arguments, CancellationToken.None);
        TruthValue twin = await pair.Twin(nullBehavior).Evaluate(Missing, pair.Arguments, CancellationToken.None);
        return (positive, twin);
    }

    private static PredicateArguments Args(params (string Name, LiteralValue Value)[] arguments)
    {
        return new PredicateArguments(arguments.ToDictionary(argument => argument.Name, argument => argument.Value));
    }

    private static LiteralValue Strings(params string[] values)
    {
        return LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
    }

    /// <summary>A positive predicate and its twin, each registered with a given <see cref="NullBehavior"/>.</summary>
    private sealed record Pair(
        Func<NullBehavior, Registration> Positive,
        Func<NullBehavior, Registration> Twin,
        PredicateArguments Arguments
    );

    /// <summary>A clock fixed at one instant.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
