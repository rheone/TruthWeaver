namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core;

/// <summary>The command line of <c>truthweaver-tui</c>, before any terminal work.</summary>
public sealed class TuiStartupTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Initializes a new instance of the <see cref="TuiStartupTests"/> class.</summary>
    public TuiStartupTests()
    {
        Directory.CreateDirectory(this.directory);
    }

    /// <summary>No argument starts with an empty rule text document.</summary>
    [Fact]
    public void Prepare_NoArguments_StartsWithAnEmptyDocument_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare([], this.directory);

        Assert.NotNull(result.Document);
        Assert.Equal(string.Empty, result.Document.Text);
        Assert.Equal(RuleFormat.Text, result.Document.Format);
    }

    /// <summary>A rule file opens, relative to the working directory, with the settings of the nearest truthweaver.json.</summary>
    [Fact]
    public void Prepare_RuleFileAndConfiguration_OpensTheFileWithTheSettings_Test()
    {
        File.WriteAllText(Path.Combine(this.directory, "rule.yaml"), "predicate: a\n");
        File.WriteAllText(Path.Combine(this.directory, ToolConfigurationFile.FileName), """{ "operatorStyle": "symbolic" }""");

        TuiStartupResult result = TuiStartup.Prepare(["rule.yaml"], this.directory);

        Assert.NotNull(result.Document);
        Assert.Equal(RuleFormat.Yaml, result.Document.Format);
        Assert.Equal(OperatorStyle.Symbolic, result.Settings.OperatorStyle);
    }

    /// <summary>--help prints the usage and exits with success.</summary>
    [Fact]
    public void Prepare_Help_PrintsTheUsage_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare(["--help"], this.directory);

        Assert.Null(result.Document);
        Assert.Equal(TuiStartup.Usage, result.Message);
        Assert.Equal(ToolExitCode.Success, result.ExitCode);
    }

    /// <summary>A file that cannot be read exits as unusable with the diagnostic.</summary>
    [Fact]
    public void Prepare_MissingFile_ExitsAsUnusable_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare(["missing.rule"], this.directory);

        Assert.Null(result.Document);
        Assert.Equal(ToolExitCode.Unusable, result.ExitCode);
        Assert.Contains(ToolDiagnosticCodes.InputUnreadable, result.Message, StringComparison.Ordinal);
    }

    /// <summary>An unknown option or a second file exits as unusable with the usage.</summary>
    [Fact]
    public void Prepare_UnknownOption_ExitsAsUnusable_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare(["--bogus"], this.directory);

        Assert.Equal(ToolExitCode.Unusable, result.ExitCode);
        Assert.Contains(TuiStartup.Usage, result.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }
}
