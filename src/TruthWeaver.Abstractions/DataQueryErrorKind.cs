namespace TruthWeaver.Abstractions;

/// <summary>Why a <see cref="IDataSource"/> could not answer a query, reported as data in a <see cref="DataQueryResult"/> or <see cref="DataScopeResult"/>.</summary>
public enum DataQueryErrorKind
{
    /// <summary>The query is not valid in the source's dialect.</summary>
    MalformedQuery,

    /// <summary>The source or its backend failed, for example an unreachable store or a timeout.</summary>
    SourceFailure,

    /// <summary>A matched node has no <see cref="LiteralValue"/> equivalent, such as an object.</summary>
    UnsupportedType,

    /// <summary>
    /// A scope query matched no node. Reported by <see cref="IDataSource.ScopeAsync"/>, where a scalar query result can
    /// never be empty. A <see cref="IDataSource.QueryAsync"/> implementation may also report this for a scalar query it
    /// knows matched nothing (for example, no row in a backing store); both the eager
    /// <c>TruthWeaver.Building.DataSourceExtensions.TryGetAsync{T}</c> read and a variable classify it as missing, the
    /// same outcome a zero-match <see cref="DataQueryResult.Success"/> already gets (ticket 31).
    /// </summary>
    NoMatch,

    /// <summary>
    /// A scope query matched more than one node. Reported by <see cref="IDataSource.ScopeAsync"/>, which narrows to
    /// exactly one. A <see cref="IDataSource.QueryAsync"/> implementation may also report this for a scalar query it
    /// knows matched several (ticket 31); both the eager read and a variable classify it as ambiguous.
    /// </summary>
    AmbiguousMatch,
}
