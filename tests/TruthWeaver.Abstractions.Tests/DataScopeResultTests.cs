namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

/// <summary>The result of <see cref="IDataSource.ScopeAsync"/>: a narrowed source or an error described as data.</summary>
public sealed class DataScopeResultTests
{
    /// <summary>A successful result carries the scoped source and no error.</summary>
    [Fact]
    public void Success_CarriesTheSourceAndNoError_Test()
    {
        StubSource source = new();

        DataScopeResult result = DataScopeResult.Success(source);

        Assert.True(result.Succeeded);
        Assert.Same(source, result.Source);
        Assert.Null(result.ErrorKind);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>A failed result carries the kind and message and no source.</summary>
    [Fact]
    public void Failure_CarriesTheKindAndMessageAndNoSource_Test()
    {
        DataScopeResult result = DataScopeResult.Failure(DataQueryErrorKind.NoMatch, "nothing matched");

        Assert.False(result.Succeeded);
        Assert.Null(result.Source);
        Assert.Equal(DataQueryErrorKind.NoMatch, result.ErrorKind);
        Assert.Equal("nothing matched", result.ErrorMessage);
    }

    /// <summary>A null source is a programming error and throws.</summary>
    [Fact]
    public void Success_NullSource_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => DataScopeResult.Success(null!));
    }

    private sealed class StubSource : IDataSource
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
