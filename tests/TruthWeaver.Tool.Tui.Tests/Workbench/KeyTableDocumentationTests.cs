namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The key tables of <c>docs/tui.md</c> (the tables under File, Edit, Rule, Analyze, Export, View and Help) and the commands
/// of the workbench give the same keys, group by group. The commands are the one source: a key in the code that its table
/// lacks, a key in a table that no command of that group has, a key under the wrong group, a missing or repeated group
/// heading fails here and names the command, the key and the group. The Pane keys table is not checked this way, because
/// pane keys belong to the panes and not to a command.
/// </summary>
public sealed class KeyTableDocumentationTests
{
    // The headings that start a key table for one menu group; each is the name of a CommandGroup. "### Pane keys" ends the
    // checked part.
    private static readonly string[] GroupHeadings = ["File", "Edit", "Rule", "Analyze", "Export", "View", "Help"];

    /// <summary>Gets the group headings as theory data, one row per heading.</summary>
    public static TheoryData<string> Headings => [.. GroupHeadings];

    /// <summary>
    /// The key table of every menu group in the documentation lists exactly the keys and second keys of the commands of that
    /// group, so a changed or new key cannot stay undocumented and a table cannot list a key that no command has.
    /// </summary>
    [Fact]
    public void CheckTables_EveryGroupTable_ListsTheKeysOfTheCommandsOfThatGroup_Test()
    {
        string[] problems = [.. CheckTables(ReadDocument(), WorkbenchCommands.Default)];

        Assert.Empty(problems);
    }

    /// <summary>
    /// A key listed under the wrong menu group fails twice and names the command, the key and the groups: the right table
    /// lacks it, and the wrong table lists a key that belongs to another group. The test moves the row of a command from its
    /// own table to the table of another group.
    /// </summary>
    [Fact]
    public void CheckTables_KeyListedUnderTheWrongGroup_FailsNamingTheCommandTheKeyAndTheGroups_Test()
    {
        WorkbenchCommand command = WorkbenchCommands.Default.First(c => c.Group == CommandGroup.Edit && c.Key != Key.Empty);
        string group = command.Group.ToString();
        string key = DocKeysOf(command)[0];
        string other = GroupHeadings.First(h => h != group);
        List<string> lines = [.. ReadDocument().Split('\n')];
        int rowIndex = RowOf(lines, group, key);
        string row = lines[rowIndex];
        lines.RemoveAt(rowIndex);
        lines.Insert(LastRowOf(lines, other) + 1, row);

        string[] problems = [.. CheckTables(string.Join('\n', lines), WorkbenchCommands.Default)];

        Assert.Contains(problems, p => Names(p, $"'{command.Title}'", $"'{key}'", $"'{group}'"));
        Assert.Contains(problems, p => Names(p, $"'{command.Title}'", $"'{key}'", $"'{other}'"));
    }

