namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The export dialog and the export keys: what they ask the library for and what they say when it refuses.</summary>
public sealed class ExportControllerTests
{
    /// <summary>A clipboard that cannot be written to keeps the artifact out of it and points to a file.</summary>
    [Fact]
    public void CopyArtifact_ClipboardNotAvailable_SaysToSaveToAFile_Test()
    {
        (ExportController controller, ScriptedServices services, Workbench workbench, _) = Create("a AND b");
        services.Clipboard.Available = false;

        bool copied = controller.CopyArtifact(Artifact("rule-text"));

        Assert.False(copied);
        Assert.Contains("Save to a file instead", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A rule text that does not compile cannot be exported; the status gives the first error and the clipboard stays empty.</summary>
    [Fact]
    public void CopyArtifact_TextWithAnError_ReportsAndLeavesTheClipboardAlone_Test()
    {
        (ExportController controller, ScriptedServices services, Workbench workbench, _) = Create("a AND");

        bool copied = controller.CopyArtifact(Artifact("rule-text"));

        Assert.False(copied);
        Assert.Null(services.Clipboard.Text);
        Assert.StartsWith("Cannot make rule-text: ", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A truth table over the term cap is refused, and the status says how to raise the cap.</summary>
    [Fact]
    public void CopyArtifact_TruthTableOverTheCap_SaysHowToRaiseTheCap_Test()
    {
        string rule = string.Join(" AND ", Enumerable.Range(0, 40).Select(i => "t" + i));
        (ExportController controller, _, Workbench workbench, _) = Create(rule);
        ArtifactDefinition table = ArtifactCatalog.All.First(a => a.Id.StartsWith("truth-table", StringComparison.Ordinal));

        bool copied = controller.CopyArtifact(table);

        Assert.False(copied);
        Assert.Contains("larger truth tables option", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>With the larger-table option on, the same refusal does not add the advice again.</summary>
    [Fact]
    public void CopyArtifact_TruthTableOverTheCapWithTheOptionOn_DoesNotAdviseTheOption_Test()
    {
        string rule = string.Join(" AND ", Enumerable.Range(0, 40).Select(i => "t" + i));
        (ExportController controller, _, Workbench workbench, _) = Create(rule);
        controller.AllowLargerTruthTable = true;
        ArtifactDefinition table = ArtifactCatalog.All.First(a => a.Id.StartsWith("truth-table", StringComparison.Ordinal));

        controller.CopyArtifact(table);

        Assert.DoesNotContain("larger truth tables option", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Saving to a file that exists asks first, and a refusal leaves the file as it was.</summary>
    [Fact]
    public void SaveArtifact_ExistingFileAndNoConsent_LeavesTheFile_Test()
    {
        (ExportController controller, ScriptedServices services, _, _) = Create("a");
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "keep me");
            services.Dialogs.Agree = false;

            bool saved = controller.SaveArtifact(Artifact("rule-text"), path);

            Assert.False(saved);
            Assert.Equal("keep me", File.ReadAllText(path));
            Assert.Single(services.Dialogs.Confirmations);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>With no path, a cancelled save prompt saves nothing and the status does not claim a save.</summary>
    [Fact]
    public void SaveArtifact_NoPathAndCancelledPrompt_SavesNothing_Test()
    {
        (ExportController controller, _, Workbench workbench, ScriptedPrompts prompts) = Create("a");
        prompts.SavePath = null;

        bool saved = controller.SaveArtifact(Artifact("rule-text"));

        Assert.False(saved);
        Assert.DoesNotContain("Saved", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A path that cannot be written gives a "Not saved" status and no exception.</summary>
    [Fact]
    public void SaveArtifact_PathIsADirectory_ReportsNotSaved_Test()
    {
        (ExportController controller, _, Workbench workbench, _) = Create("a");
        string directory = Path.Combine(Path.GetTempPath(), "truthweaver-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            bool saved = controller.SaveArtifact(Artifact("rule-text"), directory);

            Assert.False(saved);
            Assert.StartsWith("Not saved: ", workbench.Status, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    /// <summary>A missing image program is reported before any path is asked for.</summary>
    [Fact]
    public void SaveImage_ProgramMissing_SaysSoWithoutAskingForAPath_Test()
    {
        (ExportController controller, _, Workbench workbench, ScriptedPrompts prompts) = Create("a");
        string folder = Path.Combine(Path.GetTempPath(), "truthweaver-image-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string never = Path.Combine(folder, "never-used.svg");
        controller.ImageSearchPath = Path.Combine(folder, "no-tools");
        prompts.SavePath = never;
        try
        {
            bool saved = controller.SaveImage(DiagramLanguage.Dot, ImageFormat.Svg);

            Assert.False(saved);
            Assert.Contains("dot", workbench.Status, StringComparison.Ordinal);
            Assert.False(File.Exists(never));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>With no renderer on the search path no entry is an image.</summary>
    [Fact]
    public void Entries_NoRenderers_HasNoImageEntry_Test()
    {
        (ExportController controller, _, _, _) = Create("a");
        controller.ImageSearchPath = Path.Combine(Path.GetTempPath(), "truthweaver-no-tools-" + Guid.NewGuid().ToString("N"));

        Assert.All(controller.Entries(), e => Assert.Equal(ExportEntryKind.Artifact, e.Kind));
    }

    /// <summary>With no renderer on the search path there is one entry for each artifact of the catalog.</summary>
    [Fact]
    public void Entries_NoRenderers_HasOneEntryForEachArtifact_Test()
    {
        (ExportController controller, _, _, _) = Create("a");
        controller.ImageSearchPath = Path.Combine(Path.GetTempPath(), "truthweaver-no-tools-" + Guid.NewGuid().ToString("N"));

        Assert.Equal(ArtifactCatalog.All.Count, controller.Entries().Count);
    }

    /// <summary>With no renderer on the search path the image note names both missing programs.</summary>
    [Fact]
    public void ImageNote_NoRenderers_NamesBothMissingPrograms_Test()
    {
        (ExportController controller, _, _, _) = Create("a");
        controller.ImageSearchPath = Path.Combine(Path.GetTempPath(), "truthweaver-no-tools-" + Guid.NewGuid().ToString("N"));

        string note = controller.ImageNote();

        Assert.Contains("mmdc", note, StringComparison.Ordinal);
        Assert.Contains("dot", note, StringComparison.Ordinal);
    }

    /// <summary>An export of every artifact without a folder asks for one, and a cancelled folder exports nothing.</summary>
    [Fact]
    public void ExportAll_CancelledFolder_ExportsNothing_Test()
    {
        (ExportController controller, ScriptedServices services, _, _) = Create("a");
        services.Dialogs.Folder = null;

        Assert.False(controller.ExportAll());
    }

    /// <summary>An export of a rule with an error is refused before any folder is asked for.</summary>
    [Fact]
    public void ExportAll_TextWithAnError_IsRefusedWithoutAskingForAFolder_Test()
    {
        (ExportController controller, ScriptedServices services, Workbench workbench, _) = Create("a AND");
        services.Dialogs.Folder = Path.GetTempPath();

        bool exported = controller.ExportAll();

        Assert.False(exported);
        Assert.StartsWith("Not exported: ", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A cancelled export dialog exports nothing.</summary>
    [Fact]
    public void ShowMenu_DialogCancelled_ExportsNothing_Test()
    {
        (ExportController controller, ScriptedServices services, _, _) = Create("a");
        services.Dialogs.ExportRequests.Enqueue(null);

        Assert.False(controller.ShowMenu());
    }

    /// <summary>A cancelled export dialog keeps the larger-table options that were set before.</summary>
    [Fact]
    public void ShowMenu_DialogCancelled_KeepsTheTableOptionsSetBefore_Test()
    {
        (ExportController controller, ScriptedServices services, _, _) = Create("a");
        controller.AllowLargerTruthTable = true;
        controller.TruthTableTermLimit = 20;
        services.Dialogs.ExportRequests.Enqueue(null);

        controller.ShowMenu();

        Assert.True(controller.AllowLargerTruthTable);
        Assert.Equal(20, controller.TruthTableTermLimit);
    }

    /// <summary>A copy request from the menu copies the chosen artifact and stores the table options of the request.</summary>
    [Fact]
    public void ShowMenu_CopyRequest_CopiesTheArtifactAndStoresTheTableOptions_Test()
    {
        (ExportController controller, ScriptedServices services, _, _) = Create("a AND b");
        ExportEntry entry = controller.Entries().First(e => e.Artifact?.Id == "rule-text");
        services.Dialogs.ExportRequests.Enqueue(new ExportRequest(entry, ExportAction.Copy, string.Empty, true, 20));

        bool done = controller.ShowMenu();

        Assert.True(done);
        Assert.NotNull(services.Clipboard.Text);
        Assert.True(controller.AllowLargerTruthTable);
        Assert.Equal(20, controller.TruthTableTermLimit);
    }

    /// <summary>With a renderer on the search path, the image is written to the chosen path and the status names it.</summary>
    [Fact]
    public void SaveImage_RendererWritesTheImage_SavesItAndSaysWhere_Test()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake renderer is a shell script.");
        (ExportController controller, _, Workbench workbench, ScriptedPrompts prompts) = Create("a");
        using TemporaryDirectory tools = TemporaryDirectory.WithRenderer("echo fake > \"$3\"");
        controller.ImageSearchPath = tools.FullPath;
        string image = Path.Combine(tools.FullPath, "rule.svg");

        bool saved = controller.SaveImage(DiagramLanguage.Dot, ImageFormat.Svg, image);

        Assert.True(saved);
        Assert.True(File.Exists(image));
        Assert.Contains("image to", workbench.Status, StringComparison.Ordinal);
        Assert.Null(prompts.SavePath);
    }

    /// <summary>A renderer that fails leaves no image, and the status starts with the reason.</summary>
    [Fact]
    public void SaveImage_RendererFails_SaysNoImageWithTheReason_Test()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake renderer is a shell script.");
        (ExportController controller, _, Workbench workbench, _) = Create("a");
        using TemporaryDirectory tools = TemporaryDirectory.WithRenderer("echo 'syntax error' >&2; exit 3");
        controller.ImageSearchPath = tools.FullPath;
        string image = Path.Combine(tools.FullPath, "rule.svg");

        bool saved = controller.SaveImage(DiagramLanguage.Dot, ImageFormat.Svg, image);

        Assert.False(saved);
        Assert.StartsWith("No image:", workbench.Status, StringComparison.Ordinal);
        Assert.False(File.Exists(image));
    }

    /// <summary>Cancelling the save dialog writes no image.</summary>
    [Fact]
    public void SaveImage_SaveDialogCancelled_WritesNothing_Test()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake renderer is a shell script.");
        (ExportController controller, _, _, ScriptedPrompts prompts) = Create("a");
        using TemporaryDirectory tools = TemporaryDirectory.WithRenderer("echo fake > \"$3\"");
        controller.ImageSearchPath = tools.FullPath;
        prompts.SavePath = null;

        bool saved = controller.SaveImage(DiagramLanguage.Dot, ImageFormat.Svg);

        Assert.False(saved);
        Assert.Empty(Directory.GetFiles(tools.FullPath, "*.svg"));
    }

    private static ArtifactDefinition Artifact(string id)
    {
        Assert.True(ArtifactCatalog.TryFind(id, out ArtifactDefinition? artifact));
        return artifact;
    }

    private static (
        ExportController Controller,
        ScriptedServices Services,
        Workbench Workbench,
        ScriptedPrompts Prompts
    ) Create(string text)
    {
        ScriptedServices services = new();
        ScriptedPrompts prompts = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, prompts);
        return (new ExportController(workbench, services.Services), services, workbench, prompts);
    }
}
