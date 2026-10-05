namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

/// <summary>The variable reference value, the term identity that carries it, and the data source kernel types (ADR-0006).</summary>
public sealed class VariableReferenceTests
{
    /// <summary>A reference renders as the canonical DSL <c>from(...)</c> call.</summary>
    [Fact]
    public void ToString_SourceAndQuery_RendersCanonicalFromCall_Test()
    {
        VariableReference reference = new("user", "$.minAge");

        Assert.Equal("from(\"user\", \"$.minAge\")", reference.ToString());
    }

    /// <summary>A quote or backslash in the query is escaped so the rendered text is valid DSL.</summary>
    [Fact]
    public void ToString_QueryWithQuotesAndBackslashes_EscapesThem_Test()
    {
        VariableReference reference = new("doc", "$.a[?@.n==\"x\\y\"]");

        Assert.Equal("from(\"doc\", \"$.a[?@.n==\\\"x\\\\y\\\"]\")", reference.ToString());
    }

    /// <summary>Two references are equal only when the source name and the query text both match exactly.</summary>
    [Fact]
    public void Equals_SameTextDifferentQuery_AreNotEqual_Test()
    {
        Assert.Equal(new VariableReference("user", "$.a"), new VariableReference("user", "$.a"));
        Assert.NotEqual(new VariableReference("user", "$.a"), new VariableReference("user", "$.b"));
        Assert.NotEqual(new VariableReference("user", "$.a"), new VariableReference("request", "$.a"));
    }

    /// <summary>The same predicate and variable references, given in a different order, are the same identity.</summary>
    [Fact]
    public void Equals_VariablesInDifferentOrder_AreTheSameIdentity_Test()
    {
        TermIdentity first = new(
            "between",
            [],
            [
                new KeyValuePair<string, VariableReference>("low", new VariableReference("a", "$.lo")),
                new KeyValuePair<string, VariableReference>("high", new VariableReference("a", "$.hi")),
            ]
        );
        TermIdentity second = new(
            "between",
            [],
            [
                new KeyValuePair<string, VariableReference>("high", new VariableReference("a", "$.hi")),
                new KeyValuePair<string, VariableReference>("low", new VariableReference("a", "$.lo")),
            ]
        );

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>Different queries are different variables, even though the data behind them might be equal.</summary>
    [Fact]
    public void Equals_DifferentQueries_AreDifferentIdentities_Test()
    {
        TermIdentity first = new(
            "p",
            [],
            [new KeyValuePair<string, VariableReference>("v", new VariableReference("a", "$.x"))]
        );
        TermIdentity second = new(
            "p",
            [],
            [new KeyValuePair<string, VariableReference>("v", new VariableReference("a", "$.y"))]
        );

        Assert.NotEqual(first, second);
    }

    /// <summary>A variable argument is never equal to a literal argument, even a literal that spells the same query.</summary>
    [Fact]
    public void Equals_VariableAgainstLiteralOfSameName_AreDifferentIdentities_Test()
    {
        TermIdentity variable = new(
            "p",
            [],
            [new KeyValuePair<string, VariableReference>("v", new VariableReference("a", "$.x"))]
        );
        TermIdentity literal = new("p", [new KeyValuePair<string, LiteralValue>("v", LiteralValue.OfString("$.x"))]);

        Assert.NotEqual(variable, literal);
    }

    /// <summary>The identity's text interleaves literal and variable arguments in name order.</summary>
    [Fact]
    public void ToString_LiteralAndVariableArguments_AreSortedByNameTogether_Test()
    {
        TermIdentity identity = new(
            "p",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))],
            [new KeyValuePair<string, VariableReference>("min", new VariableReference("user", "$.minAge"))]
        );

        Assert.Equal("p(min: from(\"user\", \"$.minAge\"), role: \"Y\")", identity.ToString());
        Assert.Equal("min: from(\"user\", \"$.minAge\"), role: \"Y\"", identity.FormatArguments());
    }

    /// <summary>A term with no arguments has no argument text.</summary>
    [Fact]
    public void FormatArguments_NoArguments_ReturnsNull_Test()
    {
        Assert.Null(new TermIdentity("p", []).FormatArguments());
    }

    /// <summary>A successful result carries its matches and no error.</summary>
    [Fact]
    public void Success_WithMatches_ExposesThemAndNoError_Test()
    {
        DataQueryResult result = DataQueryResult.Success([LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Matches.Count);
        Assert.Null(result.ErrorKind);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>A failed result carries its kind and message and no matches.</summary>
    [Fact]
    public void Failure_KindAndMessage_AreExposedWithNoMatches_Test()
    {
        DataQueryResult result = DataQueryResult.Failure(DataQueryErrorKind.MalformedQuery, "bad query");

        Assert.False(result.Succeeded);
        Assert.Equal(DataQueryErrorKind.MalformedQuery, result.ErrorKind);
        Assert.Equal("bad query", result.ErrorMessage);
        Assert.Empty(result.Matches);
    }

    /// <summary>A source registered under a name is found by that exact, case-sensitive name.</summary>
    [Fact]
    public void TryGet_RegisteredName_FindsTheSourceCaseSensitively_Test()
    {
        IDataSource source = new NoOpSource();
        DataSources sources = new() { ["user"] = source };

        Assert.True(sources.TryGet("user", out IDataSource? found));
        Assert.Same(source, found);
        Assert.False(sources.TryGet("User", out _));
        Assert.Equal(1, sources.Count);
    }

    /// <summary>Adding a second source under the same name is rejected rather than silently replacing the first.</summary>
    [Fact]
    public void Add_DuplicateName_Throws_Test()
    {
        DataSources sources = [];
        sources.Add("user", new NoOpSource());

        Assert.Throws<ArgumentException>(() => sources.Add("user", new NoOpSource()));
    }

    /// <summary>A resolution exception exposes the reference and the failure kind it was built with.</summary>
    [Fact]
    public void Constructor_ReferenceAndKind_AreExposed_Test()
    {
        VariableReference reference = new("user", "$.minAge");

        VariableResolutionException exception = new(reference, VariableFailureKind.Ambiguous, "two matches");

        Assert.Equal(reference, exception.Reference);
        Assert.Equal(VariableFailureKind.Ambiguous, exception.Kind);
        Assert.Equal("two matches", exception.Message);
    }

    private sealed class NoOpSource : IDataSource
    {
        public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(DataQueryResult.Empty());
        }

        public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(DataScopeResult.Success(this));
        }
    }
}
