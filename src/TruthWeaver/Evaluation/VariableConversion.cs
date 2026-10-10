namespace TruthWeaver.Evaluation;

using TruthWeaver.Abstractions;

/// <summary>
/// Turns the nodes a data source matched into the value of one predicate argument (ADR-0006 decisions 6 and 7).
/// The conversions are exactly those the DSL applies to a literal and nothing wider: a string parses to a
/// <see cref="DateTimeOffset"/> or a <see cref="Guid"/>, an integer widens to <see cref="decimal"/>, and a
/// whole-number <see cref="decimal"/> narrows to <see cref="long"/> only when exactly representable. A string is never
/// coerced to a number or a boolean, and a number is never coerced to a string.
/// </summary>
internal static class VariableConversion
{
    /// <summary>Converts the matches of a query to the value of an argument of kind <paramref name="kind"/>.</summary>
    /// <param name="matches">The matched nodes, in document order.</param>
    /// <param name="kind">The kind the predicate's schema declares for the argument.</param>
    /// <param name="value">The argument value, when conversion succeeded.</param>
    /// <param name="failure">The kind of failure, when conversion did not succeed.</param>
    /// <param name="message">A description of the failure that names kinds and counts but never a value.</param>
    /// <returns><see langword="true"/> if the matches make a valid argument value.</returns>
    public static bool TryConvert(
        IReadOnlyList<LiteralValue> matches,
        LiteralKind kind,
        out LiteralValue value,
        out VariableFailureKind failure,
        out string message
    )
    {
        value = default;
        failure = default;
        message = string.Empty;

        if (IsArrayKind(kind))
        {
            // An array argument collects every match; zero matches, including a path to a missing property, is an
            // empty array because a query result cannot tell the two apart.
            LiteralKind elementKind = LiteralValue.ToElementKind(kind);
            List<LiteralValue> elements = [with(matches.Count)];
            foreach (LiteralValue match in matches)
            {
                if (!TryConvertScalar(match, elementKind, out LiteralValue element))
                {
                    failure = VariableFailureKind.TypeMismatch;
                    message = $"A match of kind {match.Kind} cannot be an element of kind {elementKind} (expected {kind}).";
                    return false;
                }

                elements.Add(element);
            }

            value = LiteralValue.OfArray(elementKind, elements);
            return true;
        }

        if (matches.Count == 0)
        {
            failure = VariableFailureKind.Missing;
            message = $"The query matched nothing (expected one {kind}).";
            return false;
        }

        if (matches.Count > 1)
        {
            failure = VariableFailureKind.Ambiguous;
            message = $"The query matched {matches.Count} nodes (expected exactly one {kind}).";
            return false;
        }

        if (!TryConvertScalar(matches[0], kind, out value))
        {
            failure = VariableFailureKind.TypeMismatch;
            message = $"The match is of kind {matches[0].Kind} but the argument expects {kind}.";
            return false;
        }

        return true;
    }

    private static bool IsArrayKind(LiteralKind kind)
    {
        return kind
            is LiteralKind.StringArray
                or LiteralKind.Int64Array
                or LiteralKind.DecimalArray
                or LiteralKind.BooleanArray
                or LiteralKind.DateTimeOffsetArray
                or LiteralKind.GuidArray;
    }

    private static bool TryConvertScalar(LiteralValue match, LiteralKind kind, out LiteralValue value)
    {
        value = default;
        switch (kind)
        {
            case LiteralKind.String when match.Kind == LiteralKind.String:
            case LiteralKind.Int64 when match.Kind == LiteralKind.Int64:
            case LiteralKind.Decimal when match.Kind == LiteralKind.Decimal:
            case LiteralKind.Boolean when match.Kind == LiteralKind.Boolean:
            case LiteralKind.DateTimeOffset when match.Kind == LiteralKind.DateTimeOffset:
            case LiteralKind.Guid when match.Kind == LiteralKind.Guid:
                value = match;
                return true;
            case LiteralKind.Decimal when match.Kind == LiteralKind.Int64:
                value = LiteralValue.OfDecimal(match.AsInt64());
                return true;
            case LiteralKind.Int64 when match.Kind == LiteralKind.Decimal:
                decimal whole = match.AsDecimal();

                // Exactly representable only: a fractional or out-of-range decimal is a mismatch, never rounded.
                if (decimal.IsInteger(whole) && whole >= long.MinValue && whole <= long.MaxValue)
                {
                    value = LiteralValue.OfInt64((long)whole);
                    return true;
                }

                return false;
            case LiteralKind.DateTimeOffset when match.Kind == LiteralKind.String:
                if (
                    DateTimeOffset.TryParse(
                        match.AsString(),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset date
                    )
                )
                {
                    value = LiteralValue.OfDateTimeOffset(date);
                    return true;
                }

                return false;
            case LiteralKind.Guid when match.Kind == LiteralKind.String:
                if (Guid.TryParse(match.AsString(), out Guid guid))
                {
                    value = LiteralValue.OfGuid(guid);
                    return true;
                }

                return false;
            default:
                return false;
        }
    }
}
