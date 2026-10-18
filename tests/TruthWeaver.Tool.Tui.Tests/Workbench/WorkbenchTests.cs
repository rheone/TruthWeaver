namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The shared workbench state and its actions, with scripted answers in place of dialogs.</summary>
public sealed class WorkbenchTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));
    private readonly ScriptedPrompts prompts = new();

    /// <summary>Initializes a new instance of the <see cref="WorkbenchTests"/> class.</summary>
    public WorkbenchTests()
    {
        Directory.CreateDirectory(this.directory);
    }

    /// <summary>Save on a new document asks for a path, and the extension selects the format.</summary>
    [Fact]
    public void Save_NewDocument_AsksForAPathAndConvertsByExtension_Test()
    {
        Workbench workbench = this.Create("a AND b");
        this.prompts.SavePath = Path.Combine(this.directory, "rule.yaml");

        bool saved = workbench.Save();

        Assert.True(saved);
        Assert.Equal(RuleFormat.Yaml, workbench.Document.Format);
        Assert.True(File.Exists(this.prompts.SavePath));
        Assert.Contains("Saved", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A cancelled save-as writes nothing.</summary>
    [Fact]
    public void SaveAs_Cancelled_WritesNothing_Test()
    {
        Workbench workbench = this.Create("a");

        bool saved = workbench.SaveAs();

        Assert.False(saved);
        Assert.Empty(Directory.EnumerateFiles(this.directory));
    }

    /// <summary>Opening another file with unsaved changes asks first, and a no keeps the document.</summary>
    [Fact]
    public void Open_UnsavedChangesDeclined_KeepsTheDocument_Test()
    {
        Workbench workbench = this.Create("a");
        workbench.Document.SetText("a AND b");
        this.prompts.OpenPath = this.Write("other.rule", "c");

        bool opened = workbench.Open();

        Assert.False(opened);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Equal(1, this.prompts.DiscardQuestions);
    }

    /// <summary>Opening a file replaces the document and raises the change event, so every pane follows.</summary>
    [Fact]
    public void Open_File_ReplacesTheDocumentAndRaisesDocumentChanged_Test()
    {
        Workbench workbench = this.Create("a");
        this.prompts.OpenPath = this.Write("other.json", """{ "predicate": "c" }""");
        int raised = 0;
        workbench.DocumentChanged += (_, _) => raised++;

        bool opened = workbench.Open();

        Assert.True(opened);
        Assert.Equal(RuleFormat.Json, workbench.Document.Format);
        Assert.Equal(1, raised);
    }

    /// <summary>After an open, edits of the new document still raise the change event of the workbench.</summary>
    [Fact]
    public void OpenPath_ThenEdit_RaisesDocumentChangedForTheNewDocument_Test()
    {
        Workbench workbench = this.Create("a");
        RuleDocument first = workbench.Document;
        workbench.OpenPath(this.Write("other.rule", "c"));
        int raised = 0;
        workbench.DocumentChanged += (_, _) => raised++;

        first.SetText("ignored");
        workbench.Document.SetText("c AND d");

        Assert.Equal(1, raised);
    }

    /// <summary>A file that cannot be opened leaves the document and says why on the status line.</summary>
    [Fact]
    public void OpenPath_MissingFile_ReportsOnTheStatusLine_Test()
    {
        Workbench workbench = this.Create("a");

        bool opened = workbench.OpenPath(Path.Combine(this.directory, "missing.rule"));

        Assert.False(opened);
        Assert.Equal("a", workbench.Document.Text);
        Assert.StartsWith(ToolDiagnosticCodes.InputUnreadable, workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Formatting text with errors changes nothing and says why.</summary>
    [Fact]
    public void Format_TextWithErrors_ChangesNothingAndSaysWhy_Test()
    {
        Workbench workbench = this.Create("a AND");

        workbench.Format(pretty: false);

        Assert.Equal("a AND", workbench.Document.Text);
        Assert.StartsWith("Not formatted", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Undo with no history says so.</summary>
    [Fact]
    public void Undo_NoHistory_SaysSo_Test()
    {
        Workbench workbench = this.Create("a");

        workbench.Undo();

        Assert.Equal("Nothing to undo.", workbench.Status);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, this.prompts);
    }

    private string Write(string name, string text)
    {
        string path = Path.Combine(this.directory, name);
        File.WriteAllText(path, text);
        return path;
    }
}
