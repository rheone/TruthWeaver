namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The palette of the structural editor with the ready-made predicates of the catalog.</summary>
public sealed class CatalogPaletteTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("tw-catalog-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    /// <summary>The list holds the catalog after the predicates of the open rule, in family groups, and every predicate is in it.</summary>
    [Fact]
    public void InsertOperand_List_HoldsTheCatalogAfterTheRulePredicatesInFamilyGroups_Test()
    {
        (StructureController controller, ScriptedServices services, _) = Create("a AND hasRole(role: \"viewer\")");

        controller.InsertOperand();

        IReadOnlyList<string> items = services.Dialogs.LastItems;
        int rule = items.ToList().FindIndex(i => i.Contains("hasRole(", StringComparison.Ordinal));
        int firstGroup = items.ToList().FindIndex(i => i.StartsWith("--- ", StringComparison.Ordinal));
        Assert.Equal(StructureController.TypeExpression, items[0]);
        Assert.Equal(StructureController.SearchCatalog, items[1]);
        Assert.InRange(rule, 2, firstGroup - 1);
        Assert.Contains("--- String ---", items);
        Assert.Contains("--- Date and time ---", items);
        foreach (CatalogPredicate predicate in PredicateCatalog.All)
        {
            Assert.Contains(items, i => i.StartsWith("catalog  " + predicate.Entry.Name, StringComparison.Ordinal));
        }

        Assert.Equal(PredicateCatalog.All.Count, items.Count(i => i.StartsWith("catalog ", StringComparison.Ordinal)));
    }

    /// <summary>Picking a catalog predicate opens the argument form and adds the call as an operand.</summary>
    [Fact]
    public void InsertOperand_CatalogPredicate_OpensTheFormAndAddsTheCall_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        controller.InsertOperand();
        int index = services
            .Dialogs.LastItems.ToList()
            .FindIndex(i => i.StartsWith("catalog  StartsWith(", StringComparison.Ordinal));
        services.Dialogs.Choices.Enqueue(index);
        services.Dialogs.Texts.Enqueue("abc");

        bool changed = controller.InsertOperand();

        Assert.True(changed);
        Assert.Equal("a AND b AND StartsWith(value: \"abc\")", workbench.Document.Text);
        Assert.NotNull(workbench.Document.Rule);
    }

    /// <summary>The search entry asks for words, lists the matches in groups and adds the one that the author picks.</summary>
    [Fact]
    public void InsertOperand_SearchByAlias_ListsTheMatchesAndAddsThePick_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Dialogs.Choices.Enqueue(1);
        services.Dialogs.Texts.Enqueue("Older Than");

        controller.InsertOperand();
        IReadOnlyList<string> matches = services.Dialogs.LastItems;
        services.Dialogs.Choices.Enqueue(1);
        services.Dialogs.Choices.Enqueue(matches.ToList().FindIndex(i => i.Contains("IsBefore(", StringComparison.Ordinal)));
        services.Dialogs.Texts.Enqueue("Older Than");
        services.Dialogs.Texts.Enqueue("2026-01-01T00:00:00Z");
        bool changed = controller.InsertOperand();

        Assert.StartsWith("--- ", matches[0], StringComparison.Ordinal);
        Assert.True(changed);
        Assert.Contains("IsBefore(", workbench.Document.Text, StringComparison.Ordinal);
    }

    /// <summary>A search that matches nothing says so on the status line and changes nothing.</summary>
    [Fact]
    public void InsertOperand_SearchWithoutMatch_SaysSoAndChangesNothing_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Dialogs.Choices.Enqueue(1);
        services.Dialogs.Texts.Enqueue("zzzqqq");

        bool changed = controller.InsertOperand();

        Assert.False(changed);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Contains("No predicate of the catalog matches 'zzzqqq'", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A group line is not an entry: picking it opens the list again.</summary>
    [Fact]
    public void InsertOperand_GroupLinePicked_OpensTheListAgain_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        controller.InsertOperand();
        List<string> items = [.. services.Dialogs.LastItems];
        services.Dialogs.Choices.Enqueue(items.IndexOf("--- String ---"));
        services.Dialogs.Choices.Enqueue(items.FindIndex(i => i.StartsWith("catalog  IsEmpty -", StringComparison.Ordinal)));

        bool changed = controller.InsertOperand();

        Assert.True(changed);
        Assert.Contains("IsEmpty", workbench.Document.Text, StringComparison.Ordinal);
    }

    /// <summary>A schema file that lacks the picked predicate gets a notice. The predicate goes in and the file stays as it was.</summary>
    [Fact]
    public void InsertOperand_SchemaFileWithoutThePredicate_ShowsTheNoticeAndLeavesTheFile_Test()
    {
        string path = this.WriteSchema(
            "predicates:\n  hasRole:\n    description: The user has the role.\n    arguments:\n      role: string\n"
        );
        string before = File.ReadAllText(path);
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b", path);
        controller.InsertOperand();
        services.Dialogs.Choices.Enqueue(
            services.Dialogs.LastItems.ToList().FindIndex(i => i.StartsWith("catalog  StartsWith(", StringComparison.Ordinal))
        );
        services.Dialogs.Texts.Enqueue("abc");

        bool changed = controller.InsertOperand();

        Assert.True(changed);
        Assert.Contains("StartsWith(value: \"abc\")", workbench.Document.Text, StringComparison.Ordinal);
        Assert.Contains(
            "The schema file does not declare 'StartsWith'; the schema validation level will flag it.",
            workbench.Status,
            StringComparison.Ordinal
        );
        Assert.Equal(before, File.ReadAllText(path));
    }

    /// <summary>A schema file that declares the picked predicate gets no notice.</summary>
    [Fact]
    public void InsertOperand_SchemaFileWithThePredicate_ShowsNoNotice_Test()
    {
        string path = this.WriteSchema(
            "predicates:\n  StartsWith:\n    description: Starts with.\n    arguments:\n      value: string\n"
        );
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b", path);
        controller.InsertOperand();
        services.Dialogs.Choices.Enqueue(
            services.Dialogs.LastItems.ToList().FindIndex(i => i.StartsWith("catalog  StartsWith(", StringComparison.Ordinal))
        );
        services.Dialogs.Texts.Enqueue("abc");

        bool changed = controller.InsertOperand();

        Assert.True(changed);
        Assert.DoesNotContain("schema validation level", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>With no schema file set, there is no notice.</summary>
    [Fact]
    public void InsertOperand_NoSchemaFile_ShowsNoNotice_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        controller.InsertOperand();
        services.Dialogs.Choices.Enqueue(
            services.Dialogs.LastItems.ToList().FindIndex(i => i.StartsWith("catalog  StartsWith(", StringComparison.Ordinal))
        );
        services.Dialogs.Texts.Enqueue("abc");

        controller.InsertOperand();

        Assert.DoesNotContain("schema validation level", workbench.Status, StringComparison.Ordinal);
    }

    private static (StructureController Controller, ScriptedServices Services, Workbench Workbench) Create(
        string text,
        string? schemaFile = null
    )
    {
        ScriptedServices services = new();
        ToolSettings settings = ToolSettings.Default with { SchemaFile = schemaFile };
        Workbench workbench = new(RuleDocument.Create(text), settings, new ScriptedPrompts());
        return (new StructureController(workbench, services.Services), services, workbench);
    }

    private string WriteSchema(string yaml)
    {
        string path = Path.Combine(this.directory, "predicates.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }
}
