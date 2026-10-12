namespace TruthWeaver.Tool;

/// <summary>The entry point of the <c>truthweaver</c> command.</summary>
internal static class Program
{
    /// <summary>Runs the command line over the process console.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code: 0 success, 1 failed, 2 unusable (see <c>docs/cli.md</c>).</returns>
    public static Task<int> Main(string[] args)
    {
        // The output is UTF-8 with no byte order mark on every platform, whatever the console code page is.
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return TruthWeaverCli.RunAsync(args, CliConsole.ForProcess());
    }
}
