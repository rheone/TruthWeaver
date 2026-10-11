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
}
