namespace TruthWeaver.Printing;

/// <summary>The notation that <see cref="Evaluation.CompiledRule{TContext}.PrintEquation"/> writes an equation in.</summary>
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
}
