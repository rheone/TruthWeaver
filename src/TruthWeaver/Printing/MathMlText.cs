namespace TruthWeaver.Printing;

using System.Text;
using System.Xml;

/// <summary>
/// Writes literal text (predicate names, argument values and constant overrides) as <c>&lt;mtext&gt;</c> elements for
/// the MathML equation dialect. The result is character data that XML 1.0 accepts, so the whole equation is well-formed
/// XML for any term.
/// </summary>
internal static class MathMlText
{
    /// <summary>The MathML namespace, which the <c>&lt;math&gt;</c> element declares.</summary>
    public const string Namespace = "http://www.w3.org/1998/Math/MathML";

    /// <summary>The replacement character, which stands in for each character that XML 1.0 cannot hold.</summary>
    private const char Replacement = (char)0xFFFD;

    /// <summary>Writes literal text as an <c>&lt;mtext&gt;</c> element.</summary>
    /// <param name="value">The literal text.</param>
    /// <returns>
    /// The element. <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c> are escaped as entities, and a tab, line feed and carriage
    /// return as character references. A lone surrogate and each control
    /// character that XML 1.0 forbids become U+FFFD.
    /// </returns>
    public static string Text(string value)
    {
        StringBuilder builder = new("<mtext>", value.Length + 15);

        // DecodeFromUtf16 reads a valid surrogate pair as one supplementary character, which XML accepts, and gives
        // U+FFFD for a lone surrogate.
        int index = 0;
        while (index < value.Length)
        {
            _ = Rune.DecodeFromUtf16(value.AsSpan(index), out Rune rune, out int consumed);
            index += consumed;
            builder.Append(
                rune.Value switch
                {
                    '&' => "&amp;",
                    '<' => "&lt;",
                    '>' => "&gt;",

                    // A line break stays a character reference so that a legend line never breaks in the middle of a term.
                    '\n' => "&#xA;",
                    '\r' => "&#xD;",
                    '\t' => "&#x9;",
                    _ when !rune.IsBmp || XmlConvert.IsXmlChar((char)rune.Value) => rune.ToString(),
                    _ => Replacement.ToString(),
                }
            );
        }

        return builder.Append("</mtext>").ToString();
    }
}
