namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Validation;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Hosting;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Import, conversion, export and validation in the workbench, tested through the UI-free layer.</summary>
public sealed class ImportExportTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("tw-ie-").FullName;

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.folder, recursive: true);
    }

    /// <summary>A valid expression on the clipboard replaces the rule in each of the three forms.</summary>
    [Theory]
    [InlineData("a AND b", RuleFormat.Text)]
    [InlineData("{\"op\":\"and\",\"operands\":[{\"predicate\":\"a\"},{\"predicate\":\"b\"}]}", RuleFormat.Json)]
    [InlineData("op: and\noperands:\n  - predicate: a\n  - predicate: b\n", RuleFormat.Yaml)]
    public void FromClipboard_ValidExpression_ReplacesTheRule_Test(string text, RuleFormat expected)
    {
        (ImportController controller, Workbench workbench, ScriptedServices services, _) = Create("x");
        services.Clipboard.Text = text;

        bool imported = controller.FromClipboard(null);

        Assert.True(imported);
        Assert.Equal(expected, workbench.Document.Format);
        Assert.Equal("a AND b", workbench.Document.Rule!.CanonicalText);
    }

    /// <summary>A named format wins over detection: rule text read as YAML is not a rule, so it is refused.</summary>
    [Fact]
    public void FromClipboard_NamedFormat_OverridesDetection_Test()
    {
        (ImportController controller, Workbench workbench, ScriptedServices services, _) = Create("x");
        services.Clipboard.Text = "a AND b";

        Assert.False(controller.FromClipboard(RuleFormat.Yaml));
        Assert.True(controller.FromClipboard(RuleFormat.Text));
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
    }

    /// <summary>Text that does not compile is refused, and the open rule stays.</summary>
    [Fact]
    public void FromClipboard_InvalidExpression_KeepsTheOpenRule_Test()
    {
        (ImportController controller, Workbench workbench, ScriptedServices services, _) = Create("x AND y");
        services.Clipboard.Text = "a AND (";

        bool imported = controller.FromClipboard(null);

        Assert.False(imported);
        Assert.Equal("x AND y", workbench.Document.Text);
        Assert.StartsWith("Not imported", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>An empty or missing clipboard says so.</summary>
    [Fact]
    public void FromClipboard_NoText_SaysSo_Test()
    {
        (ImportController controller, Workbench workbench, ScriptedServices services, _) = Create("x");
        services.Clipboard.Available = false;

        Assert.False(controller.FromClipboard(null));
        Assert.Contains("clipboard", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Unsaved changes need consent before the clipboard replaces the rule.</summary>
    [Fact]
    public void FromClipboard_UnsavedChangesRefused_KeepsTheRule_Test()
    {
        (ImportController controller, Workbench workbench, ScriptedServices services, ScriptedPrompts prompts) = Create("x");
        workbench.Document.SetText("x AND y");
        services.Clipboard.Text = "a AND b";
        prompts.Discard = false;

        Assert.False(controller.FromClipboard(null));
        Assert.Equal("x AND y", workbench.Document.Text);
    }

    /// <summary>A file opens with the format the author names.</summary>
    [Fact]
    public void FromFile_NamedFormat_OpensWithThatFormat_Test()
    {
        (ImportController controller, Workbench workbench, _, _) = Create("x");
        string path = Path.Combine(this.folder, "rule.dat");
        File.WriteAllText(path, "a AND b\n");

        bool opened = controller.FromFile(path, RuleFormat.Text);

        Assert.True(opened);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
        Assert.Equal("a AND b", workbench.Document.Rule!.CanonicalText);
    }

    /// <summary>Conversion keeps the compiled tree equal and the notes intact, for every pair.</summary>
    [Theory]
    [InlineData(RuleFormat.Json)]
    [InlineData(RuleFormat.Yaml)]
    public void ConvertTo_RuleWithNotes_KeepsTheTreeAndNotes_Test(RuleFormat target)
    {
        (ImportController controller, Workbench workbench, _, _) = Create("/* Active. */ (/* First. */ a AND b)");
        string before = workbench.Document.Rule!.PrintJson(includeNotes: true);

        bool converted = controller.ConvertTo(target);

        Assert.True(converted);
        Assert.Equal(target, workbench.Document.Format);
        Assert.Equal(before, workbench.Document.Rule.PrintJson(includeNotes: true));
        Assert.Contains("the tree and the notes are the same", controller.Banner, StringComparison.Ordinal);
    }

    /// <summary>The banner counts the YAML comments that a conversion cannot keep.</summary>
    [Fact]
    public void ConvertTo_YamlWithComments_BannerStatesTheDroppedComments_Test()
    {
        (ImportController controller, Workbench workbench, _, _) = Create(
            "op: and  # all\noperands:\n  # first\n  - predicate: a\n  - predicate: b\n",
            RuleFormat.Yaml
        );

        controller.ConvertTo(RuleFormat.Json);

        Assert.Contains("2 YAML comments cannot be kept", controller.Banner, StringComparison.Ordinal);
        Assert.Equal(RuleFormat.Json, workbench.Document.Format);
    }

    /// <summary>Text with errors is not converted and the banner says why.</summary>
    [Fact]
    public void ConvertTo_TextWithErrors_SaysNotConverted_Test()
    {
        (ImportController controller, Workbench workbench, _, _) = Create("a AND b");
        workbench.Document.SetText("a AND");

        Assert.False(controller.ConvertTo(RuleFormat.Json));
        Assert.StartsWith("Not converted", controller.Banner, StringComparison.Ordinal);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
    }

    /// <summary>The format key walks rule text, JSON, YAML and back.</summary>
    [Fact]
    public void CycleFormat_Forward_WalksTextJsonYamlText_Test()
    {
        (ImportController controller, Workbench workbench, _, _) = Create("a AND b");

        controller.CycleFormat(1);
        Assert.Equal(RuleFormat.Json, workbench.Document.Format);
        controller.CycleFormat(1);
        Assert.Equal(RuleFormat.Yaml, workbench.Document.Format);
        controller.CycleFormat(1);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
    }

    /// <summary>Every artifact of the Core catalog is an entry of the menu, and its text equals the Core output.</summary>
    [Fact]
    public void Render_EveryCatalogArtifact_EqualsTheCoreOutput_Test()
    {
        (ExportController controller, Workbench workbench, _, _) = CreateExport("a AND (b OR NOT c)");
        LoadedRule rule = RuleLoader
            .LoadString(workbench.Document.Text, "<editor>", new RuleLoadOptions { Format = RuleFormat.Text })
            .Value!;
        IReadOnlyList<ExportEntry> entries = controller.Entries();

        foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
        {
            Assert.Contains(entries, e => e.Artifact == artifact);
            ToolResult<string> expected = ArtifactExporter.Render(rule, artifact);
            ToolResult<string> actual = controller.Render(artifact);

            Assert.True(actual.Succeeded, artifact.Id);
            Assert.Equal(expected.Value, actual.Value);
        }
    }

    /// <summary>Copy puts an artifact on the clipboard, and save writes it to the chosen file.</summary>
    [Fact]
    public void CopyAndSaveArtifact_Json_PutTheCoreTextWhereAsked_Test()
    {
        (ExportController controller, Workbench workbench, ScriptedServices services, ScriptedPrompts prompts) = CreateExport(
            "a AND b"
        );
        ArtifactDefinition json = ArtifactCatalog.All.First(a => a.Id == "json");
        string path = Path.Combine(this.folder, "out.json");
        prompts.SavePath = path;

        Assert.True(controller.CopyArtifact(json));
        Assert.True(controller.SaveArtifact(json));

        string expected = controller.Render(json).Value!;
        Assert.Equal(expected, services.Clipboard.Text);
        Assert.Equal(OutputWriter.Normalize(expected), File.ReadAllText(path));
        Assert.NotNull(workbench);
    }

    /// <summary>A truth table over the cap is refused with a hint, and raising the cap makes it with a warning.</summary>
    [Fact]
    public void CopyArtifact_TruthTableOverTheCap_RefusesThenAllowsAfterRaisingTheCap_Test()
    {
        (ExportController controller, Workbench workbench, ScriptedServices services, _) = CreateExport(
            "p1 AND p2 AND p3 AND p4 AND p5 AND p6 AND p7 AND p8 AND p9"
        );
        ArtifactDefinition table = ArtifactCatalog.All.First(a => a.Id == "truth-table-csv");

        Assert.False(controller.CopyArtifact(table));
        Assert.Contains("F5", workbench.Status, StringComparison.Ordinal);
        Assert.Null(services.Clipboard.Text);

        controller.AllowLargerTruthTable = true;

        Assert.True(controller.CopyArtifact(table));
        Assert.NotNull(services.Clipboard.Text);
    }

    /// <summary>Export all writes every artifact and a manifest to the folder, as Core does.</summary>
    [Fact]
    public void ExportAll_Folder_WritesTheFilesAndTheManifest_Test()
    {
        (ExportController controller, Workbench workbench, ScriptedServices services, _) = CreateExport("a AND b");
        string target = Path.Combine(this.folder, "all");
        services.Dialogs.Folder = target;
        controller.ImageSearchPath = this.folder;

        bool exported = controller.ExportAll();

        Assert.True(exported);
        Assert.True(File.Exists(Path.Combine(target, ExportManifest.FileName)));
        LoadedRule rule = RuleLoader.LoadString("a AND b").Value!;
        int expected = ArtifactExporter.RenderAll(rule).Value!.Files.Count;
        Assert.Equal(expected, Directory.GetFiles(target).Length);
        Assert.Contains("No images", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A missing image program is a status message that names the program, with no dialog and no file.</summary>
    [Fact]
    public void SaveImage_ProgramMissing_ReportsWhatToInstall_Test()
    {
        (ExportController controller, Workbench workbench, ScriptedServices services, ScriptedPrompts prompts) = CreateExport(
            "a AND b"
        );
        controller.ImageSearchPath = this.folder;
        string path = Path.Combine(this.folder, "x.svg");
        prompts.SavePath = path;

        bool saved = controller.SaveImage(DiagramLanguage.Dot, ImageFormat.Svg);

        Assert.False(saved);
        Assert.Contains("dot", workbench.Status, StringComparison.Ordinal);
        Assert.False(File.Exists(path));
        Assert.Empty(services.Dialogs.Confirmations);
    }

    /// <summary>A Copy request from the export dialog puts the text of the artifact on the clipboard.</summary>
    [Fact]
    public void ShowMenu_CopyRequest_PutsTheArtifactOnTheClipboard_Test()
    {
        (ExportController controller, _, ScriptedServices services, _) = CreateExport("a AND b");
        ExportEntry entry = controller.Entries().First(e => e.Artifact?.Id == "mermaid");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.Copy, string.Empty, false));

        bool done = controller.ShowMenu();

        Assert.True(done);
        Assert.Equal(controller.Render(entry.Artifact!).Value, services.Clipboard.Text);
        Assert.Equal(ExportGroups.Diagram, entry.Group);
    }

    /// <summary>A To File request writes the path the dialog gave and opens no save dialog.</summary>
    [Fact]
    public void ShowMenu_ToFileRequest_WritesThePathWithoutASecondDialog_Test()
    {
        (ExportController controller, _, ScriptedServices services, ScriptedPrompts prompts) = CreateExport("a AND b");
        ExportEntry entry = controller.Entries().First(e => e.Artifact?.Id == "json");
        string path = Path.Combine(this.folder, "chosen.json");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.ToFile, path, false));
        prompts.SavePath = Path.Combine(this.folder, "never.json");

        bool done = controller.ShowMenu();

        Assert.True(done);
        Assert.Equal(OutputWriter.Normalize(controller.Render(entry.Artifact!).Value!), File.ReadAllText(path));
        Assert.False(File.Exists(prompts.SavePath));
        Assert.Empty(services.Dialogs.Confirmations);
    }

    /// <summary>A path that exists is replaced only after a yes.</summary>
    [Fact]
    public void ShowMenu_ToFileRequestOverAnExistingFile_NeedsAConfirmation_Test()
    {
        (ExportController controller, _, ScriptedServices services, _) = CreateExport("a AND b");
        ExportEntry entry = controller.Entries().First(e => e.Artifact?.Id == "json");
        string path = Path.Combine(this.folder, "old.json");
        File.WriteAllText(path, "old");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.ToFile, path, false));

        Assert.False(controller.ShowMenu());
        Assert.Equal("old", File.ReadAllText(path));

        services.Dialogs.Agree = true;
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.ToFile, path, false));

        Assert.True(controller.ShowMenu());
        Assert.NotEqual("old", File.ReadAllText(path));
    }

    /// <summary>A request with no entry writes every artifact to the folder that the dialog gave.</summary>
    [Fact]
    public void ShowMenu_FolderRequest_WritesEveryArtifactWithoutAskingForAFolder_Test()
    {
        (ExportController controller, _, ScriptedServices services, _) = CreateExport("a AND b");
        controller.ImageSearchPath = this.folder;
        string target = Path.Combine(this.folder, "everything");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(null, ExportAction.ToFile, target, false));

        bool done = controller.ShowMenu();

        Assert.True(done);
        Assert.True(File.Exists(Path.Combine(target, ExportManifest.FileName)));
    }

    /// <summary>The larger-table option of the dialog is kept, starts the dialog the next time, and lets a big table through.</summary>
    [Fact]
    public void ShowMenu_LargerTableOption_IsKeptAndAllowsTheTable_Test()
    {
        (ExportController controller, _, ScriptedServices services, _) = CreateExport(
            "p1 AND p2 AND p3 AND p4 AND p5 AND p6 AND p7 AND p8 AND p9"
        );
        ExportEntry entry = controller.Entries().First(e => e.Artifact?.Id == "truth-table-csv");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.Copy, string.Empty, true));

        bool done = controller.ShowMenu();
        _ = controller.ShowMenu();

        Assert.True(done);
        Assert.True(controller.AllowLargerTruthTable);
        Assert.True(services.Dialogs.LastExportModel!.AllowLargerTruthTable);
        Assert.NotNull(services.Clipboard.Text);
    }

    /// <summary>Cancel in the dialog exports nothing and changes nothing.</summary>
    [Fact]
    public void ShowMenu_Cancelled_ChangesNothing_Test()
    {
        (ExportController controller, Workbench workbench, ScriptedServices services, _) = CreateExport("a AND b");

        bool done = controller.ShowMenu();

        Assert.False(done);
        Assert.False(controller.AllowLargerTruthTable);
        Assert.Null(services.Clipboard.Text);
        Assert.Equal(string.Empty, workbench.Status);
    }

    /// <summary>The entries carry the heading, the five truth-table formats and the file name the dialog uses.</summary>
    [Fact]
    public void Entries_Catalog_AreGroupedUnderHeadingsWithFileNames_Test()
    {
        (ExportController controller, _, _, _) = CreateExport("a AND b");

        IReadOnlyList<ExportEntry> entries = controller.Entries();

        Assert.Equal(5, entries.Count(e => e.Group == ExportGroups.TruthTable));
        Assert.Equal(4, entries.Count(e => e.Group == ExportGroups.Images));
        Assert.All(entries, e => Assert.NotEqual(string.Empty, e.Group));
        Assert.All(entries, e => Assert.NotEqual(string.Empty, e.FileName));
        Assert.Equal(ArtifactCatalog.All.Count + 4, entries.Count);
    }

    /// <summary>Validation shows every level and the statistics.</summary>
    [Fact]
    public void Run_ValidRule_ShowsEveryLevelAndTheStatistics_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a AND (b OR c)"), ToolSettings.Default, new ScriptedPrompts());
        ValidationController controller = new(workbench);

        ValidationReport report = controller.Run();

        Assert.Equal(6, report.Levels.Count);
        string text = string.Join("\n", controller.Lines.Select(l => l.Text));
        foreach (
            ValidationLevels level in new[]
            {
                ValidationLevels.Compile,
                ValidationLevels.TreeSchema,
                ValidationLevels.Limits,
                ValidationLevels.PredicateSchema,
                ValidationLevels.Analysis,
                ValidationLevels.ConversionCheck,
            }
        )
        {
            Assert.Contains(level.ToString(), text, StringComparison.Ordinal);
        }

        Assert.Contains("Statistics:", text, StringComparison.Ordinal);
        Assert.StartsWith("Validation passed", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Selecting a diagnostic line moves the editor cursor to its span.</summary>
    [Fact]
    public void Select_DiagnosticLine_RequestsItsSpan_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a AND ("), ToolSettings.Default, new ScriptedPrompts());
        ValidationController controller = new(workbench);
        SourceSpan? requested = null;
        workbench.SpanRequested += (_, span) => requested = span;
        controller.Run();

        ValidationLine line = controller.Lines.First(l => l.Span is not null);

        Assert.True(controller.Select(line));
        Assert.Equal(line.Span, requested);
        Assert.False(controller.Select(controller.Lines[0]));
    }

    /// <summary>Strict mode changes the outcome for a rule with a warning.</summary>
    [Fact]
    public void ToggleStrict_RuleWithAWarning_FailsOnlyInStrictMode_Test()
    {
        Workbench workbench = new(RuleDocument.Create("True OR a"), ToolSettings.Default, new ScriptedPrompts());
        ValidationController controller = new(workbench);
        ValidationReport report = controller.Run();
        Assert.Contains(report.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
        Assert.StartsWith("Validation passed", workbench.Status, StringComparison.Ordinal);

        controller.ToggleStrict();

        Assert.StartsWith("Validation failed (strict)", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A dash reads the rule from standard input, with an optional format.</summary>
    [Fact]
    public void Prepare_DashWithStandardInput_OpensTheRule_Test()
    {
        using StringReader input = new("{\"predicate\":\"a\"}");

        TuiStartupResult result = TuiStartup.Prepare(["-", "--format", "json"], this.folder, input);

        Assert.NotNull(result.Document);
        Assert.Equal(RuleFormat.Json, result.Document.Format);
    }

    /// <summary>A dash with no redirected input is unusable and says so.</summary>
    [Fact]
    public void Prepare_DashWithoutStandardInput_ExitsAsUnusable_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare(["-"], this.folder);

        Assert.Null(result.Document);
        Assert.Equal(ToolExitCode.Unusable, result.ExitCode);
    }

    /// <summary>An unknown format name is unusable.</summary>
    [Fact]
    public void Prepare_UnknownFormat_ExitsAsUnusable_Test()
    {
        TuiStartupResult result = TuiStartup.Prepare(["--format", "xml"], this.folder);

        Assert.Equal(ToolExitCode.Unusable, result.ExitCode);
    }

    private static (
        ImportController Controller,
        Workbench Workbench,
        ScriptedServices Services,
        ScriptedPrompts Prompts
    ) Create(string text, RuleFormat format = RuleFormat.Text)
    {
        ScriptedPrompts prompts = new() { Discard = true };
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text, format), ToolSettings.Default, prompts);
        return (new ImportController(workbench, services.Services), workbench, services, prompts);
    }

    private static (
        ExportController Controller,
        Workbench Workbench,
        ScriptedServices Services,
        ScriptedPrompts Prompts
    ) CreateExport(string text)
    {
        ScriptedPrompts prompts = new();
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, prompts);
        return (new ExportController(workbench, services.Services), workbench, services, prompts);
    }
}
