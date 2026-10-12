namespace TruthWeaver.Tool.Tests.TestSupport;

/// <summary>Runs the command line in process, with captured output, a given standard input and the samples as the working directory.</summary>
internal static class Cli
{
    /// <summary>Runs one command line.</summary>
    /// <param name="args">The arguments.</param>
    /// <param name="standardInput">The text of standard input, or <see langword="null"/> for none.</param>
    /// <param name="workingDirectory">The working directory, or <see langword="null"/> for the samples directory.</param>
    /// <param name="rendererSearchPath">The directories with the image renderers, or <see langword="null"/> for an empty search path.</param>
    /// <returns>The exit code and the two output streams.</returns>
    public static async Task<CliRun> RunAsync(
        string[] args,
        string? standardInput = null,
        string? workingDirectory = null,
        string? rendererSearchPath = null
    )
    {
        await using StringWriter output = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
        await using StringWriter error = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
        CliConsole console = new(
            output,
            error,
            () => new MemoryStream(Encoding.UTF8.GetBytes(standardInput ?? string.Empty)),
            workingDirectory ?? Samples.Directory,
            rendererSearchPath ?? Path.Combine(Path.GetTempPath(), "truthweaver-no-renderers")
        );
        int exitCode = await TruthWeaverCli.RunAsync(args, console, TestContext.Current.CancellationToken);
        return new CliRun(exitCode, output.ToString(), error.ToString());
    }
}
