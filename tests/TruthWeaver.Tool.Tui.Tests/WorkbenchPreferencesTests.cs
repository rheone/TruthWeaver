namespace TruthWeaver.Tool.Tui.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// The choices that the workbench keeps between runs: the theme after a switch and the sizes of the regions. They live in a
/// small user file that holds only those values and that a start-up ignores when it is missing or corrupt.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class WorkbenchPreferencesTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    public WorkbenchPreferencesTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>A switched theme comes back at the next start and wins over the setting.</summary>
    [Fact]
    public void ToggleTheme_WithAPreferencesFile_ComesBackAtTheNextStart_Test()
    {
        string path = this.PathOf("prefs.json");
        Workbench first = Create(ToolSettings.Default);
        first.Preferences = WorkbenchPreferences.Load(path);
        ThemeVariant before = first.Theme;

        first.ToggleTheme();
        WorkbenchPreferences next = WorkbenchPreferences.Load(path);

        ThemeVariant expected = before == ThemeVariant.Light ? ThemeVariant.Dark : ThemeVariant.Light;
        Assert.Equal(expected, next.Theme);
        Assert.Equal(expected, first.Theme);
    }

    /// <summary>The file holds <c>light</c> or <c>dark</c>, never <c>auto</c>, even when the setting is <c>auto</c>.</summary>
    [Fact]
    public void ToggleTheme_WithTheAutoSetting_WritesLightOrDark_Test()
    {
        string path = this.PathOf("prefs.json");
        Workbench workbench = Create(ToolSettings.Default with { Theme = ToolTheme.Auto });
        workbench.Preferences = WorkbenchPreferences.Load(path);

        workbench.ToggleTheme();

        JsonObject written = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
        string theme = written["theme"]!.GetValue<string>();
        Assert.True(theme is "light" or "dark");
    }

    /// <summary>Without a switch, the file has no theme, so the setting still applies.</summary>
    [Fact]
    public void SaveLayout_WithoutAThemeSwitch_LeavesTheThemeOutOfTheFile_Test()
    {
        string path = this.PathOf("prefs.json");
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(path);

        preferences.SaveLayout(new PaneLayout());

        JsonObject written = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
        Assert.False(written.ContainsKey("theme"));
        Assert.Null(WorkbenchPreferences.Load(path).Theme);
    }

    /// <summary>A changed layout comes back at the next start and is applied to a new layout.</summary>
    [Fact]
    public void MoveDivider_WithAPreferencesFile_ComesBackAtTheNextStart_Test()
    {
        string path = this.PathOf("prefs.json");
        using WorkbenchHarness harness = new("a AND b");
        harness.Workbench.Preferences = WorkbenchPreferences.Load(path);

        harness.Window.MoveVerticalDivider(2);
        harness.Window.MoveHorizontalDivider(-1);

        WorkbenchPreferences next = WorkbenchPreferences.Load(path);
        PaneLayout restored = new();
        restored.Restore(next.EditorWidthPercent, next.TopHeightPercent);
        Assert.Equal(harness.Workbench.Layout.EditorWidthPercent, restored.EditorWidthPercent);
        Assert.Equal(harness.Workbench.Layout.TopHeightPercent, restored.TopHeightPercent);
        Assert.NotEqual(new PaneLayout().EditorWidthPercent, restored.EditorWidthPercent);
    }

    /// <summary>The file holds the theme and the layout and nothing else: no rule text and no file path.</summary>
    [Fact]
    public void Save_AfterEditingARule_HoldsOnlyTheThemeAndTheLayout_Test()
    {
        string path = this.PathOf("prefs.json");
        using WorkbenchHarness harness = new("secretTerm AND otherTerm");
        harness.Workbench.Preferences = WorkbenchPreferences.Load(path);
        harness.Workbench.Document.SetText("secretTerm OR otherTerm");

        harness.Workbench.ToggleTheme();
        harness.Window.MoveVerticalDivider(1);

        JsonObject written = Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));
        Assert.Equal(["layout", "theme"], written.Select(p => p.Key).Order(StringComparer.Ordinal));
        JsonObject layout = Assert.IsType<JsonObject>(written["layout"]);
        Assert.Equal(["editorWidthPercent", "topHeightPercent"], layout.Select(p => p.Key).Order(StringComparer.Ordinal));
        string text = File.ReadAllText(path);
        Assert.DoesNotContain("secretTerm", text, StringComparison.Ordinal);
        Assert.DoesNotContain(this.directory, text, StringComparison.Ordinal);
    }

    /// <summary>A file that is not JSON, or not an object, gives the defaults.</summary>
    [Theory]
    [InlineData("this is { not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("")]
    [InlineData("\"theme\"")]
    public void Load_CorruptFile_GivesTheDefaults_Test(string content)
    {
        string path = this.PathOf("prefs.json");
        File.WriteAllText(path, content);

        WorkbenchPreferences preferences = WorkbenchPreferences.Load(path);

        Assert.Null(preferences.Theme);
        Assert.Null(preferences.EditorWidthPercent);
        Assert.Null(preferences.TopHeightPercent);
    }

    /// <summary>A value that is not valid is left out, and the valid values stay.</summary>
    [Fact]
    public void Load_InvalidValues_AreLeftOut_Test()
    {
        string path = this.PathOf("prefs.json");
        File.WriteAllText(path, """{ "theme": "auto", "layout": { "editorWidthPercent": 3, "topHeightPercent": 60 } }""");

        WorkbenchPreferences preferences = WorkbenchPreferences.Load(path);

        Assert.Null(preferences.Theme);
        Assert.Null(preferences.EditorWidthPercent);
        Assert.Equal(60, preferences.TopHeightPercent);
    }

    /// <summary>A missing file gives the defaults.</summary>
    [Fact]
    public void Load_MissingFile_GivesTheDefaults_Test()
    {
        WorkbenchPreferences preferences = WorkbenchPreferences.Load(this.PathOf("missing", "prefs.json"));

        Assert.Null(preferences.Theme);
        Assert.Null(preferences.EditorWidthPercent);
    }

    /// <summary>A file that cannot be written does not stop the switch.</summary>
    [Fact]
    public void ToggleTheme_WhenTheFileCannotBeWritten_StillSwitchesTheTheme_Test()
    {
        // A folder where the file should be makes the write fail.
        string path = this.PathOf("blocked.json");
        _ = Directory.CreateDirectory(path);
        Workbench workbench = Create(ToolSettings.Default);
        workbench.Preferences = WorkbenchPreferences.Load(path);
        ThemeVariant before = workbench.Theme;

        workbench.ToggleTheme();

        Assert.NotEqual(before, workbench.Theme);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private static Workbench Create(ToolSettings settings)
    {
        return new Workbench(RuleDocument.Create("a"), settings, new ScriptedPrompts());
    }

    private string PathOf(params string[] parts)
    {
        return Path.Combine([this.directory, .. parts]);
    }
}
