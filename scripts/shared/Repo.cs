namespace TruthWeaver.Scripts;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Repository helpers shared by the file-based scripts in <c>scripts/</c>. Pull this file in with
/// <c>#:include shared/Repo.cs</c> (a literal path, no globs, so build caching stays on). A script that uses
/// <see cref="RunAsync"/> also includes <c>shared/ProcessResult.cs</c> and <c>shared/ScriptFailedException.cs</c>.
/// </summary>
public static class Repo
{
    /// <summary>
    /// Returns the repository root (the work tree that holds the current directory). Works from a worktree
    /// and from the Husky working directory.
    /// </summary>
    /// <returns>The absolute root path.</returns>
    /// <exception cref="ScriptFailedException">Git cannot find a work tree.</exception>
    public static async Task<string> FindRootAsync()
    {
        string output = await GitAsync("rev-parse", "--show-toplevel");
        return output.Trim();
    }

    /// <summary>
    /// Runs <c>git</c> with the given arguments and returns its standard output. The arguments are passed
    /// as a list, so no shell quoting applies.
    /// </summary>
    /// <param name="arguments">The git arguments.</param>
    /// <returns>The standard output.</returns>
    /// <exception cref="ScriptFailedException">Git exits with a non-zero code.</exception>
    public static async Task<string> GitAsync(params string[] arguments)
    {
        ProcessResult result = await RunAsync("git", arguments);
        return result.StandardOutput;
    }

    /// <summary>
    /// Returns the staged files that were added, copied, modified or renamed (not deleted), as repository-relative
    /// paths. Git is run with <c>core.quotepath=off</c> and NUL separators, so paths with non-ASCII characters
    /// or spaces come back unchanged.
    /// </summary>
    /// <param name="pathspecs">Optional git pathspecs, for example <c>*.cs</c>, that limit the result.</param>
    /// <returns>The staged paths.</returns>
    /// <exception cref="ScriptFailedException">Git exits with a non-zero code.</exception>
    public static async Task<IReadOnlyList<string>> StagedFilesAsync(params string[] pathspecs)
    {
        string output = await GitAsync([
            "-c",
            "core.quotepath=off",
            "diff",
            "--cached",
            "--name-only",
            "--diff-filter=ACMR",
            "-z",
            "--",
            .. pathspecs,
        ]);
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Splits the staged files into those whose changes are all staged (safe to rewrite and re-stage) and those
    /// that also have unstaged edits (re-staging would stage those edits, so they may only be checked).
    /// </summary>
    /// <param name="pathspecs">Optional git pathspecs, for example <c>*.cs</c>, that limit the result.</param>
    /// <returns>The fully staged paths and the partly staged paths.</returns>
    /// <exception cref="ScriptFailedException">Git exits with a non-zero code.</exception>
    public static async Task<(IReadOnlyList<string> Whole, IReadOnlyList<string> Partial)> SplitStagedAsync(
        params string[] pathspecs
    )
    {
        IReadOnlyList<string> staged = await StagedFilesAsync(pathspecs);
        string output = await GitAsync(["-c", "core.quotepath=off", "diff", "--name-only", "-z", "--", .. pathspecs]);
        HashSet<string> unstaged = new(output.Split('\0', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        return (staged.Where(path => !unstaged.Contains(path)).ToList(), staged.Where(unstaged.Contains).ToList());
    }

    /// <summary>
    /// Runs a process and fails when the exit code is not zero. Nothing is forwarded to the console.
    /// </summary>
    /// <param name="fileName">The executable, resolved through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed without shell quoting.</param>
    /// <returns>The captured standard output and standard error.</returns>
    /// <exception cref="ScriptFailedException">The process exits with a non-zero code or cannot start.</exception>
    public static Task<ProcessResult> RunAsync(string fileName, params string[] arguments)
    {
        return RunInAsync(null, fileName, arguments);
    }

    /// <summary>
    /// Runs a process in a working directory and fails when the exit code is not zero.
    /// </summary>
    /// <param name="workingDirectory">The working directory, or <see langword="null"/> for the current one.</param>
    /// <param name="fileName">The executable, resolved through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed without shell quoting.</param>
    /// <returns>The captured standard output and standard error.</returns>
    /// <exception cref="ScriptFailedException">The process exits with a non-zero code or cannot start.</exception>
    public static async Task<ProcessResult> RunInAsync(string? workingDirectory, string fileName, params string[] arguments)
    {
        ProcessStartInfo info = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            // Git and the .NET tools write UTF-8; the default would be the console code page and garble non-ASCII paths.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
        };
        if (workingDirectory is not null)
        {
            info.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        string display = $"{fileName} {string.Join(' ', arguments)}";
        string outputText;
        string errorText;
        int exitCode;
        try
        {
            using Process process = Process.Start(info) ?? throw new ScriptFailedException($"Cannot start: {display}", 1);

            // Drain both pipes at once so a full standard-error pipe cannot block the process.
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            outputText = await output;
            errorText = await error;
            exitCode = process.ExitCode;
        }
        catch (Win32Exception exception)
        {
            // The executable is not installed or not on PATH.
            throw new ScriptFailedException($"Cannot start {fileName}: {exception.Message}", exception);
        }

        if (exitCode != 0)
        {
            throw new ScriptFailedException(
                $"{display} failed with exit code {exitCode}.{Environment.NewLine}{errorText}".TrimEnd(),
                exitCode
            );
        }

        return new ProcessResult(outputText, errorText);
    }
}
