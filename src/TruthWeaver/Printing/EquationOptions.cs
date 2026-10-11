namespace TruthWeaver.Printing;

/// <summary>The options for <see cref="Evaluation.CompiledRule{TContext}.PrintEquation"/>.</summary>
public sealed record EquationOptions
{
    /// <summary>Gets the default options: the <see cref="EquationDialect.Unicode"/> dialect with argument values shown.</summary>
    public static EquationOptions Default { get; } = new();

    /// <summary>Gets the notation to write the equation in. The default is <see cref="EquationDialect.Unicode"/>.</summary>
    public EquationDialect Dialect { get; init; } = EquationDialect.Unicode;

    /// <summary>
    /// Gets a value indicating whether a term prints as its full call, for example <c>hasCrust(crust: "thin")</c>. When
    /// <see langword="false"/>, a term prints as the predicate name only, for example <c>hasCrust</c>. The default is
    /// <see langword="true"/>.
    /// </summary>
    public bool ShowArgumentValues { get; init; } = true;

    /// <summary>
    /// Gets the envelope for the <see cref="EquationDialect.LaTeX"/> dialect. Other dialects ignore it. The default is
    /// <see cref="LatexWrapMode.None"/>.
    /// </summary>
    public LatexWrapMode LatexWrap { get; init; } = LatexWrapMode.None;

    /// <summary>
    /// Gets a value indicating whether each term prints as a letter (<c>p</c>, <c>q</c>, <c>r</c>) instead of its call.
    /// Letters follow the first occurrence of a term, depth first and left to right, and identical terms share a
    /// letter. After the eleventh distinct term (<c>z</c>), the letters repeat with a numeric subscript:
    /// <c>p₁</c>, <c>q₁</c> and so on. <see cref="Evaluation.CompiledRule{TContext}.PrintEquation"/> then returns
    /// only the equation. Use <see cref="Evaluation.CompiledRule{TContext}.PrintEquationWithLegend"/> to get the legend
    /// too. The default is <see langword="false"/>.
    /// </summary>
    public bool SimpleVariables { get; init; }
}
