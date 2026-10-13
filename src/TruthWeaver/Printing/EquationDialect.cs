namespace TruthWeaver.Printing;

/// <summary>The notation that <see cref="Evaluation.CompiledRule{TContext}.PrintEquation(EquationOptions?)"/> writes an equation in.</summary>
public enum EquationDialect
{
    /// <summary>
    /// Plain Unicode text with the <see cref="OperatorStyle.Symbolic"/> glyphs, for example <c>a ∧ (b ∨ c)</c>. The
    /// output has no delimiters and no escaping.
    /// </summary>
    Unicode,

    /// <summary>
    /// LaTeX math with the commands <c>\land</c>, <c>\lor</c>, <c>\lnot</c>, <c>\oplus</c> and <c>\leftrightarrow</c>.
    /// A term is a <c>\text</c> group with its special characters escaped. The envelope is set by
    /// <see cref="EquationOptions.LatexWrap"/>.
    /// </summary>
    LaTeX,

    /// <summary>
    /// AsciiMath between backticks, for example <c>`"a" ^^ ("b" vv "c")`</c>. A term is a quoted run. AsciiMath has
    /// no escape for a quote, so the printer replaces each straight quote and each backtick in a term.
    /// </summary>
    AsciiMath,

    /// <summary>
    /// C-family operator notation, for example <c>a &amp;&amp; (b || c)</c>, with the <see cref="OperatorStyle.CStyle"/>
    /// glyphs: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>, <c>^</c>, <c>==</c>. <c>If(c, t, f)</c> prints as the ternary
    /// <c>c ? t : f</c> instead of a call. <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c> have no C-family spelling and
    /// keep their word form; <c>COALESCE</c> has no call syntax in C and keeps the function-call form. This is a
    /// rendering aid, not C# semantics: <c>==</c> for <c>EQUIVALENT</c> and <c>?:</c> for <c>If</c> follow Strong K3,
    /// so <c>Unknown == Unknown</c> prints as <c>==</c> but does not mean what C#'s <c>==</c> means for <c>null</c>.
    /// </summary>
    CStyle,

    /// <summary>
    /// Presentation MathML in one <c>&lt;math xmlns="http://www.w3.org/1998/Math/MathML"&gt;</c> element. An operator is an
    /// <c>&lt;mo&gt;</c> element and a term, a constant and a function name are <c>&lt;mtext&gt;</c> elements, with the
    /// text XML-escaped. The output is well-formed XML for any rule: the printer replaces each character that XML 1.0
    /// cannot hold with U+FFFD.
    /// </summary>
    MathML,

    /// <summary>
    /// Typst math markup between <c>$</c> signs, for example <c>$"a" and ("b" or "c")$</c>. A term is a quoted string
    /// with each double quote and backslash escaped. An operator with no Typst symbol keeps the function-call form.
    /// </summary>
    Typst,

    /// <summary>
    /// Plain text of the 7-bit ASCII range only, for example <c>a /\ (b \/ c)</c>. The output holds only the
    /// characters U+0020 to U+007E and line feeds. The printer writes each backslash in a term as two backslashes, and each
    /// other character outside that range as <c>\uXXXX</c>, where <c>XXXX</c> is the UTF-16 code unit in hexadecimal.
    /// </summary>
    Ascii,
}
