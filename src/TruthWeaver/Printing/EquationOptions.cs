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
}
