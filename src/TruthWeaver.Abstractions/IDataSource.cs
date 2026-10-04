namespace TruthWeaver.Abstractions;

/// <summary>
/// A read-only store of application data that can answer queries in its own dialect (ADR-0006), for example a JSON
/// document, a database row or a feature-flag service. Core treats the query as an opaque string. An instance is supplied
/// to one evaluation through <see cref="DataSources"/>.
/// </summary>
public interface IDataSource
{
    /// <summary>Answers a query with every matched node converted to a <see cref="LiteralValue"/>.</summary>
    /// <param name="query">The query, in this source's dialect. Queries are absolute within the source.</param>
    /// <param name="cancellationToken">A token to honour; a timeout or cancellation becomes <c>Unknown</c> plus a fault.</param>
    /// <returns>The matches, or an error described as data (a malformed query, a failing backend, an unsupported node type).</returns>
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken);

    /// <summary>Returns a new source rooted at the single node <paramref name="query"/> matches.</summary>
    /// <param name="query">A query that matches exactly one node.</param>
    /// <param name="cancellationToken">A token to honour.</param>
    /// <returns>The narrowed source, whose queries are absolute within the matched node.</returns>
    public ValueTask<IDataSource> ScopeAsync(string query, CancellationToken cancellationToken);
}
