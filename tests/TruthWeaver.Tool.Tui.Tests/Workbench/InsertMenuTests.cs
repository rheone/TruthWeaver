namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;
using TruthWeaver.Yaml;

/// <summary>The Insert menu: every entry has a description from the library and compiles, and an insert goes into the editor at the cursor.</summary>
public sealed class InsertMenuTests
{
    /// <summary>The Insert group has a menu, so the menu bar shows it.</summary>
    [Fact]
    public void GroupsOf_DefaultCommands_HasAnInsertMenu_Test()
    {
        IReadOnlyList<WorkbenchMenuGroup> menus = WorkbenchMenus.GroupsOf(WorkbenchCommands.Default, WorkbenchCommands.Choices);

        Assert.Contains(menus, m => m.Group == CommandGroup.Insert && m.Commands.Count > 0);
    }

    /// <summary>An inserted operator or constant has a description, and the operators are in the four groups of the submenu.</summary>
    [Fact]
    public void AllFixed_EveryOperatorAndConstant_HasADescription_Test()
    {
        Assert.Equal(["Connectives", "Cardinality", "Inspection", "Other"], InsertItems.Operators.Select(g => g.Title));
        Assert.Equal(["True", "False", "Unknown", "T", "F", "U"], InsertItems.Constants.Select(i => i.Name));
        Assert.All(InsertItems.AllFixed, i => Assert.False(string.IsNullOrWhiteSpace(i.Description), i.Name));
        Assert.All(InsertItems.AllFixed, i => Assert.DoesNotContain('\n', i.Description));
    }

    /// <summary>The description of an operator is the description of the operator table of the library, not a copy.</summary>
    [Fact]
    public void Operators_Description_IsTheDescriptionOfTheLibrary_Test()
    {
        InsertItem and = InsertItems.Operators.SelectMany(g => g.Items).Single(i => i.Name == "AND");
        CompilationResult<StubAssignment> compiled = RuleConverter
            .CreateCompiler(ToolSettings.Default)
            .CompileAs("a AND b", RuleFormat.Text);

        Assert.Equal(compiled.Rule!.Outline().Description, and.Description);
    }

    /// <summary>The text of every operator and constant compiles as rule text.</summary>
    [Fact]
    public void AllFixed_EveryEntry_CompilesAsRuleText_Test()
    {
        foreach (
            InsertItem item in InsertItems
                .AllFixed.Append(InsertItems.NewDefinition)
                .Append(InsertItems.Variable("user", "$.role"))
        )
        {
            CompilationResult<StubAssignment> result = RuleConverter
                .CreateCompiler(ToolSettings.Default)
                .CompileAs(item.InRule, RuleFormat.Text);

            Assert.True(
                result.Rule is not null,
                $"{item.Name}: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}"
            );
        }
    }

