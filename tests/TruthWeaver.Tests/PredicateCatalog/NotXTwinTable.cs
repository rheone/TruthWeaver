namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Predicates;

/// <summary>
/// The reviewed twin table of the predicate catalog: every public predicate factory of the predicates package is half of
/// one <see cref="TwinPair"/> or one <see cref="NoTwin"/> row. Pairs are stated explicitly, never guessed from names.
/// </summary>
/// <remarks>
/// <para>
/// Every factory with a <c>nullBehavior</c> option is registered with <see cref="NullBehavior.Unknown"/>, so a null
/// selected value is the Unknown probe. Under <see cref="NullBehavior.False"/> the twin is also the strict complement
/// (it answers True where the positive answers False), which the twin tests of each predicate cover.
/// </para>
/// <para>
/// Pairs whose names do not follow <c>X</c>/<c>NotX</c>: the ordering family pairs each comparison with the opposite
/// ordering (<c>LessThan</c> with <c>GreaterThanOrEqual</c>, <c>GreaterThan</c> with <c>LessThanOrEqual</c>), <c>Between</c>
/// pairs with <c>Outside</c>, <c>Equal</c> (and the string <c>Equals</c>) pairs with <c>NotEqual</c>, and the
/// <c>Is</c>-prefixed tests pair with <c>IsNotX</c>.
/// </para>
/// <para>
/// Definite pairs have no Unknown probe: <c>IsNull</c>/<c>IsNotNull</c>, the string null tests
/// (<c>IsNullOrEmpty</c>, <c>IsNullOrWhiteSpace</c> and their complements) and the collection emptiness tests answer a
/// definite value for a null selection, so no selected value is Unknown. Their True probe is the null selection, which
/// proves the definite answer is still the complement.
/// </para>
/// <para>
/// Array arguments of a value type are written as <c>object[]</c>: <c>RuleBuilder.Predicate</c>
/// reads an array argument as <c>IEnumerable&lt;object&gt;</c>, which a <c>long[]</c> is not.
/// </para>
/// </remarks>
internal static class NotXTwinTable
{
    private const string Definite = "A null test: a null selected value answers a definite value, so no input is Unknown.";

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly TimeProvider Clock = new FixedTimeProvider(T0);

    private static readonly TwinProbeContext Missing = new();

    private static readonly string[] AandB = ["a", "b"];

    private static readonly string[] AandZ = ["a", "z"];

    /// <summary>Gets every row of the table.</summary>
    public static IReadOnlyList<TwinTableEntry> Entries { get; } =
    [
        .. StringRows(),
        .. CollectionRows(),
        .. DateTimeRows(),
        .. Int64Rows(),
        .. DecimalRows(),
        .. BooleanRows(),
        .. GuidRows(),
        .. InstantRows(),
        .. TypeRows(),
        new NoTwin(
            "SelectedValuePredicates.Create<TContext, TSelected>",
            "A generic factory for a host-written test: the host supplies the answer, so the host registers any negation."
        ),
        new NoTwin(
            "SelectedValuePredicates.Create<TContext>",
            "A generic factory for a host-written test: the host supplies the answer, so the host registers any negation."
        ),
    ];

    /// <summary>Gets the pairs of the table.</summary>
    public static IEnumerable<TwinPair> Pairs => Entries.OfType<TwinPair>();

    private static TwinProbeContext Text(string? value)
    {
        return new(Text: value);
    }

    private static TwinProbeContext Items(params string[] values)
    {
        return new(Items: values);
    }

    private static TwinProbeContext Int64(long value)
    {
        return new(Int64: value);
    }

    private static TwinProbeContext Decimal(decimal value)
    {
        return new(Decimal: value);
    }

    private static TwinProbeContext Boolean(bool value)
    {
        return new(Boolean: value);
    }

    private static TwinProbeContext Id(Guid value)
    {
        return new(Guid: value);
    }

    private static TwinProbeContext Instant(DateTimeOffset value)
    {
        return new(Instant: value);
    }

    private static TwinProbeContext Object(object value)
    {
        return new(Object: value);
    }

