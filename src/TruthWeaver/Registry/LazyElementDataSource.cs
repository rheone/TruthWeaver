namespace TruthWeaver.Registry;

using TruthWeaver.Abstractions;

/// <summary>
/// Wraps one quantifier element as an <see cref="IDataSource"/>, deferring the projection
/// (<see cref="CollectionBinding{TContext, TItem}"/>'s <c>elementSource</c>) until the first query so a projector
/// that throws faults exactly like any other data source that fails (ADR-0001, ADR-0006 decision 9): the exception
/// surfaces from inside <see cref="QueryAsync"/> or <see cref="ScopeAsync"/>, where the evaluator's own backstop
/// around a source call already turns it into <c>Unknown</c> plus a <c>Fault</c>, rather than aborting the
/// quantifier's loop outright.
/// </summary>
/// <typeparam name="TItem">The element's type.</typeparam>
internal sealed class LazyElementDataSource<TItem>(TItem item, Func<TItem, IDataSource> projector) : IDataSource
{
    /// <inheritdoc />
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
    {
        return projector(item).QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
    {
        return projector(item).ScopeAsync(query, cancellationToken);
    }
}
