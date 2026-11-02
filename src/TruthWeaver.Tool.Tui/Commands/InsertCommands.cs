namespace TruthWeaver.Tool.Tui.Commands;

using Terminal.Gui.Input;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Tui.Controllers;
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

        RulePalette palette = PaletteController.For(workbench).Palette();
        PaletteEntry[] entries = [.. Found(palette, string.Empty)];
        string[] lines = [.. entries.Select(e => e.Display + " - " + e.Description)];

        // The dialog shows the lines whose entries the palette search finds; the list is the whole search of an empty text.
        if (
            services.Dialogs.ChooseFiltered(
                "Insert a predicate",
                lines,
                query => [.. Found(palette, query).Select(found => Array.IndexOf(entries, found))]
            )
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

    // The predicates that Insert offers: those of the schema file and the rule, then the catalog.
    private static IEnumerable<PaletteEntry> Found(RulePalette palette, string query)
    {
        return palette
            .Search(query, e => e.Source is PaletteSource.SchemaFile or PaletteSource.Document or PaletteSource.Catalog)
            .SelectMany(g => g.Entries);
    }

    // Makes the text of a predicate or a definition and inserts it. A call that does not compile is not inserted.
    private static bool Insert(Workbench workbench, PaletteEntry entry, string what)
    {
        ToolResult<string> text = InsertItems.PredicateText(entry, workbench.Settings);
        if (!text.Succeeded)
        {
            workbench.SetStatus($"Not inserted: {text.FailureSummary("the call does not compile.")}");
            return false;
        }

        return workbench.InsertText(text.Value, what);
    }
}
