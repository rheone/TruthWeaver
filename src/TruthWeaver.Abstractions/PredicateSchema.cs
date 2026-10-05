namespace TruthWeaver.Abstractions;

/// <summary>
/// The compile-time-known shape of a predicate: its registered name, a short display label, a
/// human-readable description, and its named-argument declarations. <c>RuleCompiler</c> (in the
/// <c>TruthWeaver</c> package) validates every term against its predicate's schema, so a
/// missing or mistyped argument is a compile diagnostic rather than a runtime failure inside
/// <see cref="IPredicate{TContext}"/>'s evaluation method.
/// </summary>
/// <param name="Name">
/// The predicate's registered name. Term identity normalizes to this exact casing (CONTEXT.md).
/// </param>
/// <param name="Label">
/// A short, human-friendly display name for this predicate (e.g. "Has Role"), distinct from the
/// machine-facing <paramref name="Name"/> used in rule text. Required so a rule-authoring UI can
/// render every registered predicate without falling back to its raw identifier.
/// </param>
/// <param name="Description">
/// A human-readable, read-only description of what this predicate answers (e.g. "Does the current
/// user hold the given role?"). Required so a rule-authoring UI or generated documentation always
/// has something to show for every registered predicate, never an empty string.
/// </param>
/// <param name="Arguments">The argument declarations, or empty for a zero-argument predicate.</param>
public sealed record PredicateSchema(
    string Name,
    string Label,
    string Description,
    IReadOnlyList<PredicateArgumentSchema> Arguments
)
{
    /// <summary>
    /// Gets an optional check over the values of a call's literal arguments, for a rule between arguments that their
    /// kinds cannot express (for example "lower must not be greater than upper"). <see langword="null"/> (the default)
    /// checks nothing more than the argument declarations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The compiler calls it once per call of this predicate, after every argument is present and of its declared kind,
    /// and only when that check found no error. Its input holds only the literal arguments, defaults included. An argument
    /// whose value comes from a data source is absent, because its value is known only at evaluation, so a check that
    /// needs it must skip. Each returned <see cref="PredicateArgumentProblem"/> becomes one <c>TRE0026</c> error at the call,
    /// and the rule does not compile. An empty list means the arguments are acceptable.
    /// </para>
    /// <para>
    /// The check must be pure, stateless and thread-safe, and must not throw: the compiler may share it across
    /// compilations and threads. A predicate that can receive a non-literal value must still check that value at
    /// evaluation.
    /// </para>
    /// </remarks>
    public Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>>? ArgumentValidator { get; init; }

    /// <summary>Creates a schema for a zero-argument predicate.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of what this predicate answers.</param>
    /// <returns>A schema with no arguments.</returns>
    public static PredicateSchema NoArguments(string name, string label, string description)
    {
        return new(name, label, description, []);
    }
}
