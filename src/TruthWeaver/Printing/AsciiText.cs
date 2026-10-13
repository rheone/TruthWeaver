namespace TruthWeaver.Printing;

using System.Globalization;
using System.Text;

/// <summary>
/// Escapes literal text (predicate names, argument values and constant overrides) to the printable 7-bit ASCII range
/// for the plain ASCII equation dialect.
/// </summary>
internal static class AsciiText
{
    /// <summary>Escapes literal text so that it holds only the characters U+0020 to U+007E.</summary>
    /// <param name="value">The literal text.</param>
    /// <returns>
    /// The text with each backslash written as two backslashes and each character outside U+0020 to U+007E
    /// (a control character, a line break, or any non-ASCII character) written as <c>\uXXXX</c>, where
    /// <c>XXXX</c> is its UTF-16 code unit in four uppercase hexadecimal digits. A character outside the basic plane is
    /// two escapes, one for each surrogate.
    /// </returns>
    public static string Escape(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            if (c == '\\')
            {
                builder.Append("\\\\");
            }
            else if (c is >= ' ' and <= '~')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }
}
