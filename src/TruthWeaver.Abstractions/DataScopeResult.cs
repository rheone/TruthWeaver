namespace TruthWeaver.Abstractions;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// What <see cref="IDataSource.ScopeAsync"/> answers (ADR-0006 decision 8): either the narrowed <see cref="IDataSource"/>,
/// or an error described as data. A wrong scope query is host code feeding external input, so it is a result and not an
/// exception, the same way a failed query is a <see cref="DataQueryResult"/>.
/// </summary>
public sealed class DataScopeResult
{
    private DataScopeResult(IDataSource? source, DataQueryErrorKind? errorKind, string? errorMessage)
    {
        this.Source = source;
        this.ErrorKind = errorKind;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>Gets the narrowed source, or <see langword="null"/> when <see cref="Succeeded"/> is <see langword="false"/>.</summary>
    public IDataSource? Source { get; }

    /// <summary>Gets why the scope failed, or <see langword="null"/> when it succeeded.</summary>
    public DataQueryErrorKind? ErrorKind { get; }

    /// <summary>Gets the source's description of the failure, or <see langword="null"/> when the scope succeeded. It must not contain data values.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Gets a value indicating whether the query matched exactly one node and <see cref="Source"/> is set.</summary>
    [MemberNotNullWhen(true, nameof(Source))]
    [MemberNotNullWhen(false, nameof(ErrorKind), nameof(ErrorMessage))]
    public bool Succeeded => this.ErrorKind is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="source">The source rooted at the matched node.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static DataScopeResult Success(IDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new(source, null, null);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="kind">Why the scope failed.</param>
    /// <param name="message">A description of the failure. It must not contain data values.</param>
    /// <returns>The failed result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public static DataScopeResult Failure(DataQueryErrorKind kind, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new(null, kind, message);
    }
}
