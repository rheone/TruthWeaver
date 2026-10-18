namespace TruthWeaver.Testing.Tests.RuleTests;

using TruthWeaver.Abstractions;
using TruthWeaver.Testing.RuleTests;

/// <summary>The reading of a rule test file: every problem is reported with its path, and data files load by format.</summary>
public sealed class RuleTestFileReaderTests : IDisposable
{
    private const string Case = "{ name: c, expect: { result: True } }";

    private readonly string directory = Path.Combine(Path.GetTempPath(), "tw-rtf-" + Guid.NewGuid().ToString("N"));

    public RuleTestFileReaderTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>Disposes the temporary data directory.</summary>
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    /// <summary>A file that is not well-formed YAML gives one error at the root, with the place and no document text.</summary>
    [Fact]
    public void TryParse_MalformedYaml_ReportsOneRootErrorWithoutQuotingTheDocument_Test()
    {
        bool parsed = RuleTestFile.TryParse(
            "rule: [secret-token\n",
            this.directory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        RuleTestFileError error = Assert.Single(errors);
        Assert.False(parsed);
        Assert.Equal("$", error.Path);
        Assert.Equal("The file is not well-formed YAML.", error.Message);
        Assert.DoesNotContain("secret-token", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A document that is empty or is not a mapping is not a rule test file.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("- a\n- b\n")]
    public void TryParse_RootIsNotAMapping_ReportsAnError_Test(string yaml)
    {
        bool parsed = RuleTestFile.TryParse(yaml, this.directory, out _, out IReadOnlyList<RuleTestFileError> errors);

        Assert.False(parsed);
        Assert.Equal("$", Assert.Single(errors).Path);
    }

    /// <summary>Each shape that the file cannot use is reported at the path of the part, and the file does not parse.</summary>
    [Theory]
    [InlineData("rule: [a]\ncases: [" + Case + "]", "$.rule")]
    [InlineData("ruleFile: ''\ncases: [" + Case + "]", "$.ruleFile")]
    [InlineData("rule: a\ncases: [x]", "$.cases[0]")]
    [InlineData("rule: a\ncases: [{ expect: { result: True } }]", "$.cases[0]")]
    [InlineData("rule: a\ncases: [{ name: '', expect: { result: True } }]", "$.cases[0].name")]
    [InlineData("rule: a\ncases: [{ name: c }]", "$.cases[0]")]
    [InlineData("rule: a\ncases: [{ name: c, given: x, expect: { result: True } }]", "$.cases[0].given")]
    [InlineData(
        "rule: a\ncases: [{ name: c, given: { 'hasRole(role:\"a\")': True, 'hasRole(role: \"a\")': False }, expect: { result: True } }]",
        "$.cases[0].given['hasRole(role: \"a\")']"
    )]
    [InlineData("rule: a\ncases: [{ name: c, variables: x, expect: { result: True } }]", "$.cases[0].variables")]
    [InlineData("rule: a\ncases: [{ name: c, variableFiles: x, expect: { result: True } }]", "$.cases[0].variableFiles")]
    [InlineData(
        "rule: a\ncases: [{ name: c, variables: { user: { a: 1 } }, variableFiles: { user: user.json }, expect: { result: True } }]",
        "$.cases[0].variableFiles['user']"
    )]
    [InlineData(
        "rule: a\ncases: [{ name: c, variableFiles: { user: '' }, expect: { result: True } }]",
        "$.cases[0].variableFiles['user']"
    )]
    [InlineData("rule: a\ncases: [{ name: c, expect: x }]", "$.cases[0].expect")]
    [InlineData("rule: a\ncases: [{ name: c, expect: { faults: 1 } }]", "$.cases[0].expect")]
    [InlineData("rule: a\ncases: [{ name: c, expect: { result: True, trace: '' } }]", "$.cases[0].expect.trace")]
    public async Task TryParse_UnusableShape_ReportsTheErrorAtItsPath_Test(string yaml, string path)
    {
        await File.WriteAllTextAsync(
            Path.Combine(this.directory, "user.json"),
            """{ "region": "EU" }""",
            TestContext.Current.CancellationToken
        );

        bool parsed = RuleTestFile.TryParse(
            yaml,
            this.directory,
            out RuleTestFile? file,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.False(parsed);
        Assert.Null(file);
        Assert.Contains(errors, e => e.Path == path);
    }

    /// <summary>A YAML data file is read as a data source and answers queries of the case.</summary>
    [Fact]
    public async Task TryParse_YamlVariableFile_LoadsAsADataSource_Test()
    {
        await File.WriteAllTextAsync(
            Path.Combine(this.directory, "user.yaml"),
            "region: EU\n",
            TestContext.Current.CancellationToken
        );

        bool parsed = RuleTestFile.TryParse(
            "rule: 'inRegion(region: from(\"user\", \"$.region\"))'\n"
                + "cases:\n  - name: eu\n    given: { 'inRegion(region: \"EU\")': True }\n"
                + "    variableFiles: { user: user.yaml }\n    expect: { result: True, faults: 0 }\n",
            this.directory,
            out RuleTestFile? file,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.True(parsed, string.Join("\n", errors));
        Assert.NotNull(file);
        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
    }

    /// <summary>A data file that does not parse is a problem of the test file, and the message names the file but not its content.</summary>
    [Theory]
    [InlineData("bad.json", "{ not json secret-token")]
    [InlineData("bad.yaml", "key: [secret-token")]
    public async Task TryParse_UnusableVariableFile_ReportsTheFileWithoutItsContent_Test(string name, string content)
    {
        await File.WriteAllTextAsync(Path.Combine(this.directory, name), content, TestContext.Current.CancellationToken);

        bool parsed = RuleTestFile.TryParse(
            $"rule: a\ncases: [{{ name: c, variableFiles: {{ user: {name} }}, expect: {{ result: True }} }}]",
            this.directory,
            out _,
            out IReadOnlyList<RuleTestFileError> errors
        );

        RuleTestFileError error = Assert.Single(errors);
        Assert.False(parsed);
        Assert.Equal("$.cases[0].variableFiles['user']", error.Path);
        Assert.Contains($"The data file '{name}' is not usable", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The expected result and the given values accept the short letters in any case.</summary>
    [Theory]
    [InlineData("t", TruthValue.True)]
    [InlineData("F", TruthValue.False)]
    [InlineData("u", TruthValue.Unknown)]
    public async Task RunAsync_ShortTruthValueLetters_AreAccepted_Test(string letter, TruthValue expected)
    {
        bool parsed = RuleTestFile.TryParse(
            $"rule: isActive\ncases: [{{ name: c, given: {{ isActive: {letter} }}, expect: {{ result: {letter} }} }}]",
            this.directory,
            out RuleTestFile? file,
            out IReadOnlyList<RuleTestFileError> errors
        );

        Assert.True(parsed, string.Join("\n", errors));
        Assert.NotNull(file);
        RuleTestReport report = await file.RunAsync(cancellationToken: TestContext.Current.CancellationToken);
        report.ShouldPass();
        Assert.Equal(expected, Assert.Single(report.Results).Decision!.Result);
    }
}
