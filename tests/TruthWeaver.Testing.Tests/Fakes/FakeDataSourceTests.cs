namespace TruthWeaver.Testing.Tests.Fakes;

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

    /// <summary>Each single-value overload scripts one match of its own kind.</summary>
    [Fact]
    public async Task QueryAsync_TypedSingleValueScripts_ReturnOneMatchOfEachKind_Test()
    {
        DateTimeOffset moment = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        FakeDataSource source = new FakeDataSource()
            .With("$.price", 12.5m)
            .With("$.flag", true)
            .With("$.when", moment)
            .With("$.id", id);

        DataQueryResult price = await source.QueryAsync("$.price", TestContext.Current.CancellationToken);
        DataQueryResult flag = await source.QueryAsync("$.flag", TestContext.Current.CancellationToken);
        DataQueryResult when = await source.QueryAsync("$.when", TestContext.Current.CancellationToken);
        DataQueryResult identifier = await source.QueryAsync("$.id", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfDecimal(12.5m), Assert.Single(price.Matches));
        Assert.Equal(LiteralValue.OfBoolean(true), Assert.Single(flag.Matches));
        Assert.Equal(LiteralValue.OfDateTimeOffset(moment), Assert.Single(when.Matches));
        Assert.Equal(LiteralValue.OfGuid(id), Assert.Single(identifier.Matches));
    }

    /// <summary>A multi-valued integer script returns one match per value, in order.</summary>
    [Fact]
    public async Task QueryAsync_ScriptedIntegerCollection_ReturnsOneMatchPerValue_Test()
    {
        FakeDataSource source = new FakeDataSource().With("$.levels[*]", [1, 5]);

        DataQueryResult result = await source.QueryAsync("$.levels[*]", TestContext.Current.CancellationToken);

        Assert.Equal([LiteralValue.OfInt64(1), LiteralValue.OfInt64(5)], result.Matches);
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

    /// <summary>A scripted scope is returned as a successful result.</summary>
    [Fact]
    public async Task ScopeAsync_Scripted_ReturnsTheScope_Test()
    {
        FakeDataSource inner = new();
        FakeDataSource source = new FakeDataSource().WithScope("$.orders[0]", inner);

        DataScopeResult result = await source.ScopeAsync("$.orders[0]", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Same(inner, result.Source);
    }

    /// <summary>An unscripted scope is a no-match failure result and never throws.</summary>
    [Fact]
    public async Task ScopeAsync_Unscripted_ReturnsNoMatchFailure_Test()
    {
        FakeDataSource source = new();

        DataScopeResult result = await source.ScopeAsync("$.orders[1]", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(DataQueryErrorKind.NoMatch, result.ErrorKind);
    }

    /// <summary>A scripted scope failure returns the scripted kind and message, including an unsupported type.</summary>
    [Theory]
    [InlineData(DataQueryErrorKind.AmbiguousMatch)]
    [InlineData(DataQueryErrorKind.MalformedQuery)]
    [InlineData(DataQueryErrorKind.UnsupportedType)]
    public async Task ScopeAsync_ScriptedFailure_ReturnsTheKindAndMessage_Test(DataQueryErrorKind kind)
    {
        FakeDataSource source = new FakeDataSource().FailingScope("$.orders[*]", "scripted", kind);

        DataScopeResult result = await source.ScopeAsync("$.orders[*]", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(kind, result.ErrorKind);
        Assert.Equal("scripted", result.ErrorMessage);
    }
}
