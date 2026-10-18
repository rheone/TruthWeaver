namespace TruthWeaver.Scripts;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Runs a child process that shares the console of the script, for tools whose output the user should see as it
/// happens and whose exit code the script must interpret itself. Pull this file in with
/// <c>#:include shared/ConsoleProcess.cs</c>, together with <c>shared/ScriptFailedException.cs</c>.
/// </summary>
public static class ConsoleProcess
{
    /// <summary>
    /// Runs a process with its standard output and standard error attached to the script console and returns the
    /// exit code without treating a non-zero code as a failure. On Windows, <c>npx</c> is a <c>.cmd</c> file, so
    /// it is resolved to the full path of <c>npx.cmd</c> through <c>PATH</c>.
    /// </summary>
    /// <param name="fileName">The executable, resolved through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed without shell quoting.</param>
    /// <returns>The exit code of the process.</returns>
    /// <exception cref="ScriptFailedException">The process cannot start, for example when it is not installed.</exception>
    public static async Task<int> RunAsync(string fileName, params string[] arguments)
    {
        ProcessStartInfo info = new(ResolveLauncher(fileName)) { UseShellExecute = false };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        try
        {
            using Process process = Process.Start(info) ?? throw new ScriptFailedException($"Cannot start: {fileName}", 1);
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (Win32Exception exception)
        {
            throw new ScriptFailedException($"Cannot start {fileName}: {exception.Message}", exception);
        }
    }

    // A .cmd launcher started by bare name gets the current directory as %~dp0 and cannot find its own files, so
    // the name is resolved to the full path through PATH. Other platforms and other tools keep the plain name.
    private static string ResolveLauncher(string fileName)
    {
        if (!OperatingSystem.IsWindows() || fileName != "npx")
        {
            return fileName;
        }

        string[] directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);
        string? found = directories
            .Where(directory => directory.Length > 0)
            .Select(directory => Path.Combine(directory, "npx.cmd"))
            .FirstOrDefault(File.Exists);
        return found ?? "npx.cmd";
    }
}
