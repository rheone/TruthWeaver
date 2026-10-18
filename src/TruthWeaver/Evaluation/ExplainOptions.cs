namespace TruthWeaver.Evaluation;

/// <summary>
/// Options for <c>CompiledRule.Explain</c>. The sentence template is fixed English and has no template engine and no
/// localization surface; a host changes the wording of a term only through <see cref="Labels"/>.
/// </summary>
public sealed record ExplainOptions
{
    /// <summary>
    /// Gets the label to use for a predicate instead of the label in its registered schema, keyed by predicate name
    /// (compared case-insensitively). <see langword="null"/> (the default) uses the schema label of every predicate.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// Gets a value indicating whether each term is followed by its predicate description in square brackets.
    /// The default is <see langword="false"/>.
    /// </summary>
    public bool IncludeDescriptions { get; init; }
}
