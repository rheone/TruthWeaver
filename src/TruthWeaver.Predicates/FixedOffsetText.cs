namespace TruthWeaver.Predicates;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Parses the ISO 8601 text arguments of the fixed-offset calendar predicates: a zone offset, a time of day, a duration
/// and an English day name. Each parser returns an error message instead of throwing, so the same code serves the
/// compile-time argument validator and the evaluation-time check.
/// </summary>
/// <remarks>
/// Only the forms that the predicate documents describe are accepted. A wider ISO 8601 grammar (basic format, week dates,
/// day or year durations) is rejected on purpose: one accepted spelling per value keeps rule text predictable.
/// </remarks>
internal static partial class FixedOffsetText
{
    /// <summary>The largest offset a <see cref="DateTimeOffset"/> accepts, in either direction.</summary>
    private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14);

    private static readonly string[] DayNames = Enum.GetNames<DayOfWeek>();

    /// <summary>Parses <c>Z</c> or <c>±hh:mm</c> to an offset from UTC.</summary>
    /// <param name="text">The rule-text value.</param>
    /// <param name="offset">The offset when the text is valid.</param>
    /// <param name="error">Why the text is not valid; empty when it is.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid fixed offset.</returns>
    public static bool TryParseOffset(string text, out TimeSpan offset, out string error)
    {
        offset = TimeSpan.Zero;
        error = string.Empty;
        if (text == "Z")
        {
            return true;
        }

        Match match = OffsetPattern().Match(text);
        if (match.Success)
        {
            int hours = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
            int minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
            TimeSpan magnitude = new(hours, minutes, 0);
            if (minutes < 60 && magnitude <= MaxOffset)
            {
                offset = match.Groups["sign"].Value == "-" ? -magnitude : magnitude;
                return true;
            }

            error = $"'{text}' is outside the range of a fixed offset, -14:00 to +14:00.";
            return false;
        }

        // A name such as "Europe/Paris" or "UTC" is a time zone, and time zones carry daylight-saving rules that a
        // fixed offset cannot express. Say so plainly instead of a generic format error.
        error = TimeZoneNamePattern().IsMatch(text)
            ? $"'{text}' is a time zone name. Time zone names, such as the IANA name 'Europe/Paris', are not supported. Use 'Z' or a fixed offset such as '+01:00'."
            : $"'{text}' is not a fixed offset. Use 'Z' or '+hh:mm' / '-hh:mm', for example '+05:30'.";
        return false;
    }

    /// <summary>Parses <c>hh:mm</c> or <c>hh:mm:ss</c> (00:00 to 23:59:59) to a time of day.</summary>
    /// <param name="text">The rule-text value.</param>
    /// <param name="time">The time since midnight when the text is valid.</param>
    /// <param name="error">Why the text is not valid; empty when it is.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid time of day.</returns>
    public static bool TryParseTimeOfDay(string text, out TimeSpan time, out string error)
    {
        time = TimeSpan.Zero;
        error = string.Empty;
        Match match = TimeOfDayPattern().Match(text);
        if (match.Success)
        {
            int hours = int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture);
            int minutes = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
            int seconds = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture) : 0;
            if (hours < 24 && minutes < 60 && seconds < 60)
            {
                time = new TimeSpan(hours, minutes, seconds);
                return true;
            }
        }

        error = $"'{text}' is not a time of day. Use 'hh:mm' or 'hh:mm:ss' from 00:00 to 23:59:59, for example '09:00'.";
        return false;
    }

    /// <summary>Parses an ISO 8601 time duration (<c>PTnHnMnS</c>) that is longer than zero and shorter than 24 hours.</summary>
    /// <param name="text">The rule-text value.</param>
    /// <param name="duration">The duration when the text is valid.</param>
    /// <param name="error">Why the text is not valid; empty when it is.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a valid window duration.</returns>
    public static bool TryParseDuration(string text, out TimeSpan duration, out string error)
    {
        duration = TimeSpan.Zero;
        error = string.Empty;
        Match match = DurationPattern().Match(text);
        if (!match.Success || !(match.Groups["h"].Success || match.Groups["m"].Success || match.Groups["s"].Success))
        {
            error = $"'{text}' is not an ISO 8601 time duration. Use 'PTnHnMnS', for example 'PT8H' or 'PT1H30M'.";
            return false;
        }

        duration =
            TimeSpan.FromHours(Component(match, "h"))
            + TimeSpan.FromMinutes(Component(match, "m"))
            + TimeSpan.FromSeconds(Component(match, "s"));

        // Zero is an empty window and 24 hours or more covers the whole day, so neither describes a window.
        if (duration <= TimeSpan.Zero || duration >= TimeSpan.FromDays(1))
        {
            error = $"'{text}' must be longer than zero and shorter than 24 hours.";
            return false;
        }

        return true;
    }

    /// <summary>Parses an English day name (<c>Monday</c> to <c>Sunday</c>), ignoring case (ordinal).</summary>
    /// <param name="text">The rule-text value.</param>
    /// <param name="day">The day when the text is valid.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> names a day of the week.</returns>
    public static bool TryParseDayOfWeek(string text, out DayOfWeek day)
    {
        // Enum.TryParse also accepts digits and comma lists, so match against the names only.
        for (int index = 0; index < DayNames.Length; index++)
        {
            if (string.Equals(DayNames[index], text, StringComparison.OrdinalIgnoreCase))
            {
                day = (DayOfWeek)index;
                return true;
            }
        }

        day = default;
        return false;
    }

    private static int Component(Match match, string group)
    {
        return match.Groups[group].Success ? int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture) : 0;
    }

    [GeneratedRegex(@"^(?<sign>[+-])(?<h>[0-9]{2}):(?<m>[0-9]{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex OffsetPattern();

    [GeneratedRegex(@"^[A-Za-z_]+(?:/[A-Za-z_+\-0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex TimeZoneNamePattern();

    [GeneratedRegex(@"^(?<h>[0-9]{2}):(?<m>[0-9]{2})(?::(?<s>[0-9]{2}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex TimeOfDayPattern();

    // Each component is capped at five digits so the sum cannot overflow; the 24-hour check rejects large values.
    [GeneratedRegex(@"^PT(?:(?<h>[0-9]{1,5})H)?(?:(?<m>[0-9]{1,5})M)?(?:(?<s>[0-9]{1,5})S)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DurationPattern();
}
