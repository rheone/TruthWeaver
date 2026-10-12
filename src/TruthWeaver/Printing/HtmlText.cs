namespace TruthWeaver.Printing;

using System.Text;

/// <summary>
/// The one routine that writes text into HTML output. Every term name, argument value, constant and operator symbol
/// goes through it, so no caller can put a raw markup character into the fragment.
/// </summary>
internal static class HtmlText
{
    /// <summary>Stands in for a control character or a lone surrogate, which HTML cannot hold.</summary>
    private static readonly string Replacement = Rune.ReplacementChar.ToString();

    /// <summary>Escapes text for use as HTML element content or as a double-quoted attribute value.</summary>
    /// <param name="value">The literal text.</param>
    /// <returns>
    /// The text with <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>"</c> and <c>'</c> as entities. Each logic glyph that the
    /// printers write (<c>∧ ∨ ¬ ⊕ ↔ → ↑ ↓ ⊤ ⊥</c>) becomes its named entity. A tab, line feed and carriage return become
    /// character references. Each other control character and each lone surrogate becomes U+FFFD.
    /// </returns>
    public static string Escape(string value)
    {
        StringBuilder builder = new(value.Length + 8);
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
                    '"' => "&quot;",
                    '\'' => "&#39;",
                    '\t' => "&#9;",
                    '\n' => "&#10;",
                    '\r' => "&#13;",
                    '∧' => "&and;",
                    '∨' => "&or;",
                    '¬' => "&not;",
                    '⊕' => "&oplus;",
                    '↔' => "&harr;",
                    '→' => "&rarr;",
                    '↑' => "&uarr;",
                    '↓' => "&darr;",
                    '⊤' => "&top;",
                    '⊥' => "&perp;",
                    < ' ' or 0x7F or (>= 0x80 and <= 0x9F) => Replacement,
                    _ => rune.ToString(),
                }
            );
        }

        return builder.ToString();
    }
}
