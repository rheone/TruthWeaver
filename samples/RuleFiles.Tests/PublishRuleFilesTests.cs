namespace TruthWeaver.Samples.RuleFiles.Tests;

using TruthWeaver.Testing.RuleTests;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Validation;

public sealed class PublishRuleFilesTests
{
    private static readonly string RuleFile = Path.Combine(AppContext.BaseDirectory, "RuleFiles", "publish.rule");

    private static readonly string TestFile = Path.Combine(AppContext.BaseDirectory, "RuleFiles", "publish.test.yaml");

    /// <summary>
    /// The rule file loads and passes validation on every level. This is what <c>truthweaver validate publish.rule</c>
    /// does, and it ends with exit code 0.
    /// </summary>
    [Fact]
    public void Validate_PublishRuleFile_HasExitCodeSuccess_Test()
    {
        ToolResult<LoadedRule> loaded = RuleLoader.LoadFile(RuleFile);
        Assert.True(loaded.Succeeded);

        ToolResult<ValidationReport> validated = RuleValidator.Validate(loaded.Value);

        Assert.True(validated.Succeeded);
        Assert.Equal(ToolExitCode.Success, validated.Value.ExitCode());
    }

    /// <summary>
    /// Every case of the rule test file passes. This is what <c>truthweaver test publish.test.yaml</c> does.
    /// </summary>
    [Fact]
    public async Task RunAsync_PublishTestFile_PassesEveryCase_Test()
    {
        RuleTestReport report = await RuleTestFile.RunAsync(TestFile, cancellationToken: TestContext.Current.CancellationToken);

        report.ShouldPass();
        Assert.Equal(4, report.Results.Count);
    }
}
