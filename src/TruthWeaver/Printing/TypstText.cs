namespace TruthWeaver.Printing;

using System.Globalization;
using System.Text;

/// <summary>
/// Quotes literal text (predicate names, argument values and constant overrides) as a Typst string for the Typst
/// equation dialect. A Typst string reads every character literally except the backslash and the double quote.
/// </summary>
internal static class TypstText
{
    /// <summary>Writes literal text as a Typst string.</summary>
    /// <param name="value">The literal text.</param>
    /// <returns>
    /// The text between double quotes. A double quote and a backslash get a backslash before them. A line feed,
    /// carriage return and tab become <c>\n</c>, <c>\r</c> and <c>\t</c>. Each other control character becomes
    /// <c>\u{X}</c> with the code point in hexadecimal.
    /// </returns>
    public static string Quote(string value)
    {
        StringBuilder builder = new("\"", value.Length + 2);
        foreach (char c in value)
        {
            builder.Append(
                c switch
                {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ when char.IsControl(c) => $"\\u{{{((int)c).ToString("X", CultureInfo.InvariantCulture)}}}",
                    _ => c.ToString(),
                }
            );
        }

        return builder.Append('"').ToString();
    }
}
