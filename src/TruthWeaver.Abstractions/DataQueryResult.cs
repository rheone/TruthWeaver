namespace TruthWeaver.Abstractions;

/// <summary>
/// What an <see cref="IDataSource"/> answers to one query (ADR-0006 decision 9): either every matched node already
/// converted to a <see cref="LiteralValue"/>, or an error described as data. Zero matches is a successful, empty
/// result; whether that is a fault depends on the argument that asked (a scalar needs one match, an array accepts none).
/// </summary>
public sealed class DataQueryResult
{
    private DataQueryResult(IReadOnlyList<LiteralValue> matches, DataQueryErrorKind? errorKind, string? errorMessage)
    {
        this.Matches = matches;
        this.ErrorKind = errorKind;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>Gets the matched nodes in document order; empty when nothing matched or when <see cref="Succeeded"/> is <see langword="false"/>.</summary>
    public IReadOnlyList<LiteralValue> Matches { get; }

    /// <summary>Gets why the query failed, or <see langword="null"/> when it succeeded.</summary>
    public DataQueryErrorKind? ErrorKind { get; }

    /// <summary>Gets the source's description of the failure, or <see langword="null"/> when the query succeeded. It must not contain data values.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets a value indicating whether the query was answered (possibly with no matches).</summary>
    public bool Succeeded => this.ErrorKind is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="matches">The matched nodes converted to literals, in document order.</param>
    /// <returns>The result.</returns>
    public static DataQueryResult Success(IEnumerable<LiteralValue> matches)
    {
        return new([.. matches], null, null);
    }

    /// <summary>Creates a successful result with no matches.</summary>
    /// <returns>The empty result.</returns>
    public static DataQueryResult Empty()
    {
        return new([], null, null);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="kind">Why the query failed.</param>
    /// <param name="message">A description of the failure. It must not contain data values.</param>
    /// <returns>The failed result.</returns>
    public static DataQueryResult Failure(DataQueryErrorKind kind, string message)
    {
        return new([], kind, message);
    }
}
