namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The Insert menu: a predicate (from the catalog, the schema file or the rule), an operator, a constant, a variable or a
/// definition goes into the editor at the cursor, in place of the selection. The operators and constants are submenus that
/// <see cref="WorkbenchMenus"/> builds from <see cref="InsertItems"/>; the entries here need a dialog and are commands.
/// Nothing needs a key, so every entry is reached through the menu.
/// </summary>
internal static class InsertCommands
{
    /// <summary>The title of the submenu that lists the operators.</summary>
    public const string OperatorsTitle = "Operators";

    /// <summary>The title of the submenu that lists the constants.</summary>
    public const string ConstantsTitle = "Constants";

    /// <summary>Gets the commands. <see cref="WorkbenchCommands.Default"/> includes them.</summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new(
            Key.Empty,
            CommandGroup.Insert,
            "Predicate...",
            "Insert a predicate call from the catalog, the schema file or the rule, with a placeholder for each argument",
            w => ChoosePredicate(w.Workbench, WorkbenchServices.Current)
        ),
        new(
            Key.Empty,
            CommandGroup.Insert,
            "Variable...",
            "Insert a from(source, query) variable: it asks for the data source and the query",
            w => AskVariable(w.Workbench, WorkbenchServices.Current)
        ),
        new(
            Key.Empty,
            CommandGroup.Insert,
            InsertItems.NewDefinition.Name,
            InsertItems.NewDefinition.Description,
            w => w.Workbench.InsertText(InsertItems.NewDefinition.Text, "a definition")
        ),
        new(
            Key.Empty,
            CommandGroup.Insert,
            "Use definition...",
            "Insert a use of a let definition of the open rule, with its parameter names as placeholders",
            w => ChooseDefinition(w.Workbench, WorkbenchServices.Current)
        ),
    ];

    /// <summary>
    /// Gets the indexes of the predicate list that a search shows. An empty search shows every entry. Otherwise a catalog
    /// entry shows when the catalog search finds it (name, label, family, description and aliases), and an own entry (schema
    /// file or rule) shows when its line holds every word.
    /// </summary>
    /// <param name="entries">The entries.</param>
    /// <param name="lines">The line of each entry in the list.</param>
    /// <param name="query">The text of the search box.</param>
    /// <returns>The indexes, in order.</returns>
    public static IReadOnlyList<int> FilterPredicates(
        IReadOnlyList<PaletteEntry> entries,
        IReadOnlyList<string> lines,
        string query
    )
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(query);

        string[] words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [.. Enumerable.Range(0, entries.Count)];
        }

        HashSet<PaletteEntry> found = [.. PredicateCatalog.Search(query).Select(p => p.Entry)];
        return
        [
            .. Enumerable
                .Range(0, entries.Count)
                .Where(i =>
                    entries[i].Source == PaletteSource.Catalog
                        ? found.Contains(entries[i])
                        : words.All(w => lines[i].Contains(w, StringComparison.OrdinalIgnoreCase))
                ),
        ];
    }

    /// <summary>Asks for a predicate and inserts a call of it with a placeholder for each argument.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs.</param>
    /// <returns><see langword="true"/> when the call was inserted.</returns>
    public static bool ChoosePredicate(Workbench workbench, WorkbenchServices services)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        if (!workbench.CanInsert())
        {
            return false;
        }

        PredicateSchemaFile? schema = workbench.Settings.SchemaFile is { } path ? PredicateSchemaFile.Load(path).Value : null;
        PaletteEntry[] own =
        [
            .. RulePalette
                .Create(workbench.Document.Rule, schema)
                .Entries.Where(e => e.Source is PaletteSource.SchemaFile or PaletteSource.Document),
        ];
        PaletteEntry[] entries = [.. own, .. PredicateCatalog.All.Select(p => p.Entry)];
        string[] lines = [.. entries.Select(e => e.Display + " - " + e.Description)];

        if (
            services.Dialogs.ChooseFiltered("Insert a predicate", lines, query => FilterPredicates(entries, lines, query))
            is not { } index
        )
        {
            return false;
        }

        return Insert(workbench, entries[index], entries[index].Name);
    }

    /// <summary>Asks for a data source and a query and inserts a <c>from(...)</c> variable.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs.</param>
    /// <returns><see langword="true"/> when the variable was inserted.</returns>
    public static bool AskVariable(Workbench workbench, WorkbenchServices services)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        if (
            !workbench.CanInsert()
            || services.Dialogs.AskText("Variable: name of the data source", "user") is not { } source
            || services.Dialogs.AskText("Variable: query that reads the value from the source", "$.value") is not { } query
        )
        {
            return false;
        }

        return workbench.InsertText(InsertItems.Variable(source, query).Text, "a variable");
    }

    /// <summary>Asks for one of the definitions of the open rule and inserts a use of it.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs.</param>
    /// <returns><see langword="true"/> when the use was inserted.</returns>
    public static bool ChooseDefinition(Workbench workbench, WorkbenchServices services)
    {
        ArgumentNullException.ThrowIfNull(workbench);
        ArgumentNullException.ThrowIfNull(services);

        if (!workbench.CanInsert())
        {
            return false;
        }

        PaletteEntry[] definitions =
        [
            .. RulePalette
                .Create(null, null, workbench.Document.Definitions)
                .Entries.Where(e => e.Source == PaletteSource.Definition),
        ];
        if (definitions.Length == 0)
        {
            workbench.SetStatus("The rule has no let definition to use. Insert a new definition first.");
            return false;
        }

        if (
            services.Dialogs.Choose(
                "Use a definition",
                [.. definitions.Select(d => d.Display + " - " + d.Description)],
                "_Insert"
            )
            is not { } index
        )
        {
            return false;
        }

        return Insert(workbench, definitions[index], $"a use of {definitions[index].Name}");
    }

    // Makes the text of a predicate or a definition and inserts it. A call that does not compile is not inserted.
    private static bool Insert(Workbench workbench, PaletteEntry entry, string what)
    {
        ToolResult<string> text = InsertItems.PredicateText(entry, workbench.Settings);
        if (!text.Succeeded)
        {
            Diagnostic? first = text.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            workbench.SetStatus(
                $"Not inserted: {(first is null ? "the call does not compile." : $"{first.Code}: {first.Message}")}"
            );
            return false;
        }

        return workbench.InsertText(text.Value, what);
    }
}
