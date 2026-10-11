namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>The baseline and boundary argument values that <see cref="PredicateHarness"/> generates per <see cref="LiteralKind"/>.</summary>
internal static class HarnessValues
{
    /// <summary>The length of the long-string boundary value.</summary>
    internal const int LongStringLength = 10_000;

    /// <summary>A fixed, non-empty GUID, so that the baseline differs from the <see cref="Guid.Empty"/> boundary value.</summary>
    private static readonly Guid TypicalGuid = new("6f9619ff-8b86-d011-b42d-00c04fc964ff");

    /// <summary>Gets an ordinary value of <paramref name="kind"/> that no predicate is expected to reject.</summary>
    /// <param name="kind">The declared argument kind.</param>
    /// <returns>The typical value.</returns>
    internal static LiteralValue Typical(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => LiteralValue.OfString("text"),
            LiteralKind.Int64 => LiteralValue.OfInt64(1),
            LiteralKind.Decimal => LiteralValue.OfDecimal(1m),
            LiteralKind.Boolean => LiteralValue.OfBoolean(true),
            LiteralKind.DateTimeOffset => LiteralValue.OfDateTimeOffset(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            LiteralKind.Guid => LiteralValue.OfGuid(TypicalGuid),
            _ => LiteralValue.OfArray(ElementKind(kind), [Typical(ElementKind(kind))]),
        };
    }

    /// <summary>Gets the boundary values of <paramref name="kind"/>, each with a label for the report.</summary>
    /// <param name="kind">The declared argument kind.</param>
    /// <returns>The labelled boundary values.</returns>
    internal static IReadOnlyList<(string Label, LiteralValue Value)> Boundaries(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String =>
            [
                ("empty string", LiteralValue.OfString(string.Empty)),
                ($"string of {LongStringLength} characters", LiteralValue.OfString(new string('a', LongStringLength))),
            ],
            LiteralKind.Int64 =>
            [
                ("Int64.MinValue", LiteralValue.OfInt64(long.MinValue)),
                ("0", LiteralValue.OfInt64(0)),
                ("Int64.MaxValue", LiteralValue.OfInt64(long.MaxValue)),
            ],
            LiteralKind.Decimal =>
            [
                ("Decimal.MinValue", LiteralValue.OfDecimal(decimal.MinValue)),
                ("0", LiteralValue.OfDecimal(0m)),
                ("Decimal.MaxValue", LiteralValue.OfDecimal(decimal.MaxValue)),
            ],
            LiteralKind.Boolean => [("true", LiteralValue.OfBoolean(true)), ("false", LiteralValue.OfBoolean(false))],
            LiteralKind.DateTimeOffset =>
            [
                ("DateTimeOffset.MinValue", LiteralValue.OfDateTimeOffset(DateTimeOffset.MinValue)),
                ("DateTimeOffset.MaxValue", LiteralValue.OfDateTimeOffset(DateTimeOffset.MaxValue)),
            ],
            LiteralKind.Guid => [("Guid.Empty", LiteralValue.OfGuid(Guid.Empty))],
            _ =>
            [
                ("empty array", LiteralValue.OfArray(ElementKind(kind), [])),
                (
                    "array of the element boundary values",
                    LiteralValue.OfArray(ElementKind(kind), Boundaries(ElementKind(kind)).Select(b => b.Value))
                ),
            ],
        };
    }

    private static LiteralKind ElementKind(LiteralKind arrayKind)
    {
        return arrayKind switch
        {
            LiteralKind.StringArray => LiteralKind.String,
            LiteralKind.Int64Array => LiteralKind.Int64,
            LiteralKind.DecimalArray => LiteralKind.Decimal,
            LiteralKind.BooleanArray => LiteralKind.Boolean,
            LiteralKind.DateTimeOffsetArray => LiteralKind.DateTimeOffset,
            LiteralKind.GuidArray => LiteralKind.Guid,
            _ => throw new ArgumentOutOfRangeException(nameof(arrayKind), arrayKind, "Not an array kind."),
        };
    }
}
