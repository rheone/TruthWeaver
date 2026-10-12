namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tests.TestSupport;

public sealed class PrintCommandTests
{
    /// <summary>With no artifact, print writes the rule text.</summary>
    [Fact]
    public async Task Print_NoArtifact_WritesTheRuleText_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "access.yaml"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("/* Editors who are active. */ (isActive AND hasRole(role: \"editor\"))\n", run.Output);
    }

    /// <summary>The 'equation' artifact uses the dialect of the command line.</summary>
    [Fact]
    public async Task Print_EquationWithDialect_WritesTheEquationInThatDialect_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "access.rule", "--artifact", "equation", "--dialect", "latex", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("equation-latex-letters", run.Json["result"]!["artifact"]!.GetValue<string>());
        Assert.Equal(
            "\\text{isActive} \\land \\text{hasRole(role: \"editor\")}\n",
            run.Json["result"]!["text"]!.GetValue<string>()
        );
    }

    /// <summary>Any catalog id prints, such as the Markdown truth table.</summary>
    [Fact]
    public async Task Print_TruthTableArtifact_WritesTheTable_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "active.rule", "-a", "truth-table-markdown"]);

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith("|", run.Output, StringComparison.Ordinal);
    }

    /// <summary>An id that is not in the catalog is a TWT0018 error with exit code 2.</summary>
    [Fact]
    public async Task Print_UnknownArtifact_ExitsTwo_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "access.rule", "-a", "no-such-artifact", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.UnknownArtifact], run.DiagnosticCodes);
    }
}