    /// <summary>
    /// A group heading that is renamed fails with a message that names the heading, and not with a pass over zero rows. Every
    /// group heading is renamed in turn.
    /// </summary>
    /// <param name="heading">The group heading to rename.</param>
    [Theory]
    [MemberData(nameof(Headings))]
    public void CheckTables_RenamedGroupHeading_FailsNamingTheHeading_Test(string heading)
    {
        List<string> lines = [.. ReadDocument().Split('\n')];
        lines[HeadingOf(lines, heading)] = $"### {heading} renamed";

        string[] problems = [.. CheckTables(string.Join('\n', lines), WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.Contains($"'### {heading}'", problem, StringComparison.Ordinal);
    }

    /// <summary>
    /// A group heading that appears twice fails with a message that names the heading, and the second table does not
    /// replace the first without notice. Every group heading is repeated in turn, with its table.
    /// </summary>
    /// <param name="heading">The group heading to repeat.</param>
    [Theory]
    [MemberData(nameof(Headings))]
    public void CheckTables_DuplicatedGroupHeading_FailsNamingTheHeading_Test(string heading)
    {
        List<string> lines = [.. ReadDocument().Split('\n')];
        int start = HeadingOf(lines, heading);
        int end = SectionEnd(lines, start);
        lines.InsertRange(end, lines.GetRange(start, end - start));

        string[] problems = [.. CheckTables(string.Join('\n', lines), WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.Contains($"'### {heading}'", problem, StringComparison.Ordinal);
    }

    /// <summary>A key that a command has and the table of its group lacks fails and names the command, the key and the group.</summary>
    [Fact]
    public void CheckTables_KeyMissingFromTheTable_FailsNamingTheCommandTheKeyAndTheGroup_Test()
    {
        WorkbenchCommand command = WorkbenchCommands.Default.Single(c => c.Title == "Redo");
        string group = command.Group.ToString();
        string key = Assert.Single(DocKeysOf(command));
        List<string> lines = [.. ReadDocument().Split('\n')];
        lines.RemoveAt(RowOf(lines, group, key));

        string[] problems = [.. CheckTables(string.Join('\n', lines), WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.True(Names(problem, $"'{command.Title}'", $"'{key}'", $"'{group}'"), problem);
    }

    /// <summary>A table row for a key that no command has fails and names the key and the table.</summary>
    [Fact]
    public void CheckTables_KeyOfNoCommand_FailsNamingTheKeyAndTheTable_Test()
    {
        // A key that no command binds; the check keeps the test from passing for the wrong reason if a command takes it.
        string unused = DocText(Key.F9.WithCtrl.WithShift);
        Assert.DoesNotContain(unused, WorkbenchCommands.Default.SelectMany(DocKeysOf));
        List<string> lines = [.. ReadDocument().Split('\n')];
        lines.Insert(LastRowOf(lines, "Edit") + 1, $"| {unused} | Does nothing. |");

        string[] problems = [.. CheckTables(string.Join('\n', lines), WorkbenchCommands.Default)];

        string problem = Assert.Single(problems);
        Assert.True(Names(problem, $"'{unused}'", "'Edit'"), problem);
    }

    // Tells whether a problem message contains every one of the parts.
    private static bool Names(string problem, params string[] parts)
    {
        return parts.All(part => problem.Contains(part, StringComparison.Ordinal));
    }

    // The line of a group heading in the document; a missing heading fails the test.
    private static int HeadingOf(List<string> lines, string heading)
    {
        int index = lines.IndexOf($"### {heading}");
        Assert.True(index >= 0, $"The heading '### {heading}' is not in the document.");
        return index;
    }

    // The line after the last line of the section that starts at a heading: the next heading, or the end of the document.
    private static int SectionEnd(List<string> lines, int start)
    {
        int next = lines.FindIndex(start + 1, l => l.StartsWith('#'));
        return next < 0 ? lines.Count : next;
    }

    // The line of the last table row in the table under a group heading.
    private static int LastRowOf(List<string> lines, string heading)
    {
        int start = HeadingOf(lines, heading);
        return lines.FindLastIndex(SectionEnd(lines, start) - 1, l => l.StartsWith("| ", StringComparison.Ordinal));
    }

    // The line of the table row, under a group heading, that lists the key.
    private static int RowOf(List<string> lines, string heading, string key)
    {
        int start = HeadingOf(lines, heading);
        int end = SectionEnd(lines, start);
        for (int i = start + 1; i < end; i++)
        {
            if (lines[i].StartsWith("| ", StringComparison.Ordinal) && KeysIn(lines[i].Split('|')[1]).Contains(key))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"The '{heading}' table has no row for '{key}'.");
    }

    // Compares each group table of the document with the commands of the same group. The result is empty when they agree.
    private static IEnumerable<string> CheckTables(string document, IReadOnlyList<WorkbenchCommand> commands)
    {
        foreach (string duplicate in DuplicatedHeadings(document))
        {
            yield return $"The heading '### {duplicate}' appears more than once in the key tables of docs/tui.md";
        }

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
                foreach (string key in DocKeysOf(command).Where(k => !listed.Contains(k)))
                {
                    yield return $"Command '{command.Title}': key '{key}' is missing from the '{heading}' table of docs/tui.md";
                }
            }

            foreach (string key in listed.Where(k => !ofGroup.Any(c => DocKeysOf(c).Contains(k))))
            {
                // A key that another group owns is a key under the wrong group; naming that command points at the fix.
                WorkbenchCommand? other = commands.FirstOrDefault(c => DocKeysOf(c).Contains(key));
                yield return other is null
                    ? $"Table '{heading}': key '{key}' belongs to no command"
                    : $"Table '{heading}': key '{key}' belongs to the command '{other.Title}' of the '{other.Group}' group";
            }
        }
    }

    // The keys of a command as the tables write them: its Key (unless it has none) and its SecondKey (when it has one).
    private static List<string> DocKeysOf(WorkbenchCommand command)
    {
        List<string> keys = [];
        if (command.Key != Key.Empty)
        {
            keys.Add(DocText(command.Key));
        }

        if (command.SecondKey is { } second)
        {
            keys.Add(DocText(second));
        }

        return keys;
    }

    // The text of a key as the tables write it: the library writes CursorLeft where the page writes Left.
    private static string DocText(Key key)
    {
        return key.ToString().Replace("Cursor", string.Empty, StringComparison.Ordinal);
    }

    // The group headings that the document has more than once.
    private static IEnumerable<string> DuplicatedHeadings(string document)
    {
        return document
            .Split('\n')
            .Where(l => l.StartsWith("### ", StringComparison.Ordinal))
            .Select(l => l[4..].Trim())
            .Where(GroupHeadings.Contains)
            .GroupBy(h => h)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
    }

    // The keys in the first cell of a table row: "Ctrl+Y or Alt+Y" gives two. A cell such as "(menu only)" or
    // "File > Open recent" names no key and gives none.
    private static IEnumerable<string> KeysIn(string cell)
    {
        string trimmed = cell.Trim();
        if (trimmed is "Key" or "---" || trimmed.StartsWith('('))
        {
            return [];
        }

        return trimmed
            .Replace(" or ", ", ", StringComparison.Ordinal)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(k => !k.Contains('>', StringComparison.Ordinal));
    }

    // The keys of the first column of each group table, by group heading. A group whose heading is absent has no entry. The
    // first table of a repeated heading is kept; DuplicatedHeadings reports the repeat.
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
                    List<string> table = [];
                    if (tables.TryAdd(heading, table))
                    {
                        current = table;
                    }
                }

                continue;
            }

            if (current is not null && line.StartsWith("| ", StringComparison.Ordinal))
            {
                current.AddRange(KeysIn(line.Split('|')[1]));
            }
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
