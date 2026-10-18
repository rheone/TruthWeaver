namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Tool.Core;

/// <summary>What <see cref="TuiStartup.Prepare"/> decided: run the workbench, or print a text and exit.</summary>
/// <param name="Document">The first document, or <see langword="null"/> when the process exits without a workbench.</param>
/// <param name="Settings">The resolved settings.</param>
/// <param name="Message">The text to print when the process exits, or <see langword="null"/>.</param>
/// <param name="ExitCode">The exit code when the process exits.</param>
internal sealed record TuiStartupResult(RuleDocument? Document, ToolSettings Settings, string? Message, ToolExitCode ExitCode)
{
    /// <summary>Creates a result that runs the workbench.</summary>
    /// <param name="document">The first document.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The result.</returns>
    public static TuiStartupResult Run(RuleDocument document, ToolSettings settings)
    {
        return new TuiStartupResult(document, settings, null, ToolExitCode.Success);
    }

    /// <summary>Creates a result that prints a text and exits.</summary>
    /// <param name="message">The text.</param>
    /// <param name="exitCode">The exit code.</param>
    /// <returns>The result.</returns>
    public static TuiStartupResult Exit(string message, ToolExitCode exitCode)
    {
        return new TuiStartupResult(null, ToolSettings.Default, message, exitCode);
    }
}
