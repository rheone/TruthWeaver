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

    /// <summary>A scope query matched no node. Reported by <see cref="IDataSource.ScopeAsync"/>, where a scalar query result can never be empty.</summary>
    NoMatch,

    /// <summary>A scope query matched more than one node. Reported by <see cref="IDataSource.ScopeAsync"/>, which narrows to exactly one.</summary>
    AmbiguousMatch,
}
