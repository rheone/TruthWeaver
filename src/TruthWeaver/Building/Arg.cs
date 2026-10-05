namespace TruthWeaver.Building;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;

/// <summary>
/// Argument values for <see cref="RuleBuilder"/> that are not plain .NET values (ADR-0006 decision 12). Pass the result
/// wherever <c>RuleBuilder.Predicate</c> takes an argument value.
/// </summary>
public static class Arg
{
    /// <summary>
    /// Creates a deferred variable reference, the builder form of <c>from("source", "query")</c> in rule text: nothing is
    /// read now, and the value is resolved from the named data source on every evaluation. It renders to the same JSON
    /// tree and compiles to the same canonical text as the other rule formats.
    /// </summary>
    /// <param name="source">The data source name, which the compiler must have declared in <c>CompilerOptions.DataSources</c>.</param>
    /// <param name="query">The query, in the source's own dialect.</param>
    /// <param name="validator">
    /// An optional validator for the source's dialect. When given, a malformed query is rejected here instead of at
    /// compile time. It checks syntax only. <see langword="null"/> (the default) leaves the query unchecked until the
    /// compiler's own declaration check, if any.
    /// </param>
    /// <returns>The reference.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="validator"/> reported a problem with <paramref name="query"/>; the message lists each problem.</exception>
    public static VariableReference From(string source, string query, IQueryValidator? validator = null)
    {
        if (TryFrom(source, query, validator, out VariableReference? reference, out IReadOnlyList<QueryProblem> problems))
        {
            return reference;
        }

        // The validator's messages contain no data values (IQueryValidator contract), so they are safe to surface.
        string text = string.Join(
            "; ",
            problems.Select(p => p.Position is { } at ? $"{p.Message} (at position {at})" : p.Message)
        );
        throw new ArgumentException($"The query for source '{source}' is malformed: {text}", nameof(query));
    }

    /// <summary>
    /// The non-throwing form of <see cref="From"/>: a validator rejection is returned as data, so host code that takes the
    /// query from external input needs no exception handling. A null argument is still a programming error and throws.
    /// </summary>
    /// <param name="source">The data source name, which the compiler must have declared in <c>CompilerOptions.DataSources</c>.</param>
    /// <param name="query">The query, in the source's own dialect.</param>
    /// <param name="validator">An optional validator for the source's dialect. <see langword="null"/> leaves the query unchecked. It checks syntax only.</param>
    /// <param name="reference">The reference when the query is accepted; otherwise <see langword="null"/>.</param>
    /// <param name="problems">The validator's problems when the query is rejected; otherwise empty. The messages contain no data values.</param>
    /// <returns><see langword="true"/> when <paramref name="reference"/> is set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="query"/> is <see langword="null"/>.</exception>
    public static bool TryFrom(
        string source,
        string query,
        IQueryValidator? validator,
        [NotNullWhen(true)] out VariableReference? reference,
        out IReadOnlyList<QueryProblem> problems
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(query);

        problems = validator?.Validate(query) ?? [];
        reference = problems.Count == 0 ? new VariableReference(source, query) : null;
        return reference is not null;
    }
}
