namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Predicates;

/// <summary>
/// The reviewed twin table of the predicate catalog: every public predicate factory of the predicates package is half of
/// one <see cref="TwinPair"/> or one <see cref="NoTwin"/> row. Pairs are stated explicitly, never guessed from names.
/// </summary>
/// <remarks>
/// <para>
/// Every factory with a <c>nullBehavior</c> option is registered with <see cref="NullBehavior.Unknown"/>, so a null
/// selected value is the Unknown probe. The <see cref="TwinPair.Nulls"/> of each such pair also register both factories
/// with no option and with <see cref="NullBehavior.False"/>; the catalog test checks that the twin answers
/// <c>NOT positive</c> for a null selected value under both, so a twin has the same default as its positive and is the
/// strict complement under <see cref="NullBehavior.False"/>.
/// </para>
/// <para>
/// Every twin has the same stem as its positive with <c>Not</c> after the <c>Is</c> prefix, or in front of the verb:
/// <c>IsEqualTo</c> pairs with <c>IsNotEqualTo</c>, <c>IsBetween</c> with <c>IsNotBetween</c>, <c>IsLessThan</c> with
/// <c>IsNotLessThan</c> and <c>Contains</c> with <c>NotContains</c>. The four ordering comparisons each have their own
/// exact complement, so <c>IsNotLessThan</c> answers like <c>IsGreaterThanOrEqualTo</c> but is a separate factory.
/// </para>
/// <para>
/// Definite pairs have no Unknown probe: <c>IsNull</c>/<c>IsNotNull</c>, the string null tests
/// (<c>IsNullOrEmpty</c>, <c>IsNullOrWhiteSpace</c> and their complements) answer a
/// definite value for a null selection, so no selected value is Unknown. Their True probe is the null selection, which
/// proves the definite answer is still the complement.
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

    private static readonly long[] LongValues = [1L, 2L];

    private static readonly decimal[] DecimalValues = [1.5m, 2.5m];

    private static readonly bool[] BoolValues = [true];

    private static readonly string[] AandB = ["a", "b"];

    private static readonly string[] AandZ = ["a", "z"];

    private static readonly string[] FridayOnly = ["Friday"];

    private static readonly long[] JanuaryOnly = [1L];

    /// <summary>Gets every row of the table.</summary>
    public static IReadOnlyList<TwinTableEntry> Entries { get; } =
    [
        .. StringRows(),
        .. CollectionRows(),
        .. DateTimeRows(),
        .. CalendarRows(),
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
            "StringPredicates.IsEqualTo(String)",
            "StringPredicates.IsNotEqualTo(String)",
            n => StringPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "a"), ("ignoreCase", true)],
            Text("A"),
            Text("b"),
            Missing,
            Nulls: new(
                n => StringPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "StringPredicates.StartsWith(String)",
            "StringPredicates.NotStartsWith(String)",
            n => StringPredicates.StartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotStartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "ab")],
            Text("abc"),
            Text("xbc"),
            Missing,
            Nulls: new(
                n => StringPredicates.StartsWith<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.NotStartsWith<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.StartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.NotStartsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "StringPredicates.EndsWith(String)",
            "StringPredicates.NotEndsWith(String)",
            n => StringPredicates.EndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotEndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "bc")],
            Text("abc"),
            Text("abx"),
            Missing,
            Nulls: new(
                n => StringPredicates.EndsWith<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.NotEndsWith<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.EndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.NotEndsWith<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "StringPredicates.Contains(String)",
            "StringPredicates.NotContains(String)",
            n => StringPredicates.Contains<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.NotContains<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("value", "b")],
            Text("abc"),
            Text("xyz"),
            Missing,
            Nulls: new(
                n => StringPredicates.Contains<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.NotContains<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.Contains<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.NotContains<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "StringPredicates.IsEmpty(String)",
            "StringPredicates.IsNotEmpty(String)",
            n => StringPredicates.IsEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [],
            Text(string.Empty),
            Text("a"),
            Missing,
            Nulls: new(
                n => StringPredicates.IsEmpty<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
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
            "RegexPredicates.IsMatch(String)",
            "RegexPredicates.IsNotMatch(String)",
            n => RegexPredicates.IsMatch<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => RegexPredicates.IsNotMatch<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("pattern", "^a")],
            Text("abc"),
            Text("xbc"),
            Missing,
            Nulls: new(
                n => RegexPredicates.IsMatch<TwinProbeContext>(n, c => c.Text),
                n => RegexPredicates.IsNotMatch<TwinProbeContext>(n, c => c.Text),
                n => RegexPredicates.IsMatch<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => RegexPredicates.IsNotMatch<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> CollectionRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "CollectionPredicates.IsEmpty(IReadOnlyCollection<String>)",
            "CollectionPredicates.IsNotEmpty(IReadOnlyCollection<String>)",
            n => CollectionPredicates.IsEmpty<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [],
            Items(),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.IsEmpty<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.IsEmpty<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.IsNotEmpty<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.IsNullOrEmpty(IReadOnlyCollection<String>)",
            "CollectionPredicates.IsNotNullOrEmpty(IReadOnlyCollection<String>)",
            n => CollectionPredicates.IsNullOrEmpty<TwinProbeContext>(n, c => c.Items),
            n => CollectionPredicates.IsNotNullOrEmpty<TwinProbeContext>(n, c => c.Items),
            [],
            Missing,
            Items("a"),
            null,
            Definite
        );
        yield return new TwinPair(
            "CollectionPredicates.Contains(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContains(IReadOnlyCollection<String>)",
            n => CollectionPredicates.Contains<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContains<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("value", "a")],
            Items("a", "b"),
            Items("b"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.Contains<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotContains<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.Contains<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.NotContains<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.SetEquals(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotSetEquals(IReadOnlyCollection<String>)",
            n => CollectionPredicates.SetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotSetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("b", "a"),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.SetEquals<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotSetEquals<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.SetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.NotSetEquals<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.ContainsAny(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContainsAny(IReadOnlyCollection<String>)",
            n => CollectionPredicates.ContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandZ)],
            Items("a", "b"),
            Items("b"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.ContainsAny<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotContainsAny<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.ContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.NotContainsAny<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.ContainsAll(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotContainsAll(IReadOnlyCollection<String>)",
            n => CollectionPredicates.ContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("a", "b", "c"),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.ContainsAll<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotContainsAll<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.ContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.NotContainsAll<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.IsSubsetOf(IReadOnlyCollection<String>)",
            "CollectionPredicates.IsNotSubsetOf(IReadOnlyCollection<String>)",
            n => CollectionPredicates.IsSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.IsNotSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("values", AandB)],
            Items("a"),
            Items("a", "c"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.IsSubsetOf<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.IsNotSubsetOf<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.IsSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n => CollectionPredicates.IsNotSubsetOf<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "StringPredicates.IsIn(String)",
            "StringPredicates.IsNotIn(String)",
            n => StringPredicates.IsIn<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            n => StringPredicates.IsNotIn<TwinProbeContext>(n, c => c.Text, nullBehavior: unknown),
            [("values", AandB)],
            Text("a"),
            Text("c"),
            Missing,
            Nulls: new(
                n => StringPredicates.IsIn<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsNotIn<TwinProbeContext>(n, c => c.Text),
                n => StringPredicates.IsIn<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => StringPredicates.IsNotIn<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.HasCountEqualTo(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotHasCountEqualTo(IReadOnlyCollection<String>)",
            n => CollectionPredicates.HasCountEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotHasCountEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a", "b"),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.HasCountEqualTo<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotHasCountEqualTo<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.HasCountEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n =>
                    CollectionPredicates.NotHasCountEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.HasCountLessThan(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotHasCountLessThan(IReadOnlyCollection<String>)",
            n => CollectionPredicates.HasCountLessThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotHasCountLessThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a"),
            Items("a", "b"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.HasCountLessThan<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotHasCountLessThan<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.HasCountLessThan<TwinProbeContext>(n, c => c.Items, nullBehavior: NullBehavior.False),
                n =>
                    CollectionPredicates.NotHasCountLessThan<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.HasCountGreaterThan(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotHasCountGreaterThan(IReadOnlyCollection<String>)",
            n => CollectionPredicates.HasCountGreaterThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotHasCountGreaterThan<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 1L)],
            Items("a", "b"),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.HasCountGreaterThan<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotHasCountGreaterThan<TwinProbeContext>(n, c => c.Items),
                n =>
                    CollectionPredicates.HasCountGreaterThan<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    CollectionPredicates.NotHasCountGreaterThan<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.HasCountLessThanOrEqualTo(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotHasCountLessThanOrEqualTo(IReadOnlyCollection<String>)",
            n => CollectionPredicates.HasCountLessThanOrEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotHasCountLessThanOrEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 1L)],
            Items("a"),
            Items("a", "b"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.HasCountLessThanOrEqualTo<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotHasCountLessThanOrEqualTo<TwinProbeContext>(n, c => c.Items),
                n =>
                    CollectionPredicates.HasCountLessThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    CollectionPredicates.NotHasCountLessThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "CollectionPredicates.HasCountGreaterThanOrEqualTo(IReadOnlyCollection<String>)",
            "CollectionPredicates.NotHasCountGreaterThanOrEqualTo(IReadOnlyCollection<String>)",
            n => CollectionPredicates.HasCountGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            n => CollectionPredicates.NotHasCountGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Items, nullBehavior: unknown),
            [("count", 2L)],
            Items("a", "b"),
            Items("a"),
            Missing,
            Nulls: new(
                n => CollectionPredicates.HasCountGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Items),
                n => CollectionPredicates.NotHasCountGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Items),
                n =>
                    CollectionPredicates.HasCountGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    CollectionPredicates.NotHasCountGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Items,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
    }

    private static IEnumerable<TwinTableEntry> DateTimeRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "DateTimePredicates.IsAfter(DateTimeOffset)",
            "DateTimePredicates.IsNotAfter(DateTimeOffset)",
            n => DateTimePredicates.IsAfter<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotAfter<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0.AddDays(1)),
            Instant(T0),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsAfter<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotAfter<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsAfter<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotAfter<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "DateTimePredicates.IsBefore(DateTimeOffset)",
            "DateTimePredicates.IsNotBefore(DateTimeOffset)",
            n => DateTimePredicates.IsBefore<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotBefore<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0.AddDays(-1)),
            Instant(T0),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsBefore<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotBefore<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsBefore<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotBefore<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "DateTimePredicates.IsBetween(DateTimeOffset)",
            "DateTimePredicates.IsNotBetween(DateTimeOffset)",
            n => DateTimePredicates.IsBetween<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotBetween<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("lower", T0), ("upper", T0.AddDays(10))],
            Instant(T0.AddDays(1)),
            Instant(T0.AddDays(20)),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsBetween<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotBetween<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsBetween<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotBetween<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );

        // The clock reads the fixed instant T0, so the False probe sits on the boundary where IsAfterNow and IsBeforeNow are
        // both False: the reason each has its own twin.
        yield return new TwinPair(
            "DateTimePredicates.IsAfterNow(DateTimeOffset)",
            "DateTimePredicates.IsNotAfterNow(DateTimeOffset)",
            n => DateTimePredicates.IsAfterNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            n => DateTimePredicates.IsNotAfterNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            [],
            Instant(T0.AddDays(1)),
            Instant(T0),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsAfterNow<TwinProbeContext>(n, c => c.Instant, Clock),
                n => DateTimePredicates.IsNotAfterNow<TwinProbeContext>(n, c => c.Instant, Clock),
                n =>
                    DateTimePredicates.IsAfterNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: NullBehavior.False),
                n =>
                    DateTimePredicates.IsNotAfterNow<TwinProbeContext>(
                        n,
                        c => c.Instant,
                        Clock,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "DateTimePredicates.IsBeforeNow(DateTimeOffset)",
            "DateTimePredicates.IsNotBeforeNow(DateTimeOffset)",
            n => DateTimePredicates.IsBeforeNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            n => DateTimePredicates.IsNotBeforeNow<TwinProbeContext>(n, c => c.Instant, Clock, nullBehavior: unknown),
            [],
            Instant(T0.AddDays(-1)),
            Instant(T0),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsBeforeNow<TwinProbeContext>(n, c => c.Instant, Clock),
                n => DateTimePredicates.IsNotBeforeNow<TwinProbeContext>(n, c => c.Instant, Clock),
                n =>
                    DateTimePredicates.IsBeforeNow<TwinProbeContext>(
                        n,
                        c => c.Instant,
                        Clock,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    DateTimePredicates.IsNotBeforeNow<TwinProbeContext>(
                        n,
                        c => c.Instant,
                        Clock,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
    }

    /// <summary>
    /// The fixed-offset calendar pairs. <c>T0</c> is Thursday 2026-01-01 12:00 UTC, so the probes read it in an offset:
    /// at <c>+14:00</c> it is Friday 02:00, which moves the day and keeps the month.
    /// </summary>
    private static IEnumerable<TwinTableEntry> CalendarRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "DateTimePredicates.IsOnDayOfWeek(DateTimeOffset)",
            "DateTimePredicates.IsNotOnDayOfWeek(DateTimeOffset)",
            n => DateTimePredicates.IsOnDayOfWeek<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotOnDayOfWeek<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("days", FridayOnly), ("offset", "+14:00")],
            Instant(T0),
            Instant(T0.AddDays(1)),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsOnDayOfWeek<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotOnDayOfWeek<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsOnDayOfWeek<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotOnDayOfWeek<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "DateTimePredicates.IsInMonth(DateTimeOffset)",
            "DateTimePredicates.IsNotInMonth(DateTimeOffset)",
            n => DateTimePredicates.IsInMonth<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotInMonth<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("months", JanuaryOnly), ("offset", "Z")],
            Instant(T0),
            Instant(T0.AddMonths(1)),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsInMonth<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotInMonth<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsInMonth<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotInMonth<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "DateTimePredicates.IsInTimeWindow(DateTimeOffset)",
            "DateTimePredicates.IsNotInTimeWindow(DateTimeOffset)",
            n => DateTimePredicates.IsInTimeWindow<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => DateTimePredicates.IsNotInTimeWindow<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("start", "09:00"), ("end", "17:00"), ("offset", "Z")],
            Instant(T0),
            Instant(T0.AddHours(8)),
            Missing,
            Nulls: new(
                n => DateTimePredicates.IsInTimeWindow<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsNotInTimeWindow<TwinProbeContext>(n, c => c.Instant),
                n => DateTimePredicates.IsInTimeWindow<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => DateTimePredicates.IsNotInTimeWindow<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> Int64Rows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "NumericPredicates.IsEqualTo(Int64)",
            "NumericPredicates.IsNotEqualTo(Int64)",
            n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(5),
            Int64(6),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsLessThan(Int64)",
            "NumericPredicates.IsNotLessThan(Int64)",
            n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(4),
            Int64(5),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsGreaterThan(Int64)",
            "NumericPredicates.IsNotGreaterThan(Int64)",
            n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(6),
            Int64(5),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsLessThanOrEqualTo(Int64)",
            "NumericPredicates.IsNotLessThanOrEqualTo(Int64)",
            n => NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(5),
            Int64(6),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n =>
                    NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Int64,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsGreaterThanOrEqualTo(Int64)",
            "NumericPredicates.IsNotGreaterThanOrEqualTo(Int64)",
            n => NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("value", 5L)],
            Int64(5),
            Int64(4),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Int64),
                n =>
                    NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Int64,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Int64,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsBetween(Int64)",
            "NumericPredicates.IsNotBetween(Int64)",
            n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("lower", 1L), ("upper", 5L)],
            Int64(3),
            Int64(9),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsIn(Int64)",
            "NumericPredicates.IsNotIn(Int64)",
            n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Int64, nullBehavior: unknown),
            [("values", LongValues)],
            Int64(1),
            Int64(3),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Int64),
                n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Int64, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> DecimalRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "NumericPredicates.IsEqualTo(Decimal)",
            "NumericPredicates.IsNotEqualTo(Decimal)",
            n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(5.5m),
            Decimal(6.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsLessThan(Decimal)",
            "NumericPredicates.IsNotLessThan(Decimal)",
            n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(4.5m),
            Decimal(5.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsLessThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotLessThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsGreaterThan(Decimal)",
            "NumericPredicates.IsNotGreaterThan(Decimal)",
            n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(6.5m),
            Decimal(5.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsGreaterThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotGreaterThan<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsLessThanOrEqualTo(Decimal)",
            "NumericPredicates.IsNotLessThanOrEqualTo(Decimal)",
            n => NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(5.5m),
            Decimal(6.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n =>
                    NumericPredicates.IsLessThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Decimal,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    NumericPredicates.IsNotLessThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Decimal,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsGreaterThanOrEqualTo(Decimal)",
            "NumericPredicates.IsNotGreaterThanOrEqualTo(Decimal)",
            n => NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("value", 5.5m)],
            Decimal(5.5m),
            Decimal(4.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(n, c => c.Decimal),
                n =>
                    NumericPredicates.IsGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Decimal,
                        nullBehavior: NullBehavior.False
                    ),
                n =>
                    NumericPredicates.IsNotGreaterThanOrEqualTo<TwinProbeContext>(
                        n,
                        c => c.Decimal,
                        nullBehavior: NullBehavior.False
                    )
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsBetween(Decimal)",
            "NumericPredicates.IsNotBetween(Decimal)",
            n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("lower", 1.5m), ("upper", 5.5m)],
            Decimal(3.5m),
            Decimal(9.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsBetween<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotBetween<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "NumericPredicates.IsIn(Decimal)",
            "NumericPredicates.IsNotIn(Decimal)",
            n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Decimal, nullBehavior: unknown),
            [("values", DecimalValues)],
            Decimal(1.5m),
            Decimal(3.5m),
            Missing,
            Nulls: new(
                n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsIn<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotIn<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Decimal),
                n => NumericPredicates.IsDefault<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False),
                n => NumericPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Decimal, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> BooleanRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.IsEqualTo(Boolean)",
            "ScalarPredicates.IsNotEqualTo(Boolean)",
            n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            [("value", true)],
            Boolean(true),
            Boolean(false),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "ScalarPredicates.IsIn(Boolean)",
            "ScalarPredicates.IsNotIn(Boolean)",
            n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Boolean, nullBehavior: unknown),
            [("values", BoolValues)],
            Boolean(true),
            Boolean(false),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Boolean),
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Boolean, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> GuidRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.IsEqualTo(Guid)",
            "ScalarPredicates.IsNotEqualTo(Guid)",
            n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            [("value", A)],
            Id(A),
            Id(B),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "ScalarPredicates.IsIn(Guid)",
            "ScalarPredicates.IsNotIn(Guid)",
            n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Guid, nullBehavior: unknown),
            [("values", new Guid[] { A })],
            Id(A),
            Id(B),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Guid),
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Guid, nullBehavior: NullBehavior.False)
            )
        );
    }

    private static IEnumerable<TwinTableEntry> InstantRows()
    {
        const NullBehavior unknown = NullBehavior.Unknown;
        yield return new TwinPair(
            "ScalarPredicates.IsEqualTo(DateTimeOffset)",
            "ScalarPredicates.IsNotEqualTo(DateTimeOffset)",
            n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("value", T0)],
            Instant(T0),
            Instant(T0.AddDays(1)),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsEqualTo<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotEqualTo<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "ScalarPredicates.IsIn(DateTimeOffset)",
            "ScalarPredicates.IsNotIn(DateTimeOffset)",
            n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Instant, nullBehavior: unknown),
            [("values", new DateTimeOffset[] { T0 })],
            Instant(T0),
            Instant(T0.AddDays(1)),
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsIn<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotIn<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Instant),
                n => ScalarPredicates.IsDefault<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False),
                n => ScalarPredicates.IsNotDefault<TwinProbeContext>(n, c => c.Instant, nullBehavior: NullBehavior.False)
            )
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
            Missing,
            Nulls: new(
                n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsGuid(Object)",
            "TypePredicates.IsNotGuid(Object)",
            n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Object),
            [],
            Object(A),
            Object(5),
            Missing,
            Nulls: new(
                n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsGuid<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotGuid<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsNumeric(String)",
            "TypePredicates.IsNotNumeric(String)",
            n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Text),
            [],
            Text("42"),
            Text("abc"),
            Missing,
            Nulls: new(
                n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsNumeric(Object)",
            "TypePredicates.IsNotNumeric(Object)",
            n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Object),
            [],
            Object(42),
            Object(A),
            Missing,
            Nulls: new(
                n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNumeric<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotNumeric<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsUrl(String)",
            "TypePredicates.IsNotUrl(String)",
            n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Text),
            [],
            Text("https://example.com/"),
            Text("ftp://example.com/"),
            Missing,
            Nulls: new(
                n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsUrl(Object)",
            "TypePredicates.IsNotUrl(Object)",
            n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Object),
            [],
            Object(new Uri("https://example.com/")),
            Object(5),
            Missing,
            Nulls: new(
                n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsUrl<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotUrl<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False)
            )
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
            "With a string selector every non-null value is a string, so no input makes IsString False.",
            Nulls: new(
                n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsString(Object)",
            "TypePredicates.IsNotString(Object)",
            n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Object),
            [],
            Object("a"),
            Object(5),
            Missing,
            Nulls: new(
                n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsString<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotString<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsDateTimeOffset(String)",
            "TypePredicates.IsNotDateTimeOffset(String)",
            n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Text),
            n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Text),
            [],
            Text("2026-01-01T12:00:00Z"),
            Text("2026-01-01T12:00:00"),
            Missing,
            Nulls: new(
                n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Text),
                n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Text, nullBehavior: NullBehavior.False)
            )
        );
        yield return new TwinPair(
            "TypePredicates.IsDateTimeOffset(Object)",
            "TypePredicates.IsNotDateTimeOffset(Object)",
            n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Object),
            n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Object),
            [],
            Object(T0),
            Object(5),
            Missing,
            Nulls: new(
                n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Object),
                n => TypePredicates.IsDateTimeOffset<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False),
                n => TypePredicates.IsNotDateTimeOffset<TwinProbeContext>(n, c => c.Object, nullBehavior: NullBehavior.False)
            )
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
