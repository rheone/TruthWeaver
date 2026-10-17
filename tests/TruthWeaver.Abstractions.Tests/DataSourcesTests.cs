namespace TruthWeaver.Abstractions.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;

public sealed class DataSourcesTests
{
    /// <summary>A source added under a name is found by TryGet and counted.</summary>
    [Fact]
    public void TryGet_AfterAdd_ReturnsTheRegisteredSource_Test()
    {
        IDataSource source = Substitute.For<IDataSource>();
        DataSources sources = new() { { "users", source } };

        bool found = sources.TryGet("users", out IDataSource? result);

        Assert.True(found);
        Assert.Same(source, result);
        Assert.Equal(1, sources.Count);
    }

    /// <summary>Names are case-sensitive, so a different casing is not found.</summary>
    [Fact]
    public void TryGet_DifferentCasing_ReturnsFalse_Test()
    {
        DataSources sources = new() { { "users", Substitute.For<IDataSource>() } };

        bool found = sources.TryGet("Users", out IDataSource? result);

        Assert.False(found);
        Assert.Null(result);
    }

    /// <summary>Adding a second source under an existing name is rejected.</summary>
    [Fact]
    public void Add_DuplicateName_ThrowsArgumentException_Test()
    {
        DataSources sources = new() { { "users", Substitute.For<IDataSource>() } };

        Assert.Throws<ArgumentException>(() => sources.Add("users", Substitute.For<IDataSource>()));
    }

    /// <summary>The indexer replaces an existing source and returns it on read.</summary>
    [Fact]
    public void Indexer_SetOverExistingName_ReplacesTheSource_Test()
    {
        IDataSource replacement = Substitute.For<IDataSource>();
        DataSources sources = new() { { "users", Substitute.For<IDataSource>() } };

        sources["users"] = replacement;

        Assert.Same(replacement, sources["users"]);
        Assert.Equal(1, sources.Count);
    }

    /// <summary>Reading an unsupplied name through the indexer throws KeyNotFoundException.</summary>
    [Fact]
    public void Indexer_UnknownName_ThrowsKeyNotFoundException_Test()
    {
        DataSources sources = [];

        Assert.Throws<KeyNotFoundException>(() => sources["missing"]);
    }

    /// <summary>Enumerating yields every name and source pair, through both the generic and non-generic enumerators.</summary>
    [Fact]
    public void GetEnumerator_Enumeration_YieldsEveryRegisteredPair_Test()
    {
        DataSources sources = new() { { "a", Substitute.For<IDataSource>() }, { "b", Substitute.For<IDataSource>() } };

        List<string> names = [.. sources.Select(pair => pair.Key)];
        int untypedCount = ((System.Collections.IEnumerable)sources).Cast<object>().Count();

        Assert.Equal(["a", "b"], names.Order(StringComparer.Ordinal));
        Assert.Equal(2, untypedCount);
    }
}
