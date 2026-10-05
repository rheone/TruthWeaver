namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>
/// An in-memory <see cref="IDataSource"/> for tests of rules that use variable references (ADR-0006), so a test needs no
/// JSON parsing and no query engine. A query is matched by its exact text: <see cref="With(string, long)"/> and its
/// siblings script what a query returns, <see cref="Failing"/> and <see cref="Throwing"/> script a failure, and a query
/// nobody scripted matches nothing. Every query received is recorded in <see cref="Queries"/>, which is how a test
/// checks that a repeated reference is asked only once.
/// </summary>
public sealed class FakeDataSource : IDataSource
{
    private readonly Dictionary<string, Func<DataQueryResult>> answers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DataScopeResult> scopes = new(StringComparer.Ordinal);
    private readonly List<string> queries = [];

    /// <summary>Gets every query this source has been asked, in order, including repeats.</summary>
    public IReadOnlyList<string> Queries => this.queries;

    /// <summary>Scripts <paramref name="query"/> to match one string.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, string value)
    {
        return this.WithMatches(query, [LiteralValue.OfString(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match one integer.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, long value)
    {
        return this.WithMatches(query, [LiteralValue.OfInt64(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match one decimal.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, decimal value)
    {
        return this.WithMatches(query, [LiteralValue.OfDecimal(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match one boolean.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, bool value)
    {
        return this.WithMatches(query, [LiteralValue.OfBoolean(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match one date and time.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, DateTimeOffset value)
    {
        return this.WithMatches(query, [LiteralValue.OfDateTimeOffset(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match one GUID.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="value">The matched value.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, Guid value)
    {
        return this.WithMatches(query, [LiteralValue.OfGuid(value)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match several strings, one match each.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="values">The matched values, in document order.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, IEnumerable<string> values)
    {
        return this.WithMatches(query, [.. values.Select(LiteralValue.OfString)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match several integers, one match each.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="values">The matched values, in document order.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource With(string query, IEnumerable<long> values)
    {
        return this.WithMatches(query, [.. values.Select(LiteralValue.OfInt64)]);
    }

    /// <summary>Scripts <paramref name="query"/> to match exactly the given literals, one match each. Use it for a mix of kinds.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="matches">The matched values, in document order; empty for a query that matches nothing.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource WithMatches(string query, IEnumerable<LiteralValue> matches)
    {
        LiteralValue[] snapshot = [.. matches];
        this.answers[query] = () => DataQueryResult.Success(snapshot);
        return this;
    }

    /// <summary>Scripts <paramref name="query"/> to fail as data, the way a source reports a failing backend or a malformed query.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="kind">Why the query failed; defaults to <see cref="DataQueryErrorKind.SourceFailure"/>.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource Failing(string query, string message, DataQueryErrorKind kind = DataQueryErrorKind.SourceFailure)
    {
        this.answers[query] = () => DataQueryResult.Failure(kind, message);
        return this;
    }

    /// <summary>Scripts <paramref name="query"/> to throw, the way a misbehaving source or an elapsed timeout does.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="exception">The exception every call throws.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource Throwing(string query, Exception exception)
    {
        this.answers[query] = () => throw exception;
        return this;
    }

    /// <summary>Scripts <see cref="ScopeAsync"/> for <paramref name="query"/> to return <paramref name="scope"/>.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="scope">The source returned as the narrowed scope.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource WithScope(string query, IDataSource scope)
    {
        this.scopes[query] = DataScopeResult.Success(scope);
        return this;
    }

    /// <summary>Scripts <see cref="ScopeAsync"/> for <paramref name="query"/> to fail as data, the way a source reports a query that matches no node or several.</summary>
    /// <param name="query">The exact query text.</param>
    /// <param name="message">The failure description.</param>
    /// <param name="kind">Why the scope failed; defaults to <see cref="DataQueryErrorKind.NoMatch"/>.</param>
    /// <returns>This source, for chaining.</returns>
    public FakeDataSource FailingScope(string query, string message, DataQueryErrorKind kind = DataQueryErrorKind.NoMatch)
    {
        this.scopes[query] = DataScopeResult.Failure(kind, message);
        return this;
    }

    /// <inheritdoc />
    public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
    {
        this.queries.Add(query);
        return ValueTask.FromResult(
            this.answers.TryGetValue(query, out Func<DataQueryResult>? answer) ? answer() : DataQueryResult.Empty()
        );
    }

    /// <inheritdoc />
    /// <remarks>A query nobody scripted with <see cref="WithScope"/> or <see cref="FailingScope"/> matches no node, so it is a <see cref="DataQueryErrorKind.NoMatch"/> failure.</remarks>
    public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(
            this.scopes.TryGetValue(query, out DataScopeResult? scope)
                ? scope
                : DataScopeResult.Failure(DataQueryErrorKind.NoMatch, "No scope was scripted for this query. Use WithScope.")
        );
    }
}
