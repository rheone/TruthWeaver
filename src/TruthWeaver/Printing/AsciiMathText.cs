namespace TruthWeaver.Printing;

/// <summary>
/// Quotes literal text (predicate names and argument values) for the AsciiMath equation dialect. AsciiMath shows text
/// between double quotes and reads every character in it literally, so the only characters that need work are the
/// ones that end the quoted run or the backtick envelope.
/// </summary>
internal static class AsciiMathText
{
    /// <summary>The right double quotation mark, which stands in for a straight quote because AsciiMath has no escape for one.</summary>
    private const char QuoteReplacement = '”';

    /// <summary>The reversed prime, which stands in for a backtick because a backtick ends the AsciiMath envelope.</summary>
    private const char BacktickReplacement = '‵';

    /// <summary>Writes literal text as an AsciiMath quoted run.</summary>
    /// <param name="value">The literal text.</param>
    /// <returns>The text between double quotes, with each straight quote and backtick replaced.</returns>
    public static string Quote(string value)
    {
        return $"\"{value.Replace('"', QuoteReplacement).Replace('`', BacktickReplacement)}\"";
    }
}
