namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The import and conversion keys, with scripted dialogs, prompts and clipboard and no terminal.</summary>
public sealed class ImportControllerTests
{
    /// <summary>A clipboard that cannot be read leaves the rule alone and says so in the status line.</summary>
    [Fact]
    public void FromClipboard_ClipboardNotAvailable_KeepsTheRuleAndSaysSo_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Clipboard.Available = false;

        bool imported = controller.FromClipboard(null);

        Assert.False(imported);
        Assert.Equal("a", workbench.Document.Text);
        Assert.Contains("clipboard has no text", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A clipboard with only white space is not text to import.</summary>
    [Fact]
    public void FromClipboard_OnlyWhiteSpace_IsRefused_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Clipboard.Text = "  \n\t ";

        Assert.False(controller.FromClipboard(null));
        Assert.Equal("a", workbench.Document.Text);
    }

    /// <summary>Clipboard text that does not compile is not imported: the status gives the first error and the open rule stays.</summary>
    [Fact]
    public void FromClipboard_TextWithAnError_ReportsTheErrorAndKeepsTheRule_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Clipboard.Text = "a AND (";

        bool imported = controller.FromClipboard(null);

        Assert.False(imported);
        Assert.Equal("a", workbench.Document.Text);
        Assert.StartsWith("Not imported: ", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>When the open rule has unsaved changes and the author does not agree to drop them, the clipboard is not imported.</summary>
    [Fact]
    public void FromClipboard_DirtyDocumentAndNoConsent_KeepsTheRule_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, ScriptedPrompts prompts) = Create("a");
        workbench.Document.SetText("a AND b");
        services.Clipboard.Text = "c";
        prompts.Discard = false;

        bool imported = controller.FromClipboard(null);

        Assert.False(imported);
        Assert.Equal(1, prompts.DiscardQuestions);
        Assert.Equal("a AND b", workbench.Document.Text);
    }

    /// <summary>Valid clipboard text in a format that the author names replaces the rule and the status names the format and the evidence.</summary>
    [Fact]
    public void FromClipboard_JsonWithTheFormatNamed_ReplacesTheRuleAndNamesTheEvidence_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Clipboard.Text = """{"predicate":"b"}""";

        bool imported = controller.FromClipboard(RuleFormat.Json);

        Assert.True(imported);
        Assert.Equal(RuleFormat.Json, workbench.Document.Format);
        Assert.Contains("the format you named", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A file that is missing is reported with its path and does not replace the rule.</summary>
    [Fact]
    public void FromFile_MissingFile_ReportsThePathAndKeepsTheRule_Test()
    {
        (ImportController controller, _, Workbench workbench, _) = Create("a");
        string path = Path.Combine(Path.GetTempPath(), "truthweaver-missing-" + Guid.NewGuid().ToString("N") + ".rule");

        bool opened = controller.FromFile(path, null);

        Assert.False(opened);
        Assert.Equal("a", workbench.Document.Text);
        Assert.StartsWith($"Cannot open '{path}'", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>The format menu maps its entries to formats, and detection (or an unknown entry) gives no format.</summary>
    [Theory]
    [InlineData(0, null)]
    [InlineData(1, RuleFormat.Text)]
    [InlineData(2, RuleFormat.Json)]
    [InlineData(3, RuleFormat.Yaml)]
    [InlineData(9, null)]
    public void FormatOfChoice_MenuEntry_GivesTheFormat_Test(int choice, RuleFormat? expected)
    {
        Assert.Equal(expected, ImportController.FormatOfChoice(choice));
    }

    /// <summary>Cancelling the source choice, or the format choice, imports nothing and asks nothing more.</summary>
    [Fact]
    public void Import_CancelledAtTheFormatChoice_ImportsNothing_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Clipboard.Text = "b";
        services.Dialogs.Choices.Enqueue(0);
        services.Dialogs.Choices.Enqueue(null);

        bool imported = controller.Import();

        Assert.False(imported);
        Assert.Equal("a", workbench.Document.Text);
    }

    /// <summary>Importing from a file asks for the path, and a cancelled path import nothing.</summary>
    [Fact]
    public void Import_FileSourceWithNoPath_ImportsNothing_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, ScriptedPrompts prompts) = Create("a");
        services.Dialogs.Choices.Enqueue(1);
        services.Dialogs.Choices.Enqueue(0);
        prompts.OpenPath = null;

        Assert.False(controller.Import());
        Assert.Equal("a", workbench.Document.Text);
    }

    /// <summary>Converting text that has errors is refused with a banner, and the text stays.</summary>
    [Fact]
    public void ConvertTo_TextWithErrors_SetsTheBannerAndKeepsTheText_Test()
    {
        (ImportController controller, _, Workbench workbench, _) = Create("a AND");

        bool converted = controller.ConvertTo(RuleFormat.Json);

        Assert.False(converted);
        Assert.Contains("Not converted", controller.Banner, StringComparison.Ordinal);
        Assert.Equal("a AND", workbench.Document.Text);
    }

    /// <summary>Converting to the format that the rule already has says so and changes nothing.</summary>
    [Fact]
    public void ConvertTo_SameFormat_SaysTheRuleIsAlreadyInThatFormat_Test()
    {
        (ImportController controller, _, Workbench workbench, _) = Create("a");

        bool converted = controller.ConvertTo(RuleFormat.Text);

        Assert.True(converted);
        Assert.Contains("already", controller.Banner, StringComparison.Ordinal);
        Assert.False(workbench.Document.CanUndo);
    }

    /// <summary>Cycling backwards from rule text wraps round to YAML.</summary>
    [Fact]
    public void CycleFormat_BackwardsFromRuleText_WrapsToYaml_Test()
    {
        (ImportController controller, _, Workbench workbench, _) = Create("a");

        bool converted = controller.CycleFormat(-1);

        Assert.True(converted);
        Assert.Equal(RuleFormat.Yaml, workbench.Document.Format);
    }

    /// <summary>The Convert dialog lists the targets, and a cancelled dialog or an index outside the list converts nothing.</summary>
    [Fact]
    public void Convert_Cancelled_ConvertsNothing_Test()
    {
        (ImportController controller, ScriptedServices services, Workbench workbench, _) = Create("a");
        services.Dialogs.Choices.Enqueue(null);
        services.Dialogs.Choices.Enqueue(17);

        Assert.False(controller.Convert());
        Assert.False(controller.Convert());

        Assert.Equal(ImportController.ConvertTargets.Count, services.Dialogs.LastItems.Count);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
    }

    private static (
        ImportController Controller,
        ScriptedServices Services,
        Workbench Workbench,
        ScriptedPrompts Prompts
    ) Create(string text)
    {
        ScriptedServices services = new();
        ScriptedPrompts prompts = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, prompts);
        return (new ImportController(workbench, services.Services), services, workbench, prompts);
    }
}
