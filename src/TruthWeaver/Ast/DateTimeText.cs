namespace TruthWeaver.Ast;

using System.Globalization;

/// <summary>
/// The single reader for date-time text in a rule literal (DSL, JSON, YAML, <see cref="Building.RuleBuilder"/>) and in a
/// value a data source resolves. The text must end in <c>Z</c> or carry an offset such as <c>+02:00</c>. Text with no
/// offset is rejected instead of being read in the host's time zone, so a stored rule means the same instant on every host.
/// </summary>
internal static class DateTimeText
{
    /// <summary>The sentence a diagnostic appends so the author knows how to fix offset-less text.</summary>
    public const string OffsetFix = "A date-time must end in 'Z' or carry an offset such as '+02:00'.";

    /// <summary>Attempts to read date-time text that carries <c>Z</c> or an offset.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="value">The instant when the text has an offset and is a valid date-time.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a date-time with an explicit offset.</returns>
    public static bool TryParse(string text, out DateTimeOffset value)
    {
        // With RoundtripKind, DateTime keeps Unspecified only when the text names no offset or Z. The kind is read from the
        // text alone, so this check does not depend on the host time zone.
        if (
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime kindProbe)
            && kindProbe.Kind != DateTimeKind.Unspecified
        )
        {
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }

        value = default;
        return false;
    }

    /// <summary>Tests whether the text is a date-time that lacks the <c>Z</c> or offset this reader requires.</summary>
    /// <param name="text">The text to test.</param>
    /// <returns><see langword="true"/> when the text reads as a date-time except for the missing offset.</returns>
    public static bool IsMissingOffset(string text)
    {
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime probe)
            && probe.Kind == DateTimeKind.Unspecified;
    }
}
