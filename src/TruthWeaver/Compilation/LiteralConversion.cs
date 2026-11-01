namespace TruthWeaver.Compilation;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Ast;
using TruthWeaver.Parsing;

/// <summary>
/// Reconciles a <see cref="RawLiteral"/> (the shape a literal took in whichever surface it was
/// parsed from) against a predicate argument's declared <see cref="LiteralKind"/>. This is the one
/// place a DSL string, a JSON scalar, and a YAML scalar all become the same <see cref="LiteralValue"/>, including the DSL's only way to express a <see cref="LiteralKind.DateTimeOffset"/>: a quoted
/// string the schema says to parse as a date rather than keep as text.
/// </summary>
internal static class LiteralConversion
{
    /// <summary>Attempts to convert a raw literal to the expected kind.</summary>
    /// <param name="raw">The raw literal, as parsed.</param>
    /// <param name="expectedKind">The kind the owning predicate's schema declares.</param>
    /// <param name="value">The converted value, if conversion succeeded.</param>
    /// <returns><see langword="true"/> if <paramref name="raw"/> matches <paramref name="expectedKind"/>.</returns>
    public static bool TryConvert(RawLiteral raw, LiteralKind expectedKind, out LiteralValue value)
    {
        if (IsArrayKind(expectedKind))
        {
            return TryConvertArray(raw, expectedKind, out value);
        }

        switch (expectedKind)
        {
            case LiteralKind.String when raw.Form == RawLiteralForm.QuotedString && raw.Text is not null:
                value = LiteralValue.OfString(raw.Text);
                return true;
            case LiteralKind.Int64
                when raw.Form == RawLiteralForm.Number
                    && raw.Text?.Contains('.') == false
                    && long.TryParse(raw.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue):
                value = LiteralValue.OfInt64(longValue);
                return true;
            case LiteralKind.Decimal
                when raw.Form == RawLiteralForm.Number
                    && raw.Text is not null
                    && decimal.TryParse(raw.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue):
                value = LiteralValue.OfDecimal(decimalValue);
                return true;
            case LiteralKind.Boolean when raw.Form == RawLiteralForm.Boolean:
                value = LiteralValue.OfBoolean(raw.BooleanValue);
                return true;
            case LiteralKind.DateTimeOffset
                when raw.Form == RawLiteralForm.QuotedString
                    && raw.Text is not null
                    && DateTimeText.TryParse(raw.Text, out DateTimeOffset dateTimeOffsetValue):
                value = LiteralValue.OfDateTimeOffset(dateTimeOffsetValue);
                return true;
            case LiteralKind.Guid
                when raw.Form == RawLiteralForm.QuotedString
                    && raw.Text is not null
                    && Guid.TryParse(raw.Text, out Guid guidValue):
                value = LiteralValue.OfGuid(guidValue);
                return true;
            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    /// Infers a <see cref="LiteralValue"/> from a raw literal with no declared schema to validate against: used only for
    /// a term bound to an unregistered predicate under <c>CompilationMode.Lenient</c>, where the term will never
    /// actually be evaluated (it is permanently <c>TruthValue.Unknown</c>) but still needs a stable identity for
    /// memoization. A literal that cannot be kept exactly is reported, never replaced by a default, because a default
    /// would make two different literals one term.
    /// </summary>
    /// <param name="raw">The raw literal.</param>
    /// <param name="value">The inferred value, if the literal could be kept exactly.</param>
    /// <param name="problem">Why the literal could not be kept, when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> holds the literal exactly as written.</returns>
    public static bool TryGuess(RawLiteral raw, out LiteralValue value, [NotNullWhen(false)] out string? problem)
    {
        problem = null;
        switch (raw.Form)
        {
            case RawLiteralForm.QuotedString:
                value = LiteralValue.OfString(raw.Text ?? string.Empty);
                return true;
            case RawLiteralForm.Boolean:
                value = LiteralValue.OfBoolean(raw.BooleanValue);
                return true;
            case RawLiteralForm.Number:
                return TryGuessNumber(raw.Text ?? "0", out value, out problem);
            default:
                return TryGuessArray(raw.Elements ?? [], out value, out problem);
        }
    }

    /// <summary>
    /// Tests whether a literal expected to be a date-time (or an array of them) holds text that reads as a date-time except
    /// for a missing <c>Z</c> or offset, so the caller can name that fix in its diagnostic.
    /// </summary>
    /// <param name="raw">The raw literal.</param>
    /// <param name="expectedKind">The kind the owning predicate's schema declares.</param>
    /// <returns><see langword="true"/> when the offset is the only problem.</returns>
    public static bool IsMissingOffset(RawLiteral raw, LiteralKind expectedKind)
    {
        return expectedKind switch
        {
            LiteralKind.DateTimeOffset => raw is { Form: RawLiteralForm.QuotedString, Text: { } text }
                && DateTimeText.IsMissingOffset(text),
            LiteralKind.DateTimeOffsetArray => raw.Elements?.Any(e => IsMissingOffset(e, LiteralKind.DateTimeOffset)) == true,
            _ => false,
        };
    }

    // A whole number that fits long stays Int64. Any other number is kept by value as a decimal, which holds a large
    // identifier or an exponent form such as 1e5 exactly. A number that neither type holds exactly is reported.
    private static bool TryGuessNumber(string text, out LiteralValue value, [NotNullWhen(false)] out string? problem)
    {
        problem = null;
        if (!text.Contains('.') && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long int64Value))
        {
            value = LiteralValue.OfInt64(int64Value);
            return true;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            value = LiteralValue.OfDecimal(decimalValue);
            return true;
        }

        value = default;
        problem = $"the number {text} is outside the range that an integer or a decimal holds exactly";
        return false;
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

    private static bool TryConvertArray(RawLiteral raw, LiteralKind expectedKind, out LiteralValue value)
    {
        if (raw.Form != RawLiteralForm.Array || raw.Elements is null)
        {
            value = default;
            return false;
        }

        LiteralKind elementKind = LiteralValue.ToElementKind(expectedKind);
        List<LiteralValue> items = [with(raw.Elements.Count)];
        foreach (RawLiteral element in raw.Elements)
        {
            if (!TryConvert(element, elementKind, out LiteralValue itemValue))
            {
                value = default;
                return false;
            }

            items.Add(itemValue);
        }

        value = LiteralValue.OfArray(elementKind, items);
        return true;
    }

    private static bool TryGuessArray(
        IReadOnlyList<RawLiteral> elements,
        out LiteralValue value,
        [NotNullWhen(false)] out string? problem
    )
    {
        problem = null;
        if (elements.Count == 0)
        {
            value = LiteralValue.OfArray(LiteralKind.String, []);
            return true;
        }

        if (!TryGuess(elements[0], out LiteralValue first, out problem))
        {
            value = default;
            return false;
        }

        if (IsArrayKind(first.Kind))
        {
            value = default;
            problem = "a list inside a list has no exact kind to keep";
            return false;
        }

        List<LiteralValue> guessed = [first];
        for (int i = 1; i < elements.Count; i++)
        {
            if (!TryGuess(elements[i], out LiteralValue candidate, out problem))
            {
                value = default;
                return false;
            }

            if (candidate.Kind != first.Kind)
            {
                value = default;
                problem = $"the list mixes {first.Kind} and {candidate.Kind} elements";
                return false;
            }

            guessed.Add(candidate);
        }

        value = LiteralValue.OfArray(first.Kind, guessed);
        return true;
    }
}
