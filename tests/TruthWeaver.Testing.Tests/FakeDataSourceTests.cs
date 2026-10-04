namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;

/// <summary>The scripted in-memory data source used by tests of variable references.</summary>
public sealed class FakeDataSourceTests
{
    /// <summary>A scripted query returns its match and an unscripted query matches nothing.</summary>
    [Fact]
    public async Task QueryAsync_ScriptedAndUnscriptedQueries_ReturnMatchAndEmpty_Test()
    {
        FakeDataSource source = new FakeDataSource().With("$.minAge", 18L);

        DataQueryResult scripted = await source.QueryAsync("$.minAge", TestContext.Current.CancellationToken);
        DataQueryResult unscripted = await source.QueryAsync("$.other", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(18), Assert.Single(scripted.Matches));
        Assert.True(unscripted.Succeeded);
        Assert.Empty(unscripted.Matches);
    }

    /// <summary>A multi-valued script returns one match per value, in order.</summary>
    [Fact]
    public async Task QueryAsync_ScriptedCollection_ReturnsOneMatchPerValue_Test()
    {
        FakeDataSource source = new FakeDataSource().With("$.roles[*]", ["admin", "auditor"]);

        DataQueryResult result = await source.QueryAsync("$.roles[*]", TestContext.Current.CancellationToken);

        Assert.Equal([LiteralValue.OfString("admin"), LiteralValue.OfString("auditor")], result.Matches);
    }

    /// <summary>A failing script reports its kind and message as data rather than throwing.</summary>
    [Fact]
    public async Task QueryAsync_FailingScript_ReportsTheErrorAsData_Test()
    {
        FakeDataSource source = new FakeDataSource().Failing("$.salary", "source offline");

        DataQueryResult result = await source.QueryAsync("$.salary", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(DataQueryErrorKind.SourceFailure, result.ErrorKind);
        Assert.Equal("source offline", result.ErrorMessage);
    }

    /// <summary>A throwing script throws the scripted exception.</summary>
    [Fact]
    public async Task QueryAsync_ThrowingScript_ThrowsTheScriptedException_Test()
    {
        InvalidOperationException scripted = new("boom");
        FakeDataSource source = new FakeDataSource().Throwing("$.x", scripted);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.QueryAsync("$.x", TestContext.Current.CancellationToken)
        );

        Assert.Same(scripted, thrown);
    }

    /// <summary>Every query is recorded, repeats included, so tests can count how often a source was asked.</summary>
    [Fact]
    public async Task Queries_RepeatedQuery_IsRecordedEachTime_Test()
    {
        FakeDataSource source = new();

        await source.QueryAsync("$.a", TestContext.Current.CancellationToken);
        await source.QueryAsync("$.a", TestContext.Current.CancellationToken);
        await source.QueryAsync("$.b", TestContext.Current.CancellationToken);

        Assert.Equal(["$.a", "$.a", "$.b"], source.Queries);
    }

    /// <summary>A scripted scope is returned and an unscripted scope is a clear error.</summary>
    [Fact]
    public async Task ScopeAsync_ScriptedAndUnscripted_ReturnScopeAndThrow_Test()
    {
        FakeDataSource inner = new();
        FakeDataSource source = new FakeDataSource().WithScope("$.orders[0]", inner);

        IDataSource scoped = await source.ScopeAsync("$.orders[0]", TestContext.Current.CancellationToken);

        Assert.Same(inner, scoped);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.ScopeAsync("$.orders[1]", TestContext.Current.CancellationToken)
        );
    }
}
