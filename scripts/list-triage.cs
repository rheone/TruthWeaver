#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs

// Lists the triage state of every ticket, spec, plan and map under .scratch/.
//
// Prints one tab-separated row per Status line: the path, the triage label, the "Blocked by:" value and the prose
// that trails the label (an owner hold such as "do not start until the owner releases the sub-tickets" lives there).
// An optional first argument filters on a label, for example "needs-triage", or is "--open" to leave out the resolved
// states done and wontfix. The folders engine-v1 and _superseded are skipped, as in scripts/validate-triage-labels.cs,
// which checks that each label is valid.
//
// Usage: dotnet run scripts/list-triage.cs [-- <label> | --open]
using System.Text.RegularExpressions;
using TruthWeaver.Scripts;

try
{
    string root = await Repo.FindRootAsync();
    string scratch = Path.Combine(root, ".scratch");
    bool openOnly = args.Length > 0 && args[0] == "--open";
    string? filter = args.Length > 0 && !openOnly ? args[0] : null;

    // The patterns run on local files only, but a timeout keeps a pathological line from hanging the script.
    TimeSpan timeout = TimeSpan.FromSeconds(5);
    Regex skippedFolder = new(@"[\\/](engine-v1|_superseded)[\\/]", RegexOptions.Compiled, timeout);
    Regex statusLine = new(@"^\s*(?:\*\*Status:\*\*|Status:)\s*([A-Za-z-]+)\s*(.*?)\s*$", RegexOptions.Compiled, timeout);
    Regex blockedLine = new(@"^\s*(?:\*\*Blocked by:\*\*|Blocked by:)\s*(.+?)\s*$", RegexOptions.Compiled, timeout);

    IEnumerable<string> files = Directory
        .EnumerateFiles(scratch, "*.md", SearchOption.AllDirectories)
        .Where(path => !skippedFolder.IsMatch(path))
        .Order(StringComparer.OrdinalIgnoreCase);

    foreach (string file in files)
    {
        string? label = null;
        string hold = string.Empty;
        string blockedBy = string.Empty;

        // Only the header of a ticket carries these lines, so the first 20 lines are enough and a quoted example
        // further down never counts.
        foreach (string line in File.ReadLines(file).Take(20))
        {
            if (label is null && statusLine.Match(line) is { Success: true } status)
            {
                label = status.Groups[1].Value;
                hold = status.Groups[2].Value;
            }
            else if (blockedBy.Length == 0 && blockedLine.Match(line) is { Success: true } blocked)
            {
                blockedBy = blocked.Groups[1].Value;
            }
        }

        if (
            label is null
            || (filter is not null && !label.Equals(filter, StringComparison.OrdinalIgnoreCase))
            || (openOnly && label.ToLowerInvariant() is "done" or "wontfix")
        )
        {
            continue;
        }

        await Console.Out.WriteLineAsync(
            string.Join('\t', Path.GetRelativePath(root, file), label.ToLowerInvariant(), blockedBy, hold)
        );
    }

    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
