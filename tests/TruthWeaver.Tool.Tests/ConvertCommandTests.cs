namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class ConvertCommandTests
{
    /// <summary>Rule text converts to JSON with the note kept on the node it is written before (the first term).</summary>
    [Fact]
    public async Task Convert_TextToJson_KeepsTheNote_Test()
    {
        CliRun run = await Cli.RunAsync(["convert", "access.rule", "--to", "json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(
            "{\"op\":\"and\",\"operands\":[{\"predicate\":\"isActive\",\"note\":\"Editors who are active.\"},{\"predicate\":\"hasRole\",\"args\":{\"role\":\"editor\"}}]}\n",
            run.Output
        );
    }

    /// <summary>YAML converts to rule text with the note as a comment; --no-notes leaves it out.</summary>
    [Fact]
    public async Task Convert_YamlToText_WritesTheNoteUnlessNoNotes_Test()
    {
        CliRun withNotes = await Cli.RunAsync(["convert", "access.yaml", "--to", "text"]);
        CliRun withoutNotes = await Cli.RunAsync(["convert", "access.yaml", "--to", "text", "--no-notes"]);

        Assert.Equal("/* Editors who are active. */ (isActive AND hasRole(role: \"editor\"))\n", withNotes.Output);
        Assert.Equal("isActive AND hasRole(role: \"editor\")\n", withoutNotes.Output);
    }

    /// <summary>Standard input is read when the input is left out, and its format comes from the content.</summary>
    [Fact]
    public async Task Convert_StandardInput_DetectsTheFormatFromTheContent_Test()
    {
        CliRun run = await Cli.RunAsync(
            ["convert", "--to", "text"],
            standardInput: "{\"op\":\"not\",\"operands\":[{\"predicate\":\"isBanned\"}]}"
        );

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("NOT isBanned\n", run.Output);
    }

    /// <summary>An embedded rule is selected by --path.</summary>
    [Fact]
    public async Task Convert_EmbeddedRule_SelectsItByPath_Test()
    {
        CliRun run = await Cli.RunAsync(["convert", "policies.yaml", "--path", "rules.list[0]", "--to", "text"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("NOT isBanned\n", run.Output);
    }

    /// <summary>--output writes the file, and --check then finds it identical (exit 0) or different (exit 1).</summary>
    [Fact]
    public async Task Convert_OutputAndCheck_WritesThenComparesTheFile_Test()
    {
        using TemporaryDirectory folder = new();
        string output = folder.File("access.yaml");

        CliRun written = await Cli.RunAsync(["convert", "access.rule", "--to", "yaml", "--output", output]);
        CliRun identical = await Cli.RunAsync(["convert", "access.rule", "--to", "yaml", "--output", output, "--check"]);
        await File.AppendAllTextAsync(output, "# edited\n", TestContext.Current.CancellationToken);
        CliRun differs = await Cli.RunAsync([
            "convert",
            "access.rule",
            "--to",
            "yaml",
            "--output",
            output,
            "--check",
            "--json",
        ]);

        Assert.Equal(0, written.ExitCode);
        Assert.Empty(written.Output);
        Assert.Equal(0, identical.ExitCode);
        Assert.Equal($"{output}: identical\n", identical.Output);
        Assert.Equal(1, differs.ExitCode);
        Assert.Equal("differs", differs.Json["result"]!["check"]!["status"]!.GetValue<string>());
    }

    /// <summary>--check against a file that does not exist reports it missing, with exit code 1.</summary>
    [Fact]
    public async Task Convert_CheckWithAMissingFile_FailsWithExitCodeOne_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun run = await Cli.RunAsync([
            "convert",
            "access.rule",
            "--to",
            "json",
            "--output",
            folder.File("none.json"),
            "--check",
        ]);

        Assert.Equal(1, run.ExitCode);
        Assert.EndsWith("none.json: missing\n", run.Output, StringComparison.Ordinal);
    }

    /// <summary>--check without --output is a usage error with exit code 2.</summary>
    [Fact]
    public async Task Convert_CheckWithoutOutput_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["convert", "access.rule", "--to", "json", "--check"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("--check needs --output", run.Error, StringComparison.Ordinal);
    }

    /// <summary>The JSON envelope holds the target format and the converted text.</summary>
    [Fact]
    public async Task Convert_Json_HoldsTheFormatAndText_Test()
    {
        CliRun run = await Cli.RunAsync(["convert", "active.rule", "--to", "yaml", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("convert", run.Json["command"]!.GetValue<string>());
        Assert.Equal("yaml", run.Json["result"]!["format"]!.GetValue<string>());
        Assert.StartsWith("predicate: isActive", run.Json["result"]!["text"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>A rule that does not compile writes no output, reports the error on standard error, and exits 1.</summary>
    [Fact]
    public async Task Convert_RuleThatDoesNotCompile_ReportsTheErrorAndExitsOne_Test()
    {
        CliRun run = await Cli.RunAsync(["convert", "broken.rule", "--to", "json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Contains("TRE0001", run.Error, StringComparison.Ordinal);
    }
}
