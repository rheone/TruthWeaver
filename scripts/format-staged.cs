#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs
#:include shared/ConsoleProcess.cs

// Pre-commit step: formats the staged C# files and re-stages them, without sweeping in unstaged edits.
//
// A file whose changes are all staged is formatted (CSharpier, then dotnet format, then CSharpier again) and
// re-staged, so a commit never fails on formatting. A file that is only partly staged (it also has unstaged edits)
// cannot be re-staged without also staging those edits, so it is only checked: the run fails with a message and the
// developer formats it, then stages the hunks they want. Called by .husky/task-runner.json.
//
// Usage: dotnet run scripts/format-staged.cs
using TruthWeaver.Scripts;

try
{
    string root = await Repo.FindRootAsync();
    Directory.SetCurrentDirectory(root);

    (IReadOnlyList<string> whole, IReadOnlyList<string> partial) = await Repo.SplitStagedAsync("*.cs");

    if (whole.Count > 0)
    {
        // Both formatters run, then CSharpier runs again, because dotnet format can move a token that CSharpier
        // then moves back. The two checks after that are the CI gates. If they still fail, the formatters disagree
        // about this code and no amount of formatting settles it: the code needs a different shape, not another run.
        await RunCheckedAsync("csharpier format", ["csharpier", "format", .. whole]);
        await RunCheckedAsync("dotnet format", ["format", "--no-restore", "--severity", "info", "--include", .. whole]);
        await RunCheckedAsync("csharpier format (second pass)", ["csharpier", "format", .. whole]);
        await RunCheckedAsync("csharpier check", ["csharpier", "check", .. whole]);
        await RunCheckedAsync(
            "dotnet format check (CSharpier and dotnet format disagree on these files; reshape the code, see CLAUDE.md \"Formatting\")",
            ["format", "--no-restore", "--severity", "info", "--verify-no-changes", "--include", .. whole]
        );
        await RunCheckedAsync("git add", ["add", "--", .. whole], "git");
    }

    if (partial.Count > 0)
    {
        await Console.Out.WriteLineAsync(
            $"Partly staged files are checked, not rewritten (a rewrite would stage your unstaged edits):\n  {string.Join("\n  ", partial)}"
        );
        await RunCheckedAsync("csharpier check", ["csharpier", "check", .. partial]);
        await RunCheckedAsync(
            "dotnet format check",
            ["format", "--no-restore", "--severity", "info", "--verify-no-changes", "--include", .. partial]
        );
    }

    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}

// Runs "dotnet <arguments>" (or another tool) with console output and fails the script on a non-zero exit code.
static async Task RunCheckedAsync(string description, string[] arguments, string tool = "dotnet")
{
    int exitCode = await ConsoleProcess.RunAsync(tool, arguments);
    if (exitCode != 0)
    {
        throw new ScriptFailedException($"pre-commit: {description} failed (exit code {exitCode}).");
    }
}
