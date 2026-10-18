#!/usr/bin/env dotnet
#:include shared/Repo.cs
#:include shared/ProcessResult.cs
#:include shared/ScriptFailedException.cs

// Lists tickets marked done that still carry an unticked checklist item.
//
// Scans every Markdown file under .scratch/ whose status line starts with "done" and prints each "- [ ]" item.
// The folders engine-v1 and _superseded are skipped. An open item is "noted" when its line records why it stays
// open: a "Note YYYY-MM-DD:" marker or a "(waived:" marker. The exit code is 1 when any open item has no note,
// so a clean run means every remaining item explains itself.
// Ticking an item or adding a note is a manual step. Check the evidence (code, tests, commit) first.
using System.Text.RegularExpressions;
using TruthWeaver.Scripts;

try
{
    string root = await Repo.FindRootAsync();
    string scratch = Path.Combine(root, ".scratch");
    int unnoted = 0;

    // The patterns run on local files only, but a timeout keeps a pathological line from hanging the script.
    TimeSpan timeout = TimeSpan.FromSeconds(5);
    Regex status = new(@"^\s*(\*\*Status:\*\*|Status:)\s*done\b", RegexOptions.Compiled, timeout);
    Regex note = new(@"Note \d{4}-\d{2}-\d{2}:|\(waived:", RegexOptions.Compiled, timeout);
    Regex skipped = new(@"[\\/](engine-v1|_superseded)[\\/]", RegexOptions.Compiled, timeout);
    Regex openItem = new(@"^\s*- \[ \]", RegexOptions.Compiled, timeout);

    IEnumerable<string> files = Directory
        .EnumerateFiles(scratch, "*.md", SearchOption.AllDirectories)
        .Where(path => !skipped.IsMatch(path))
        .Order(StringComparer.OrdinalIgnoreCase);

    foreach (string file in files)
    {
        string[] lines = await File.ReadAllLinesAsync(file);
        if (!lines.Any(line => status.IsMatch(line)))
        {
            continue;
        }

        List<(int Line, string Text)> open = [];
        for (int i = 0; i < lines.Length; i++)
        {
            if (openItem.IsMatch(lines[i]))
            {
                open.Add((i + 1, lines[i].Trim()));
            }
        }

        if (open.Count == 0)
        {
            continue;
        }

        await Console.Out.WriteLineAsync(Path.GetRelativePath(root, file));
        foreach ((int line, string text) in open)
        {
            bool noted = note.IsMatch(text);
            if (!noted)
            {
                unnoted++;
            }

            string number = line.ToString(CultureInfo.InvariantCulture).PadLeft(4);
            string tag = noted ? "noted" : "NO NOTE";
            await Console.Out.WriteLineAsync($"  {number} [{tag}] {text}");
        }
    }

    if (unnoted > 0)
    {
        await Console.Error.WriteLineAsync($"list-open-done-tickets: {unnoted} open item(s) have no note.");
        return 1;
    }

    await Console.Out.WriteLineAsync("list-open-done-tickets: every open item in a done ticket has a note.");
    return 0;
}
catch (ScriptFailedException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return exception.ExitCode;
}
