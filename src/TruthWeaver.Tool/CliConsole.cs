namespace TruthWeaver.Tool;

/// <summary>
/// The streams and the working directory of one run of the command line. The program uses the process console; the tests
/// give their own writers, input and directory, so every command runs in process.
/// </summary>
/// <param name="Out">Standard output: the result of a command.</param>
/// <param name="Error">Standard error: the diagnostics in text mode, and the usage errors.</param>
/// <param name="OpenInput">Opens standard input, for the input <c>-</c>. It is called only when a command reads standard input.</param>
/// <param name="WorkingDirectory">The directory that relative paths are resolved against.</param>
/// <param name="RendererSearchPath">
/// The directories to find the image renderers in, as a <c>PATH</c> value, or <see langword="null"/> for the <c>PATH</c> of
/// the process. The tests put fake renderers here.
/// </param>
internal sealed record CliConsole(
    TextWriter Out,
    TextWriter Error,
    Func<Stream> OpenInput,
    string WorkingDirectory,
    string? RendererSearchPath = null
)
{
    /// <summary>Gets a console over the process streams and the current directory.</summary>
    /// <returns>The console.</returns>
    public static CliConsole ForProcess()
    {
        return new CliConsole(Console.Out, Console.Error, Console.OpenStandardInput, Environment.CurrentDirectory);
    }

    /// <summary>Resolves a path that the user gave against <see cref="WorkingDirectory"/>.</summary>
    /// <param name="path">The path as given.</param>
    /// <returns>The full path.</returns>
    public string FullPath(string path)
    {
        return Path.GetFullPath(path, this.WorkingDirectory);
    }
}
