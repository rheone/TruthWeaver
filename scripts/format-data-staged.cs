#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs
#:include shared/ConsoleProcess.cs

// Pre-commit step: formats the staged JSON, YAML and YML files and re-stages them.
//
// A file whose changes are all staged is rewritten by scripts/format-data.cs and re-staged. A file that is only partly
// staged cannot be re-staged without also staging its unstaged edits, so it is only checked: the run fails with a
// message and the developer runs scripts/format-data.cs, then stages the hunks they want.
// Called by .husky/task-runner.json.
//
// Usage: dotnet run scripts/format-data-staged.cs
using TruthWeaver.Scripts;

try
{
    string root = await Repo.FindRootAsync();
    Directory.SetCurrentDirectory(root);

    (IReadOnlyList<string> whole, IReadOnlyList<string> partial) = await Repo.SplitStagedAsync("*.json", "*.yaml", "*.yml");

    if (whole.Count > 0)
    {
        if (await ConsoleProcess.RunAsync("dotnet", ["run", "scripts/format-data.cs", "--", .. whole]) != 0)
        {
            return 1;
        }

        // Re-staging an unchanged file is a no-op, so every whole file is added.
        if (await ConsoleProcess.RunAsync("git", ["add", "--", .. whole]) != 0)
        {
            return 1;
        }
    }

    if (
        partial.Count > 0
        && await ConsoleProcess.RunAsync("dotnet", ["run", "scripts/format-data.cs", "--", "--check-only", .. partial]) != 0
    )
    {
        await Console.Error.WriteLineAsync(
            "pre-commit: partly staged JSON/YAML files need formatting; run scripts/format-data.cs, then stage the hunks you want."
        );
        return 1;
    }

    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
