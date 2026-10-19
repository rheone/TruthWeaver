namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The key tables of <c>docs/tui.md</c> (the tables under File, Edit, Rule, Analyze, Export, View and Help) and the commands
/// of the workbench give the same keys. The commands are the one source: a key in the code that the tables lack, or a key in
/// the tables that no command has, fails here and names the command and the key. The Pane keys table is not checked this
/// way, because pane keys belong to the panes and not to a command.
/// </summary>
public sealed class KeyTableDocumentationTests
{
    // The headings that start a key table for one menu group. "### Pane keys" ends the checked part.
    private static readonly string[] GroupHeadings = ["File", "Edit", "Rule", "Analyze", "Export", "View", "Help"];

    /// <summary>
    /// Every key and second key of every command appears in a key table of the documentation, so a changed or new key
    /// cannot stay undocumented.
    /// </summary>
    [Fact]
    public void KeyTables_EveryCommandKeyAndSecondKey_IsListed_Test()
    {
        HashSet<string> documented = [.. ReadTableRows().SelectMany(row => row.Keys)];
        List<string> missing = [];

        foreach (WorkbenchCommand command in WorkbenchCommands.Default)
        {
            foreach (Key key in KeysOf(command))
            {
                if (!documented.Contains(DocText(key)))
                {
                    missing.Add($"Command '{command.Title}': key '{key}' is missing from the key tables of docs/tui.md");
                }
            }
        }

        Assert.Empty(missing);
    }

    /// <summary>
    /// Every key in the first column of a menu group table belongs to a command, as its key or its second key. A table
    /// row for a key that no command has fails.
    /// </summary>
    [Fact]
    public void KeyTables_EveryListedKey_BelongsToACommand_Test()
    {
        HashSet<string> known = [.. WorkbenchCommands.Default.SelectMany(KeysOf).Select(DocText)];
        List<string> unknown = [];

        foreach ((string group, string[] keys) in ReadTableRows())
        {
            unknown.AddRange(
                keys.Where(k => !known.Contains(k)).Select(k => $"Table '{group}': key '{k}' belongs to no command")
            );
        }

        Assert.Empty(unknown);
    }

    // Every key a command binds: its Key (unless it has none) and its SecondKey (when it has one).
    private static IEnumerable<Key> KeysOf(WorkbenchCommand command)
    {
        if (command.Key != Key.Empty)
        {
            yield return command.Key;
        }

        if (command.SecondKey is { } second)
        {
            yield return second;
        }
    }

    // The text of a key as the tables write it: the library writes CursorLeft where the page writes Left.
    private static string DocText(Key key)
    {
        return key.ToString().Replace("Cursor", string.Empty, StringComparison.Ordinal);
    }

    // The rows of the menu group tables: the group, and the keys of the first column. A row such as "(menu only)" or
    // "File > Open recent" names no key and gives none.
    private static IEnumerable<(string Group, string[] Keys)> ReadTableRows()
    {
        string group = string.Empty;
        foreach (string line in File.ReadLines(FindDocument()))
        {
            if (line.StartsWith('#'))
            {
                group = line.StartsWith("### ", StringComparison.Ordinal) ? line[4..].Trim() : string.Empty;
                continue;
            }

            if (!GroupHeadings.Contains(group) || !line.StartsWith("| ", StringComparison.Ordinal))
            {
                continue;
            }

            string cell = line.Split('|')[1].Trim();
            if (cell is "Key" or "---" || cell.StartsWith('('))
            {
                continue;
            }

            string[] keys =
            [
                .. cell.Replace(" or ", ", ", StringComparison.Ordinal)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Where(k => !k.Contains('>', StringComparison.Ordinal)),
            ];
            yield return (group, keys);
        }
    }

    // docs/tui.md, found by walking up from the test output folder to the repository root.
    private static string FindDocument()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "docs", "tui.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("docs/tui.md was not found above the test folder.");
    }
}
