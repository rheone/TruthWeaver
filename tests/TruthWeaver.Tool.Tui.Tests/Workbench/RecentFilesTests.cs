namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The recent files and the unsaved-changes mark: the list lives in <c>workbench.json</c> as paths only, holds ten files, newest
/// first, shows in the File menu with missing files marked, and the window title carries a mark while the rule has unsaved edits.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class RecentFilesTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Initializes a new instance of the <see cref="RecentFilesTests"/> class and creates the temporary folder of the test.</summary>
    public RecentFilesTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>Files go to the top in the order they were opened, and a file opened again moves up instead of repeating.</summary>
    [Fact]
    public void AddRecent_SeveralFiles_ListsTheNewestFirstWithoutRepeats_Test()
    {
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(this.PathOf("prefs.json"));

        preferences.AddRecent(this.PathOf("a.rule"));
        preferences.AddRecent(this.PathOf("b.rule"));
        preferences.AddRecent(this.PathOf("c.rule"));
        preferences.AddRecent(this.PathOf("a.rule"));

        Assert.Equal([this.PathOf("a.rule"), this.PathOf("c.rule"), this.PathOf("b.rule")], preferences.RecentFiles);
    }

    /// <summary>The list holds ten files: the eleventh pushes out the oldest.</summary>
    [Fact]
    public void AddRecent_MoreThanTenFiles_KeepsTheNewestTen_Test()
    {
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(this.PathOf("prefs.json"));

        for (int i = 1; i <= 12; i++)
        {
            preferences.AddRecent(this.PathOf($"f{i}.rule"));
        }

        Assert.Equal(WorkbenchPreferences.MaxRecentFiles, preferences.RecentFiles.Count);
        Assert.Equal(this.PathOf("f12.rule"), preferences.RecentFiles[0]);
        Assert.Equal(this.PathOf("f3.rule"), preferences.RecentFiles[^1]);
    }

    /// <summary>The file keeps the list in order, as paths only, and the next start reads it back.</summary>
    [Fact]
    public void AddRecent_Written_HoldsOnlyPathsAndComesBackInOrder_Test()
    {
        string file = this.PathOf("prefs.json");
        WorkbenchPreferences first = WorkbenchPreferences.Load(file);
        first.AddRecent(this.PathOf("a.rule"));
        first.AddRecent(this.PathOf("b.rule"));

        JsonObject written = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(file)));
        WorkbenchPreferences next = WorkbenchPreferences.Load(file);

        JsonArray stored = Assert.IsType<JsonArray>(written["recentFiles"]);
        Assert.Equal([this.PathOf("b.rule"), this.PathOf("a.rule")], stored.Select(n => n!.GetValue<string>()));
        Assert.Equal(first.RecentFiles, next.RecentFiles);
        Assert.Equal(["recentFiles"], written.Select(p => p.Key));
    }

    /// <summary>A list in the file that is too long, repeats a path or holds something that is no path is trimmed when it is read.</summary>
    [Fact]
    public void Load_MessyList_KeepsTenDistinctPaths_Test()
    {
        string file = this.PathOf("prefs.json");
        JsonArray list = [];
        for (int i = 0; i < 14; i++)
        {
            list.Add(this.PathOf($"f{i % 12}.rule"));
        }

        list.Add(5);
        File.WriteAllText(file, new JsonObject { ["recentFiles"] = list }.ToJsonString());

        WorkbenchPreferences preferences = WorkbenchPreferences.Load(file);

        Assert.Equal(10, preferences.RecentFiles.Count);
        Assert.Equal(preferences.RecentFiles.Distinct(), preferences.RecentFiles);
        Assert.Equal(this.PathOf("f0.rule"), preferences.RecentFiles[0]);
    }

    /// <summary>A path that is taken out of the list is gone from the file too.</summary>
    [Fact]
    public void RemoveRecent_ListedPath_TakesItOutOfTheFile_Test()
    {
        string file = this.PathOf("prefs.json");
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(file);
        preferences.AddRecent(this.PathOf("a.rule"));
        preferences.AddRecent(this.PathOf("b.rule"));

        bool removed = preferences.RemoveRecent(this.PathOf("a.rule"));

        Assert.True(removed);
        Assert.Equal([this.PathOf("b.rule")], WorkbenchPreferences.Load(file).RecentFiles);
        Assert.False(preferences.RemoveRecent(this.PathOf("never.rule")));
    }

    /// <summary>Opening a file puts it at the top of the list, and a file that does not open stays out.</summary>
    [Fact]
    public void OpenPath_File_AddsItToTheRecentFiles_Test()
    {
        (Workbench workbench, _) = this.Create();
        string first = this.Write("one.rule", "a AND b");
        string second = this.Write("two.rule", "c OR d");

        Assert.True(workbench.OpenPath(first));
        Assert.True(workbench.OpenPath(second));
        Assert.False(workbench.OpenPath(this.PathOf("absent.rule")));

        Assert.Equal([second, first], workbench.RecentFiles);
    }

    /// <summary>A file of the list opens in place of the rule.</summary>
    [Fact]
    public void OpenRecent_ExistingFile_OpensIt_Test()
    {
        (Workbench workbench, _) = this.Create();
        string file = this.Write("one.rule", "a AND b");
        workbench.RememberRecent(file);

        bool opened = workbench.OpenRecent(file);

        Assert.True(opened);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.Equal(file, workbench.RecentFiles[0]);
    }

    /// <summary>A missing file is not opened, goes out of the list, and the open rule and its unsaved edits stay without a question.</summary>
    [Fact]
    public void OpenRecent_MissingFile_RemovesItAndKeepsTheRule_Test()
    {
        (Workbench workbench, ScriptedPrompts prompts) = this.Create();
        string gone = this.PathOf("gone.rule");
        workbench.RememberRecent(gone);
        workbench.EditText("x AND y");

        bool opened = workbench.OpenRecent(gone);

        Assert.False(opened);
        Assert.Empty(workbench.RecentFiles);
        Assert.Equal("x AND y", workbench.Document.Text);
        Assert.Equal(0, prompts.DiscardQuestions);
        Assert.Contains("no longer exists", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Unsaved edits need the usual consent before a recent file replaces the rule.</summary>
    [Fact]
    public void OpenRecent_UnsavedChangesDeclined_KeepsTheRule_Test()
    {
        (Workbench workbench, ScriptedPrompts prompts) = this.Create();
        string file = this.Write("one.rule", "a AND b");
        workbench.RememberRecent(file);
        workbench.EditText("x AND y");

        bool opened = workbench.OpenRecent(file);

        Assert.False(opened);
        Assert.Equal(1, prompts.DiscardQuestions);
        Assert.Equal("x AND y", workbench.Document.Text);
    }

    /// <summary>With no preferences file the list is empty and nothing is written.</summary>
    [Fact]
    public void RememberRecent_WithoutPreferences_KeepsNothing_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());

        workbench.RememberRecent(this.PathOf("a.rule"));

        Assert.Empty(workbench.RecentFiles);
        Assert.False(workbench.RemoveRecent(this.PathOf("a.rule")));
    }

    /// <summary>The title has no mark for a rule that is as saved, gets the mark after an edit, and loses it after a save.</summary>
    [Fact]
    public void TitleOf_EditThenSave_ShowsAndClearsTheUnsavedMark_Test()
    {
        (Workbench workbench, ScriptedPrompts prompts) = this.Create();
        string file = this.Write("one.rule", "a AND b");
        Assert.True(workbench.OpenPath(file));
        Assert.Equal("one.rule - TruthWeaver workbench", WorkbenchWindow.TitleOf(workbench));

        workbench.EditText("a OR b");
        string edited = WorkbenchWindow.TitleOf(workbench);
        prompts.SavePath = file;
        Assert.True(workbench.Save());

        Assert.Equal("* one.rule - TruthWeaver workbench", edited);
        Assert.Equal("one.rule - TruthWeaver workbench", WorkbenchWindow.TitleOf(workbench));
    }

    /// <summary>The window shows the mark in its title after a typed edit and removes it after the save.</summary>
    [Fact]
    public void Window_Edit_UpdatesTheTitle_Test()
    {
        (Workbench workbench, ScriptedPrompts prompts) = this.Create();
        prompts.SavePath = this.PathOf("new.rule");
        using WorkbenchWindow window = new(workbench);
        Assert.StartsWith("(new rule) - ", window.Title, StringComparison.Ordinal);

        workbench.EditText("a AND b");
        string edited = window.Title;
        Assert.True(workbench.Save());

        Assert.StartsWith("* (new rule)", edited, StringComparison.Ordinal);
        Assert.Equal("new.rule - TruthWeaver workbench", window.Title);
    }

    /// <summary>The File menu lists the recent files under Open recent in order, and marks a missing one.</summary>
    [Fact]
    public void FileMenu_Opened_ListsTheRecentFilesAndMarksTheMissingOne_Test()
    {
        string present = this.Write("here.rule", "a");
        string gone = this.PathOf("gone.rule");
        (Workbench workbench, _) = this.Create();
        workbench.RememberRecent(gone);
        workbench.RememberRecent(present);
        using MenuSession session = new(workbench);

        session.Press(Key.F.WithAlt);
        string[] titles =
        [
            .. session
                .Window.MenuBar.GetMenuItemsWith(i => i.Title.Length > 2 && char.IsDigit(i.Title[0]) && i.Title[1] == '.')
                .Select(i => i.Title),
        ];

        Assert.Equal(2, titles.Length);
        Assert.Equal(WorkbenchMenus.RecentEntries(workbench).Select(e => e.Title), titles);
        Assert.StartsWith("1. ", titles[0], StringComparison.Ordinal);
        Assert.DoesNotContain("missing", titles[0], StringComparison.Ordinal);
        Assert.StartsWith("2. ", titles[1], StringComparison.Ordinal);
        Assert.Contains("missing", titles[1], StringComparison.Ordinal);
        Assert.Equal(workbench.RecentFiles, WorkbenchMenus.RecentEntries(workbench).Select(e => e.Path));
        Assert.Equal([false, true], WorkbenchMenus.RecentEntries(workbench).Select(e => e.Missing));
    }

    /// <summary>A path longer than the menu shows keeps its end.</summary>
    [Fact]
    public void RecentEntries_LongPath_KeepsTheEnd_Test()
    {
        (Workbench workbench, _) = this.Create();
        string deep = Path.Combine(this.directory, new string('d', 80), "tail.rule");
        workbench.RememberRecent(deep);

        RecentEntry entry = Assert.Single(WorkbenchMenus.RecentEntries(workbench));

        Assert.StartsWith("1. ...", entry.Title, StringComparison.Ordinal);
        Assert.Contains("tail.rule", entry.Title, StringComparison.Ordinal);
        Assert.True(entry.Title.Length < deep.Length);
    }

    /// <summary>Removes the temporary folder.</summary>
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private string PathOf(string name)
    {
        return Path.Combine(this.directory, name);
    }

    private string Write(string name, string text)
    {
        string file = this.PathOf(name);
        File.WriteAllText(file, text);
        return file;
    }

    private (Workbench Workbench, ScriptedPrompts Prompts) Create()
    {
        ScriptedPrompts prompts = new();
        Workbench workbench = new(RuleDocument.Create(string.Empty), ToolSettings.Default, prompts)
        {
            Preferences = WorkbenchPreferences.Load(this.PathOf("prefs.json")),
        };
        return (workbench, prompts);
    }

    /// <summary>A headless run of the application with the workbench window, for the keys that open a menu.</summary>
    private sealed class MenuSession : IDisposable
    {
        private readonly IApplication app;
        private readonly SessionToken? token;

        public MenuSession(Workbench workbench)
        {
            this.app = Application.Create();
            this.app.Init(DriverRegistry.Names.ANSI);
            this.app.Driver!.SetScreenSize(100, 30);
            this.Window = new WorkbenchWindow(workbench);
            this.token = this.app.Begin(this.Window);
            this.Window.FocusRegion(PaneSlot.Editor);
            this.app.LayoutAndDraw(true);
        }

        public WorkbenchWindow Window { get; }

        public void Press(Key key)
        {
            _ = this.app.Keyboard.RaiseKeyDownEvent(key);
            this.app.LayoutAndDraw(true);
        }

        public void Dispose()
        {
            if (this.token is not null)
            {
                this.app.End(this.token);
            }

            this.Window.Dispose();
            this.app.Dispose();
        }
    }
}