    /// <summary>Every predicate of the catalog has a description, and its call with placeholders compiles.</summary>
    [Fact]
    public void PredicateText_EveryCatalogPredicate_HasADescriptionAndCompiles_Test()
    {
        Assert.NotEmpty(PredicateCatalog.All);
        foreach (CatalogPredicate predicate in PredicateCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(predicate.Entry.Description), predicate.Entry.Name);

            ToolResult<string> text = InsertItems.PredicateText(predicate.Entry, ToolSettings.Default);
            Assert.True(text.Succeeded, predicate.Entry.Name);
            Assert.True(
                RuleConverter.CreateCompiler(ToolSettings.Default).CompileAs(text.Value, RuleFormat.Text).Rule is not null,
                $"{predicate.Entry.Name}: {text.Value}"
            );
        }
    }

    /// <summary>The search of the predicate list shows every entry for an empty search and the matches of the catalog for words.</summary>
    [Fact]
    public void FilterPredicates_WordsAndAnEmptySearch_ShowTheMatches_Test()
    {
        PaletteEntry[] entries = [.. PredicateCatalog.All.Select(p => p.Entry)];
        string[] lines = [.. entries.Select(e => e.Display)];

        IReadOnlyList<int> all = InsertCommands.FilterPredicates(entries, lines, string.Empty);
        IReadOnlyList<int> starts = InsertCommands.FilterPredicates(entries, lines, "starts");

        Assert.Equal(entries.Length, all.Count);
        Assert.Contains(starts, i => entries[i].Name == "StartsWith");
        Assert.True(starts.Count < all.Count);
    }

    /// <summary>The Predicate entry inserts the chosen call with placeholders at the cursor.</summary>
    [Fact]
    public void ChoosePredicate_PicksACatalogPredicate_InsertsItsCall_Test()
    {
        using WorkbenchHarness harness = new();
        ScriptedServices services = new();
        int index = PredicateCatalog.All.ToList().FindIndex(p => p.Entry.Name == "StartsWith");
        services.Dialogs.Choices.Enqueue(index);

        bool inserted = InsertCommands.ChoosePredicate(harness.Workbench, services.Services);

        Assert.True(inserted);
        Assert.StartsWith("StartsWith(", harness.Workbench.Document.Text, StringComparison.Ordinal);
        Assert.True(harness.Workbench.Document.IsRuleCurrent);
    }

    /// <summary>An insert goes at the cursor, and Ctrl+Z undoes it.</summary>
    [Fact]
    public void InsertText_AtTheCursor_PutsTheTextThereAndUndoRestoresIt_Test()
    {
        using WorkbenchHarness harness = new("a AND ");
        harness.Press(Key.End.WithCtrl);

        bool inserted = harness.Workbench.InsertText("(b OR c)", "OR");
        string after = harness.Workbench.Document.Text;
        harness.Press(Key.Z.WithCtrl);

        Assert.True(inserted);
        Assert.Equal("a AND (b OR c)", after);
        Assert.Equal("a AND ", harness.Workbench.Document.Text);
    }

    /// <summary>An insert replaces the selection.</summary>
    [Fact]
    public void InsertText_WithASelection_ReplacesTheSelection_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        RuleEditorView editor = Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
        editor.SelectRange(6, 7);

        harness.Workbench.InsertText("True", "True");

        Assert.Equal("a AND True", harness.Workbench.Document.Text);
        Assert.True(harness.Workbench.Document.IsRuleCurrent);
    }

    /// <summary>JSON and YAML take no insert: the text stays and the status line says why.</summary>
    [Theory]
    [InlineData(RuleFormat.Json, """{"predicate":"a"}""")]
    [InlineData(RuleFormat.Yaml, "predicate: a")]
    public void InsertText_InJsonOrYaml_IsNotAvailableAndSaysWhy_Test(RuleFormat format, string text)
    {
        Workbench workbench = new(RuleDocument.Create(text, format), ToolSettings.Default, new ScriptedPrompts());

        bool inserted = workbench.InsertText("True", "True");

        Assert.False(inserted);
        Assert.Equal(text, workbench.Document.Text);
        Assert.Contains("not available", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>The variable entry asks for the source and the query and inserts the reference.</summary>
    [Fact]
    public void AskVariable_WithASourceAndAQuery_InsertsTheReference_Test()
    {
        using WorkbenchHarness harness = new("hasRole(role: )");
        ScriptedServices services = new();
        services.Dialogs.Texts.Enqueue("user");
        services.Dialogs.Texts.Enqueue("$.role");
        harness.Press(Key.End.WithCtrl, Key.CursorLeft);

        bool inserted = InsertCommands.AskVariable(harness.Workbench, services.Services);

        Assert.True(inserted);
        Assert.Equal("""hasRole(role: from("user", "$.role"))""", harness.Workbench.Document.Text);
    }

    /// <summary>The use entry lists the definitions of the open rule and inserts a use with the parameter names; with none, it says so.</summary>
    [Fact]
    public void ChooseDefinition_WithAndWithoutDefinitions_InsertsAUseOrSaysThereIsNone_Test()
    {
        using WorkbenchHarness none = new("a");
        using WorkbenchHarness some = new("let both(x, y) = x AND y;\nboth(a, b)");
        ScriptedServices services = new();
        services.Dialogs.Choices.Enqueue(0);

        bool noneInserted = InsertCommands.ChooseDefinition(none.Workbench, services.Services);
        some.Press(Key.End.WithCtrl);
        bool someInserted = InsertCommands.ChooseDefinition(some.Workbench, services.Services);

        Assert.False(noneInserted);
        Assert.Contains("no let definition", none.Workbench.Status, StringComparison.Ordinal);
        Assert.True(someInserted);
        Assert.EndsWith("both(a, b)both(x, y)", some.Workbench.Document.Text, StringComparison.Ordinal);
    }

    /// <summary>A cancelled list inserts nothing.</summary>
    [Fact]
    public void ChoosePredicate_Cancelled_InsertsNothing_Test()
    {
        using WorkbenchHarness harness = new("a");

        bool inserted = InsertCommands.ChoosePredicate(harness.Workbench, new ScriptedServices().Services);

        Assert.False(inserted);
        Assert.Equal("a", harness.Workbench.Document.Text);
    }
}
