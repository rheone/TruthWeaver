namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The key tables of <c>docs/tui.md</c> (the tables under File, Edit, Rule, Analyze, Export, View and Help) and the commands
/// of the workbench give the same keys, group by group. The commands are the one source: a key in the code that its table
/// lacks, a key in a table that no command of that group has, a key under the wrong group, or a missing group heading fails
/// here and names the command, the key and the group. The Pane keys table is not checked this way, because pane keys belong
/// to the panes and not to a command.
/// </summary>
public sealed class KeyTableDocumentationTests
{
    // The headings that start a key table for one menu group; each is the name of a CommandGroup. "### Pane keys" ends the
    // checked part.
    private static readonly string[] GroupHeadings = ["File", "Edit", "Rule", "Analyze", "Export", "View", "Help"];

    /// <summary>
    /// The key table of every menu group in the documentation lists exactly the keys and second keys of the commands of that
    /// group, so a changed or new key cannot stay undocumented and a table cannot list a key that no command has.
    /// </summary>
    [Fact]
    public void KeyTables_EveryGroupTable_ListsTheKeysOfTheCommandsOfThatGroup_Test()
    {
        string[] problems = [.. CheckTables(ReadDocument(), WorkbenchCommands.Default)];

        Assert.Empty(problems);
    }

    /// <summary>
    /// A key listed under the wrong menu group fails twice and names the command, the key and the groups: the right table
    /// lacks it, and the wrong table lists a key that belongs to another group.
    /// </summary>
    [Fact]
    public void KeyTables_KeyListedUnderTheWrongGroup_FailsNamingTheCommandTheKeyAndTheGroups_Test()
    {
        // Move the row of the canonical format from the end of the Edit table to the File table.
        const string row = "| F8 | Formats the rule: canonical. |\n";
        const string fileRow = "| Ctrl+Q | Quits.";
        string text = ReadDocument();
        Assert.Contains(row, text, StringComparison.Ordinal);
        string moved = text.Replace(row, string.Empty, StringComparison.Ordinal)
            .Replace(fileRow, row + fileRow, StringComparison.Ordinal);

        string[] problems = [.. CheckTables(moved, WorkbenchCommands.Default)];

        Assert.Contains(problems, p => Names(p, "'Format: canonical'", "'F8'", "'Edit'"));
        Assert.Contains(problems, p => Names(p, "'Format: canonical'", "'F8'", "'File'"));
    }

    /// <summary>A renamed group heading fails with a message that names the heading, and not with a pass over zero rows.</summary>
    [Fact]
    public void KeyTables_RenamedGroupHeading_FailsNamingTheHeading_Test()
    {
        string renamed = ReadDocument().Replace("### Analyze\n", "### Analysis\n", StringComparison.Ordinal);

        string[] problems = [.. CheckTables(renamed, WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.Contains("'### Analyze'", problem, StringComparison.Ordinal);
    }

    /// <summary>A key that a command has and the table of its group lacks fails and names the command, the key and the group.</summary>
    [Fact]
    public void KeyTables_KeyMissingFromTheTable_FailsNamingTheCommandTheKeyAndTheGroup_Test()
    {
        string without = ReadDocument().Replace("| Ctrl+Y | Redo. |\n", string.Empty, StringComparison.Ordinal);

        string[] problems = [.. CheckTables(without, WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.True(Names(problem, "'Redo'", "'Ctrl+Y'", "'Edit'"), problem);
    }

    /// <summary>A table row for a key that no command has fails and names the key and the table.</summary>
    [Fact]
    public void KeyTables_KeyOfNoCommand_FailsNamingTheKeyAndTheTable_Test()
    {
        const string redo = "| Ctrl+Y | Redo. |\n";
        string extra = ReadDocument().Replace(redo, redo + "| Ctrl+Shift+F9 | Does nothing. |\n", StringComparison.Ordinal);

        string[] problems = [.. CheckTables(extra, WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.True(Names(problem, "'Ctrl+Shift+F9'", "'Edit'"), problem);
    }

    // Tells whether a problem message contains every one of the parts.
    private static bool Names(string problem, params string[] parts)
    {
        return parts.All(part => problem.Contains(part, StringComparison.Ordinal));
    }

    // Compares each group table of the document with the commands of the same group. The result is empty when they agree.
    private static IEnumerable<string> CheckTables(string document, IReadOnlyList<WorkbenchCommand> commands)
    {
        Dictionary<string, List<string>> tables = ReadTables(document);
        foreach (string heading in GroupHeadings)
        {
            if (!tables.TryGetValue(heading, out List<string>? listed))
            {
                yield return $"The heading '### {heading}' is missing from the key tables of docs/tui.md";
                continue;
            }

            CommandGroup group = Enum.Parse<CommandGroup>(heading);
            WorkbenchCommand[] ofGroup = [.. commands.Where(c => c.Group == group)];

            foreach (WorkbenchCommand command in ofGroup)
            {
                foreach (Key key in KeysOf(command).Where(k => !listed.Contains(DocText(k))))
                {
                    yield return $"Command '{command.Title}': key '{DocText(key)}' is missing from the '{heading}' table of docs/tui.md";
                }
            }

            foreach (string key in listed.Where(k => !ofGroup.Any(c => KeysOf(c).Any(ck => DocText(ck) == k))))
            {
                // A key that another group owns is a key under the wrong group; naming that command points at the fix.
                WorkbenchCommand? other = commands.FirstOrDefault(c => KeysOf(c).Any(ck => DocText(ck) == key));
                yield return other is null
                    ? $"Table '{heading}': key '{key}' belongs to no command"
                    : $"Table '{heading}': key '{key}' belongs to the command '{other.Title}' of the '{other.Group}' group";
            }
        }
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

    // The keys of the first column of each group table, by group heading. A group whose heading is absent has no entry. A
    // row such as "(menu only)" or "File > Open recent" names no key and gives none.
    private static Dictionary<string, List<string>> ReadTables(string document)
    {
        Dictionary<string, List<string>> tables = [];
        List<string>? current = null;
        foreach (string line in document.Split('\n'))
        {
            if (line.StartsWith('#'))
            {
                string heading = line.StartsWith("### ", StringComparison.Ordinal) ? line[4..].Trim() : string.Empty;
                current = null;
                if (GroupHeadings.Contains(heading))
                {
                    current = [];
                    tables[heading] = current;
                }

                continue;
            }

            if (current is null || !line.StartsWith("| ", StringComparison.Ordinal))
            {
                continue;
            }

            string cell = line.Split('|')[1].Trim();
            if (cell is "Key" or "---" || cell.StartsWith('('))
            {
                continue;
            }

            current.AddRange(
                cell.Replace(" or ", ", ", StringComparison.Ordinal)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Where(k => !k.Contains('>', StringComparison.Ordinal))
            );
        }

        return tables;
    }

    // The text of docs/tui.md with LF line endings.
    private static string ReadDocument()
    {
        return File.ReadAllText(FindDocument()).Replace("\r\n", "\n", StringComparison.Ordinal);
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