    private static IEnumerable<TwinTableEntry> StringRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "StringPredicates.Equals(String)",
            "StringPredicates.NotEqual(String)",
            n => StringPredicates.Equals<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotEqual<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "a")],
            Text("a"),
            Text("b"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.EqualsIgnoreCase(String)",
            "StringPredicates.NotEqualsIgnoreCase(String)",
            n => StringPredicates.EqualsIgnoreCase<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotEqualsIgnoreCase<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "a")],
            Text("A"),
            Text("b"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.StartsWith(String)",
            "StringPredicates.NotStartsWith(String)",
            n => StringPredicates.StartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotStartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "ab")],
            Text("abc"),
            Text("xbc"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.EndsWith(String)",
            "StringPredicates.NotEndsWith(String)",
            n => StringPredicates.EndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotEndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "bc")],
            Text("abc"),
            Text("abx"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.EqualsConfigurable(String)",
            "StringPredicates.NotEqualsConfigurable(String)",
            n => StringPredicates.EqualsConfigurable<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotEqualsConfigurable<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "a")],
            Text("A"),
            Text("b"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.Contains(String)",
            "StringPredicates.NotContains(String)",
            n => StringPredicates.Contains<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotContains<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "b")],
            Text("abc"),
            Text("xyz"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.IsEmpty(String)",
            "StringPredicates.IsNotEmpty(String)",
            n => StringPredicates.IsEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [],
            Text(string.Empty),
            Text("a"),
            Missing
        );
        yield return new TwinPair(
            "StringPredicates.IsNullOrEmpty(String)",
            "StringPredicates.IsNotNullOrEmpty(String)",
            n => StringPredicates.IsNullOrEmpty<TwinProbeContext>(n, c => c.Text),
            n => StringPredicates.IsNotNullOrEmpty<TwinProbeContext>(n, c => c.Text),
            [],
            Missing,
            Text("a"),
            null,
            Definite
        );
        yield return new TwinPair(
            "StringPredicates.IsNullOrWhiteSpace(String)",
            "StringPredicates.IsNotNullOrWhiteSpace(String)",
            n => StringPredicates.IsNullOrWhiteSpace<TwinProbeContext>(n, c => c.Text),
            n => StringPredicates.IsNotNullOrWhiteSpace<TwinProbeContext>(n, c => c.Text),
            [],
            Missing,
            Text("a"),
            null,
            Definite
        );
        yield return new TwinPair(
            "RegexPredicates.Matches(String)",
            "RegexPredicates.NotMatches(String)",
            n => RegexPredicates.Matches<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => RegexPredicates.NotMatches<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("pattern", "^a")],
            Text("abc"),
            Text("xbc"),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> CollectionRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "CollectionPredicates.IsEmpty(IReadOnlyCollection<String>)",
            "CollectionPredicates.IsNotEmpty(IReadOnlyCollection<String>)",
            n => CollectionPredicates.IsEmpty<TwinProbeContext>(n, c => c.Items),
            n => CollectionPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Items),
            [],
            Missing,
            Items("a"),
            null,
            "An emptiness test: a null collection counts as empty, so no input is Unknown."
        );
        yield return new TwinPair(
            "CollectionPredicates.Contains(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContains(IReadOnlyCollection<String>)",
            n => CollectionPredicates.Contains<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContains<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("value", "a")],
            Items("a", "b"),
            Items("b"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.SetEquals(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotSetEquals(IReadOnlyCollection<String>)",
            n => CollectionPredicates.SetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotSetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("b", "a"),
            Items("a"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.ContainsAny(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContainsAny(IReadOnlyCollection<String>)",
            n => CollectionPredicates.ContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandZ)],
            Items("a", "b"),
            Items("b"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.ContainsAll(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContainsAll(IReadOnlyCollection<String>)",
            n => CollectionPredicates.ContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("a", "b", "c"),
            Items("a"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.IsSubsetOf(IReadOnlyCollection<String>)",
            "CollectionPredicates.IsNotSubsetOf(IReadOnlyCollection<String>)",
            n => CollectionPredicates.IsSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.IsNotSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("a"),
            Items("a", "c"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.In(String)",
            "CollectionPredicates.NotIn(String)",
            n => CollectionPredicates.In<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => CollectionPredicates.NotIn<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("values", AandB)],
            Text("a"),
            Text("c"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.CountEqual(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotCountEqual(IReadOnlyCollection<String>)",
            n => CollectionPredicates.CountEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotCountEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a", "b"),
            Items("a"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.CountLessThan(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotCountLessThan(IReadOnlyCollection<String>)",
            n => CollectionPredicates.CountLessThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotCountLessThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a"),
            Items("a", "b"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.CountGreaterThan(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotCountGreaterThan(IReadOnlyCollection<String>)",
            n => CollectionPredicates.CountGreaterThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotCountGreaterThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 1L)],
            Items("a", "b"),
            Items("a"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.CountLessThanOrEqual(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotCountLessThanOrEqual(IReadOnlyCollection<String>)",
            n => CollectionPredicates.CountLessThanOrEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotCountLessThanOrEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 1L)],
            Items("a"),
            Items("a", "b"),
            Missing
        );
        yield return new TwinPair(
            "CollectionPredicates.CountGreaterThanOrEqual(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotCountGreaterThanOrEqual(IReadOnlyCollection<String>)",
            n => CollectionPredicates.CountGreaterThanOrEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotCountGreaterThanOrEqual<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a", "b"),
            Items("a"),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> DateTimeRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "DateTimePredicates.After(DateTimeOffset)",
            "DateTimePredicates.NotAfter(DateTimeOffset)",
            n => DateTimePredicates.After<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.NotAfter<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0.AddDays(1)),
            Instant(T0),
            Missing
        );
        yield return new TwinPair(
            "DateTimePredicates.Before(DateTimeOffset)",
            "DateTimePredicates.NotBefore(DateTimeOffset)",
            n => DateTimePredicates.Before<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.NotBefore<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0.AddDays(-1)),
            Instant(T0),
            Missing
        );
        yield return new TwinPair(
            "DateTimePredicates.Between(DateTimeOffset)",
            "DateTimePredicates.Outside(DateTimeOffset)",
            n => DateTimePredicates.Between<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.Outside<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("lower", T0), ("upper", T0.AddDays(10))],
            Instant(T0.AddDays(1)),
            Instant(T0.AddDays(20)),
            Missing
        );

        // The clock reads the fixed instant T0, so the False probe sits on the boundary where AfterNow and BeforeNow are
        // both False: the reason each has its own twin.
        yield return new TwinPair(
            "DateTimePredicates.AfterNow(DateTimeOffset)",
            "DateTimePredicates.NotAfterNow(DateTimeOffset)",
            n => DateTimePredicates.AfterNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            n => DateTimePredicates.NotAfterNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            [],
            Instant(T0.AddDays(1)),
            Instant(T0),
            Missing
        );
        yield return new TwinPair(
            "DateTimePredicates.BeforeNow(DateTimeOffset)",
            "DateTimePredicates.NotBeforeNow(DateTimeOffset)",
            n => DateTimePredicates.BeforeNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            n => DateTimePredicates.NotBeforeNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            [],
            Instant(T0.AddDays(-1)),
            Instant(T0),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> Int64Rows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "NumericPredicates.Equal(Int64)",
            "NumericPredicates.NotEqual(Int64)",
            n => NumericPredicates.Equal<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.NotEqual<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(5),
            Int64(6),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.LessThan(Int64)",
            "NumericPredicates.GreaterThanOrEqual(Int64)",
            n => NumericPredicates.LessThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.GreaterThanOrEqual<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(4),
            Int64(5),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.GreaterThan(Int64)",
            "NumericPredicates.LessThanOrEqual(Int64)",
            n => NumericPredicates.GreaterThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.LessThanOrEqual<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(6),
            Int64(5),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.Between(Int64)",
            "NumericPredicates.Outside(Int64)",
            n => NumericPredicates.Between<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.Outside<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("lower", 1L), ("upper", 5L)],
            Int64(3),
            Int64(9),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.In(Int64)",
            "NumericPredicates.NotIn(Int64)",
            n => NumericPredicates.In<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.NotIn<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("values", new object[] { 1L, 2L })],
            Int64(1),
            Int64(3),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.IsNull(Int64)",
            "NumericPredicates.IsNotNull(Int64)",
            n => NumericPredicates.IsNull<TwinProbeContext>(n, c => c.Int64),
            n => NumericPredicates.IsNotNull<TwinProbeContext>(n, c => c.Int64),
            [],
            Missing,
            Int64(5),
            null,
            Definite
        );
        yield return new TwinPair(
            "NumericPredicates.IsDefault(Int64)",
            "NumericPredicates.IsNotDefault(Int64)",
            n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [],
            Int64(0),
            Int64(5),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> DecimalRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "NumericPredicates.Equal(Decimal)",
            "NumericPredicates.NotEqual(Decimal)",
            n => NumericPredicates.Equal<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.NotEqual<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(5.5m),
            Decimal(6.5m),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.LessThan(Decimal)",
            "NumericPredicates.GreaterThanOrEqual(Decimal)",
            n => NumericPredicates.LessThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.GreaterThanOrEqual<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(4.5m),
            Decimal(5.5m),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.GreaterThan(Decimal)",
            "NumericPredicates.LessThanOrEqual(Decimal)",
            n => NumericPredicates.GreaterThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.LessThanOrEqual<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(6.5m),
            Decimal(5.5m),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.Between(Decimal)",
            "NumericPredicates.Outside(Decimal)",
            n => NumericPredicates.Between<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.Outside<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("lower", 1.5m), ("upper", 5.5m)],
            Decimal(3.5m),
            Decimal(9.5m),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.In(Decimal)",
            "NumericPredicates.NotIn(Decimal)",
            n => NumericPredicates.In<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.NotIn<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("values", new object[] { 1.5m, 2.5m })],
            Decimal(1.5m),
            Decimal(3.5m),
            Missing
        );
        yield return new TwinPair(
            "NumericPredicates.IsNull(Decimal)",
            "NumericPredicates.IsNotNull(Decimal)",
            n => NumericPredicates.IsNull<TwinProbeContext>(n, c => c.Decimal),
            n => NumericPredicates.IsNotNull<TwinProbeContext>(n, c => c.Decimal),
            [],
            Missing,
            Decimal(5.5m),
            null,
            Definite
        );
        yield return new TwinPair(
            "NumericPredicates.IsDefault(Decimal)",
            "NumericPredicates.IsNotDefault(Decimal)",
            n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [],
            Decimal(0m),
            Decimal(5.5m),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> BooleanRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.Equal(Boolean)",
            "ScalarPredicates.NotEqual(Boolean)",
            n => ScalarPredicates.Equal<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            n => ScalarPredicates.NotEqual<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            [("value", true)],
            Boolean(true),
            Boolean(false),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.In(Boolean)",
            "ScalarPredicates.NotIn(Boolean)",
            n => ScalarPredicates.In<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            n => ScalarPredicates.NotIn<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            [("values", new object[] { true })],
            Boolean(true),
            Boolean(false),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.IsNull(Boolean)",
            "ScalarPredicates.IsNotNull(Boolean)",
            n => ScalarPredicates.IsNull<TwinProbeContext>(n, c => c.Boolean),
            n => ScalarPredicates.IsNotNull<TwinProbeContext>(n, c => c.Boolean),
            [],
            Missing,
            Boolean(true),
            null,
            Definite
        );
        yield return new TwinPair(
            "ScalarPredicates.IsDefault(Boolean)",
            "ScalarPredicates.IsNotDefault(Boolean)",
            n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            [],
            Boolean(false),
            Boolean(true),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> GuidRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.Equal(Guid)",
            "ScalarPredicates.NotEqual(Guid)",
            n => ScalarPredicates.Equal<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            n => ScalarPredicates.NotEqual<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            [("value", A)],
            Id(A),
            Id(B),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.In(Guid)",
            "ScalarPredicates.NotIn(Guid)",
            n => ScalarPredicates.In<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            n => ScalarPredicates.NotIn<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            [("values", new object[] { A })],
            Id(A),
            Id(B),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.IsNull(Guid)",
            "ScalarPredicates.IsNotNull(Guid)",
            n => ScalarPredicates.IsNull<TwinProbeContext>(n, c => c.Guid),
            n => ScalarPredicates.IsNotNull<TwinProbeContext>(n, c => c.Guid),
            [],
            Missing,
            Id(A),
            null,
            Definite
        );
        yield return new TwinPair(
            "ScalarPredicates.IsDefault(Guid)",
            "ScalarPredicates.IsNotDefault(Guid)",
            n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            [],
            Id(Guid.Empty),
            Id(A),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> InstantRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.Equal(DateTimeOffset)",
            "ScalarPredicates.NotEqual(DateTimeOffset)",
            n => ScalarPredicates.Equal<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => ScalarPredicates.NotEqual<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0),
            Instant(T0.AddDays(1)),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.In(DateTimeOffset)",
            "ScalarPredicates.NotIn(DateTimeOffset)",
            n => ScalarPredicates.In<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => ScalarPredicates.NotIn<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("values", new object[] { T0 })],
            Instant(T0),
            Instant(T0.AddDays(1)),
            Missing
        );
        yield return new TwinPair(
            "ScalarPredicates.IsNull(DateTimeOffset)",
            "ScalarPredicates.IsNotNull(DateTimeOffset)",
            n => ScalarPredicates.IsNull<TwinProbeContext>(n, c => c.Instant),
            n => ScalarPredicates.IsNotNull<TwinProbeContext>(n, c => c.Instant),
            [],
            Missing,
            Instant(T0),
            null,
            Definite
        );
        yield return new TwinPair(
            "ScalarPredicates.IsDefault(DateTimeOffset)",
            "ScalarPredicates.IsNotDefault(DateTimeOffset)",
            n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [],
            Instant(default),
            Instant(T0),
            Missing
        );
    }

    private static IEnumerable<TwinTableEntry> TypeRows()
    {
        yield return new TwinPair(
            "TypePredicates.IsGuid(String)",
            "TypePredicates.IsNotGuid(String)",
            n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Text),
            [],
            Text(A.ToString()),
            Text("not-a-guid"),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsGuid(Object)",
            "TypePredicates.IsNotGuid(Object)",
            n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Object),
            [],
            Object(A),
            Object(5),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsNumeric(String)",
            "TypePredicates.IsNotNumeric(String)",
            n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Text),
            [],
            Text("42"),
            Text("abc"),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsNumeric(Object)",
            "TypePredicates.IsNotNumeric(Object)",
            n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Object),
            [],
            Object(42),
            Object(A),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsUrl(String)",
            "TypePredicates.IsNotUrl(String)",
            n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Text),
            [],
            Text("https://example.com/"),
            Text("ftp://example.com/"),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsUrl(Object)",
            "TypePredicates.IsNotUrl(Object)",
            n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Object),
            [],
            Object(new Uri("https://example.com/")),
            Object(5),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsString(String)",
            "TypePredicates.IsNotString(String)",
            n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Text),
            [],
            Text(string.Empty),
            null,
            Missing,
            "With a string selector every non-null value is a string, so no input makes IsString False."
        );
        yield return new TwinPair(
            "TypePredicates.IsString(Object)",
            "TypePredicates.IsNotString(Object)",
            n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Object),
            [],
            Object("a"),
            Object(5),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsDateTimeOffset(String)",
            "TypePredicates.IsNotDateTimeOffset(String)",
            n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Text),
            [],
            Text("2026-01-01T12:00:00Z"),
            Text("2026-01-01T12:00:00"),
            Missing
        );
        yield return new TwinPair(
            "TypePredicates.IsDateTimeOffset(Object)",
            "TypePredicates.IsNotDateTimeOffset(Object)",
            n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Object),
            [],
            Object(T0),
            Object(5),
            Missing
        );
    }

    /// <summary>A clock that always reads one instant, so the clock predicates have a stable boundary.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
