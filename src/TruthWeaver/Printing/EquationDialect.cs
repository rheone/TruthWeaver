namespace TruthWeaver.Printing;

/// <summary>The notation that <see cref="Evaluation.CompiledRule{TContext}.PrintEquation"/> writes an equation in.</summary>
public enum EquationDialect
{
    /// <summary>
    /// Plain Unicode text with the <see cref="OperatorStyle.Symbolic"/> glyphs, for example <c>a ∧ (b ∨ c)</c>. The
    /// output has no delimiters and no escaping.
    /// </summary>
    Unicode,
}
