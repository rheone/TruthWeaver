namespace TruthWeaver.Abstractions;

/// <summary>
/// Checks the syntax of queries in one data source's dialect without any data (ADR-0006 decision 4), so a malformed
/// query is a compile diagnostic rather than a fault at the first evaluation. Declare it with the source name in
/// <c>DataSourceDeclarations</c>. A validator checks syntax only: it cannot know whether the data holds a match, so a
/// well-formed query can still be missing at evaluation.
/// </summary>
/// <remarks>Implementations must be stateless and thread-safe: the compiler may share one instance across compilations and threads.</remarks>
public interface IQueryValidator
{
    /// <summary>Checks the syntax of a query.</summary>
    /// <param name="query">The query exactly as written in the rule, after the rule syntax's own string escapes are resolved.</param>
    /// <returns>The problems found, in query order; empty when the query is well-formed.</returns>
    public IReadOnlyList<QueryProblem> Validate(string query);
}
