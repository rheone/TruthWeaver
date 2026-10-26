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
/// <remarks>
/// The projector runs at most once per instance (ADR-0010 amendment: "the delegate runs at the first query of that
/// element"): the projected source, or the exception the projector threw, is kept and reused by every later query, so
/// several distinct <c>from(variable, ...)</c> queries in one body see one view of the element and pay for one
/// projection. One instance exists per element per quantifier evaluation.
/// </remarks>
/// <param name="projector">Projects the element to its source; called at most once.</param>
/// <param name="memoScope">The memoization scope path of the element this source is bound to (for example <c>lines[2]</c>).</param>
internal sealed class LazyElementDataSource(Func<IDataSource> projector, string memoScope) : IDataSource
{
    // Lazy<T> built from a factory caches a thrown exception as well as a value, so a throwing projector is not
    // re-run either: each later query rethrows the same exception, and each becomes its own source-error fault.
    private readonly Lazy<IDataSource> projected = new(projector);

    /// <summary>
    /// Gets the memoization scope path of the element this source is bound to, which the evaluator keys this
    /// source's answers by (see <c>Evaluator&lt;TContext&gt;.QuerySourceAsync</c>).
    /// </summary>
    public string MemoScope => memoScope;

    /// <inheritdoc />
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
    {
        return this.projected.Value.QueryAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
    {
        return this.projected.Value.ScopeAsync(query, cancellationToken);
    }
}
