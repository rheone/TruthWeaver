namespace TruthWeaver.Tool.Tests;

using System.Runtime.Versioning;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tests.TestSupport;

public sealed class ExportCommandTests
{
    /// <summary>One artifact is written to the --output file.</summary>
    [Fact]
    public async Task Export_OneArtifact_WritesTheFile_Test()
    {
        using TemporaryDirectory folder = new();
        string output = folder.File("rule.mmd");

        CliRun run = await Cli.RunAsync(["export", "access.rule", "--artifact", "mermaid", "--output", output]);

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith(
            "flowchart",
            await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken),
            StringComparison.Ordinal
        );
    }

    /// <summary>--all writes every artifact and the manifest; a second run with --check finds every file identical.</summary>
    [Fact]
    public async Task Export_AllThenCheck_WritesTheFolderAndFindsItIdentical_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun exported = await Cli.RunAsync(["export", "access.rule", "--all", folder.FullPath, "--json"]);
        CliRun checkedRun = await Cli.RunAsync(["export", "access.rule", "--all", folder.FullPath, "--check"]);

        Assert.Equal(0, exported.ExitCode);
        Assert.True(File.Exists(folder.File(ExportManifest.FileName)));
        Assert.NotEmpty(exported.Json["result"]!["manifest"]!["artifacts"]!.AsArray());
        Assert.Equal(0, checkedRun.ExitCode);
        Assert.DoesNotContain("differs", checkedRun.Output, StringComparison.Ordinal);
    }

    /// <summary>--check on a folder with a changed file fails with exit code 1.</summary>
    [Fact]
    public async Task Export_CheckOfAChangedFolder_FailsWithExitCodeOne_Test()
    {
        using TemporaryDirectory folder = new();
        await Cli.RunAsync(["export", "access.rule", "--all", folder.FullPath]);
        await File.WriteAllTextAsync(folder.File("rule-text.rule"), "isActive\n", TestContext.Current.CancellationToken);

        CliRun run = await Cli.RunAsync(["export", "access.rule", "--all", folder.FullPath, "--check"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("rule-text.rule: differs at line 1", run.Output, StringComparison.Ordinal);
    }

    /// <summary>--render with no renderer on the search path is a TWT0016 error with exit code 2.</summary>
    [Fact]
    public async Task Export_RenderWithoutTheRenderer_ExitsTwo_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun run = await Cli.RunAsync(
            ["export", "access.rule", "-a", "dot", "-o", folder.File("rule.svg"), "--render", "svg", "--json"],
            rendererSearchPath: folder.FullPath
        );

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.RendererNotFound], run.DiagnosticCodes);
    }

    /// <summary>--render runs the renderer that is on the search path, with the diagram and the output file.</summary>
    [Fact]
    public async Task Export_RenderWithAFakeRenderer_WritesTheImage_Test()
    {
        using TemporaryDirectory tools = new();
        WriteFakeDot(tools);
        string output = tools.File("out/rule.svg");

        CliRun run = await Cli.RunAsync(
            ["export", "access.rule", "-a", "dot", "-o", output, "--render", "svg"],
            rendererSearchPath: tools.FullPath
        );

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith(
            "fake dot -Tsvg",
            await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken),
            StringComparison.Ordinal
        );
    }

    /// <summary>Neither --output nor --all, or --render with an artifact that is not a diagram, is a usage error with exit code 2.</summary>
    [Theory]
    [InlineData("export", "access.rule", "--artifact", "json")]
    [InlineData("export", "access.rule", "--artifact", "json", "-o", "x.json", "--render", "svg")]
    public async Task Export_InvalidOptions_IsAUsageError_Test(params string[] args)
    {
        CliRun run = await Cli.RunAsync(args);

        Assert.Equal(2, run.ExitCode);
        Assert.NotEmpty(run.Error);
    }

    // Writes a dot that writes "fake dot <arguments>" to its third argument (the output file).
    private static void WriteFakeDot(TemporaryDirectory directory)
    {
        if (OperatingSystem.IsWindows())
        {
            File.WriteAllText(directory.File("dot.cmd"), "@echo off\r\necho fake dot %*> \"%~3\"\r\n");
        }
        else
        {
            WriteScript(directory.File("dot"), "#!/bin/sh\necho \"fake dot $*\" > \"$3\"\n");
        }
    }

    [UnsupportedOSPlatform("windows")]
    private static void WriteScript(string path, string text)
    {
        File.WriteAllText(path, text);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}
