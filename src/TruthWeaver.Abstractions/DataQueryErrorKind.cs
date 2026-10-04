namespace TruthWeaver.Abstractions;

/// <summary>Why a <see cref="IDataSource"/> could not answer a query, reported as data in a <see cref="DataQueryResult"/>.</summary>
public enum DataQueryErrorKind
{
    /// <summary>The query is not valid in the source's dialect.</summary>
    MalformedQuery,

    /// <summary>The source or its backend failed, for example an unreachable store or a timeout.</summary>
    SourceFailure,

    /// <summary>A matched node has no <see cref="LiteralValue"/> equivalent, such as an object.</summary>
    UnsupportedType,
}
