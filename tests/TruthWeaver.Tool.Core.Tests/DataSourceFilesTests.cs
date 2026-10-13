namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Tests.TestSupport;

public sealed class DataSourceFilesTests
{
    /// <summary>A JSON and a YAML data file with the same data answer a query the same way.</summary>
    /// <param name="fixture">The data file.</param>
    [Theory]
    [InlineData("user.json")]
    [InlineData("user.yaml")]
    public async Task Load_DataFixture_AnswersJsonPathQuery_Test(string fixture)
    {
        ToolResult<IDataSource> result = DataSourceFiles.Load(Fixtures.Path(fixture));

        Assert.True(result.Succeeded);
        DataQueryResult query = await result.Value.QueryAsync("$.name", TestContext.Current.CancellationToken);
        Assert.True(query.Succeeded);
        Assert.Equal("Ada", Assert.Single(query.Matches).AsString());
    }

    /// <summary>LoadAll registers each file under its source name.</summary>
    [Fact]
    public void LoadAll_TwoFiles_RegistersEachName_Test()
    {
        ToolResult<DataSources> result = DataSourceFiles.LoadAll([
            new("user", Fixtures.Path("user.json")),
            new("profile", Fixtures.Path("user.yaml")),
        ]);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value.Count);
        Assert.True(result.Value.TryGet("profile", out _));
    }

    /// <summary>A repeated name and an unreadable file are both reported, and no sources are returned.</summary>
    [Fact]
    public void LoadAll_DuplicateNameAndMissingFile_ReportsBoth_Test()
    {
        ToolResult<DataSources> result = DataSourceFiles.LoadAll([
            new("user", Fixtures.Path("user.json")),
            new("user", Fixtures.Path("user.yaml")),
            new("other", Fixtures.Path("absent.json")),
        ]);

        Assert.Null(result.Value);
        Assert.Equal(
            [ToolDiagnosticCodes.DuplicateDataSource, ToolDiagnosticCodes.InputUnreadable],
            result.Diagnostics.Select(d => d.Code)
        );
    }

    /// <summary>A malformed data document is a MalformedDocument error.</summary>
    /// <param name="text">The document.</param>
    /// <param name="format">Its format.</param>
    [Theory]
    [InlineData("{", RuleFormat.Json)]
    [InlineData("a: [", RuleFormat.Yaml)]
    public void Parse_MalformedDocument_ReportsMalformedDocument_Test(string text, RuleFormat format)
    {
        ToolResult<IDataSource> result = DataSourceFiles.Parse(text, format);

        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, Assert.Single(result.Diagnostics).Code);
    }
}
