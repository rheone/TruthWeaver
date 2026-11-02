#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs

// Validates the triage-label Status line(s) of the issue tracker under .scratch/.
//
// Checks two kinds of file: an issue ticket (.scratch/<effort>/issues/*.md) and the top-level spec/plan/map file
// of an effort (.scratch/<effort>/spec.md, plan.md or map.md). Every "Status:" or "**Status:**" line outside a
// fenced code block must lead with one of the five triage labels in docs/agents/triage-labels.md
// (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix) or "done", the resolved state that
// scripts/list-open-done-tickets.cs already recognizes, matched case-insensitively. Trailing prose after the
// label (a date, a parenthetical, a follow-on note) is not checked. A file with no Status line is not required
// to have one. The folders engine-v1 and _superseded are skipped, as in scripts/list-open-done-tickets.cs.
// A ticket under issues/ that is staged and leads with ready-for-agent must also hold a "## Agent Brief" heading, so
// an agent never gets a ticket with no brief. A staged ticket that leads with done must have every acceptance box of its
// "## Agent Brief" section ticked ("- [x]"). Tickets that are not staged are not checked, so older tickets stay as they are.
// Called by .husky/task-runner.json; also runnable standalone.
//
// Usage: dotnet run scripts/validate-triage-labels.cs
using System.Text.RegularExpressions;
using TruthWeaver.Scripts;

try
{
    string root = await Repo.FindRootAsync();
    string scratch = Path.Combine(root, ".scratch");

    HashSet<string> validLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "needs-triage",
        "needs-info",
        "ready-for-agent",
        "ready-for-human",
        "wontfix",
        "done",
    };

    // The patterns run on local files only, but a timeout keeps a pathological line from hanging the script.
    TimeSpan timeout = TimeSpan.FromSeconds(5);
    Regex skippedFolder = new(@"[\\/](engine-v1|_superseded)[\\/]", RegexOptions.Compiled, timeout);
    Regex fence = new(@"^\s*```", RegexOptions.Compiled, timeout);
    Regex statusLine = new(@"^\s*(?:\*\*Status:\*\*|Status:)\s*(.+?)\s*$", RegexOptions.Compiled, timeout);
    Regex leadingLabel = new(@"^[A-Za-z-]+", RegexOptions.Compiled, timeout);
    Regex uncheckedBox = new(@"^\s*[-*]\s\[ \]", RegexOptions.Compiled, timeout);

    bool IsTrackedTicket(string relativeToScratch)
    {
        string[] segments = relativeToScratch.Split('/');
        return segments.Length switch
        {
            3 when segments[1] == "issues" => true,
            2 when segments[1] is "spec.md" or "plan.md" or "map.md" => true,
            _ => false,
        };
    }

    IEnumerable<string> files = Directory
        .EnumerateFiles(scratch, "*.md", SearchOption.AllDirectories)
        .Where(path => !skippedFolder.IsMatch(path))
        .Where(path => IsTrackedTicket(Path.GetRelativePath(scratch, path).Replace('\\', '/')))
        .Order(StringComparer.OrdinalIgnoreCase);

    HashSet<string> staged = new(
        (await Repo.StagedFilesAsync(".scratch/**/issues/*.md")).Select(path => Path.GetFullPath(Path.Combine(root, path))),
        StringComparer.OrdinalIgnoreCase
    );

    int invalid = 0;
    foreach (string file in files)
    {
        string[] lines = await File.ReadAllLinesAsync(file);
        bool readyForAgent = false;
        bool done = false;
        bool inFence = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (fence.IsMatch(lines[i]))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            Match statusMatch = statusLine.Match(lines[i]);
            if (!statusMatch.Success)
            {
                continue;
            }

            Match labelMatch = leadingLabel.Match(statusMatch.Groups[1].Value);
            string label = labelMatch.Success ? labelMatch.Value : string.Empty;
            if (validLabels.Contains(label))
            {
                readyForAgent |= label.Equals("ready-for-agent", StringComparison.OrdinalIgnoreCase);
                done |= label.Equals("done", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            invalid++;
            string relative = Path.GetRelativePath(root, file);
            string lineNumber = (i + 1).ToString(CultureInfo.InvariantCulture);
            string shown = string.IsNullOrEmpty(label) ? "(none)" : label;
            await Console.Error.WriteLineAsync(
                $"{relative}:{lineNumber}: invalid triage label '{shown}' in \"{lines[i].Trim()}\""
            );
        }

        if (
            readyForAgent
            && staged.Contains(Path.GetFullPath(file))
            && !lines.Any(line => line.StartsWith("## Agent Brief", StringComparison.Ordinal))
        )
        {
            invalid++;
            await Console.Error.WriteLineAsync(
                $"{Path.GetRelativePath(root, file)}: a ready-for-agent ticket needs a '## Agent Brief' section."
            );
        }

        if (done && staged.Contains(Path.GetFullPath(file)))
        {
            // Only the Agent Brief holds acceptance boxes; a checklist elsewhere in the ticket is not checked.
            bool inBrief = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal))
                {
                    inBrief = lines[i].StartsWith("## Agent Brief", StringComparison.Ordinal);
                }
                else if (inBrief && uncheckedBox.IsMatch(lines[i]))
                {
                    invalid++;
                    string lineNumber = (i + 1).ToString(CultureInfo.InvariantCulture);
                    await Console.Error.WriteLineAsync(
                        $"{Path.GetRelativePath(root, file)}:{lineNumber}: a done ticket has an unchecked acceptance box: \"{lines[i].Trim()}\""
                    );
                }
            }
        }
    }

    if (invalid > 0)
    {
        await Console.Error.WriteLineAsync(
            $"validate-triage-labels: {invalid} problem(s): a Status line that does not lead with needs-triage, needs-info, "
                + "ready-for-agent, ready-for-human, wontfix or done, a staged ready-for-agent ticket with no Agent Brief, "
                + "or a staged done ticket with an unchecked Agent Brief box (see docs/agents/triage-labels.md)."
        );
        return 1;
    }

    await Console.Out.WriteLineAsync(
        "validate-triage-labels: every issue ticket and spec/plan/map Status line carries a valid triage label."
    );
    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
