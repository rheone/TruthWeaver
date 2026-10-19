namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Tui.Hosting;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The logic behind the export dialog: every artifact of the tool core catalog for the open rule, to the clipboard or to a file,
/// every artifact to a folder with a manifest, and the diagrams as images when Graphviz or the Mermaid CLI is installed. The
/// text of an artifact is what <see cref="ArtifactExporter"/> makes, so it equals the output of the command line. It has no
/// view, and a missing image program is a message on the status line, never a dialog.
/// </summary>
internal sealed class ExportController
{
    private static readonly ConditionalWeakTable<Workbench, ExportController> Controllers = [];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;

    /// <summary>Initializes a new instance of the <see cref="ExportController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public ExportController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
    }

    /// <summary>Gets or sets the directories to search for the image programs, or <see langword="null"/> for the <c>PATH</c>. A test sets it.</summary>
    public string? ImageSearchPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a truth table over the term cap is made anyway, up to the hard limit, with a
    /// warning (<see cref="ExportOptions.AllowLargerTruthTable"/>). The default is <see langword="false"/>.
    /// </summary>
    public bool AllowLargerTruthTable { get; set; }

    /// <summary>
    /// Gets or sets the most distinct terms of a truth table (<see cref="ExportOptions.TruthTableTermLimit"/>), or
    /// <see langword="null"/> for the cap and <see cref="AllowLargerTruthTable"/>. A number allows a larger table. The default is
    /// <see langword="null"/>.
    /// </summary>
    public int? TruthTableTermLimit { get; set; }

    private WorkbenchServices Services => this.services ?? WorkbenchServices.Current;

    private ExportOptions Options =>
        new()
        {
            Settings = this.workbench.Settings,
            AllowLargerTruthTable = this.AllowLargerTruthTable,
            TruthTableTermLimit = this.TruthTableTermLimit,
            Images = null,
        };

    /// <summary>Gets the controller of a workbench. The keys use the same controller each time.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static ExportController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new ExportController(key));
    }

    /// <summary>
    /// Gets the entries of the dialog: every artifact of the catalog, then the images of each diagram language whose renderer
    /// is found. A language without its renderer has no entries; <see cref="ImageNote"/> says what to install.
    /// </summary>
    /// <returns>The entries.</returns>
    public IReadOnlyList<ExportEntry> Entries()
    {
        List<ExportEntry> entries =
        [
            .. ArtifactCatalog.All.Select(a => new ExportEntry(
                ExportEntryKind.Artifact,
                $"{a.Id}  ({a.Format}): {a.Description}",
                a,
                Group: GroupOf(a),
                FileName: a.FileName
            )),
        ];
        foreach (DiagramLanguage language in new[] { DiagramLanguage.Mermaid, DiagramLanguage.Dot })
        {
            foreach (ImageFormat format in new[] { ImageFormat.Svg, ImageFormat.Png })
            {
                if (ImageRenderer.FindProgram(language, this.ImageSearchPath) is null)
                {
                    continue;
                }

                string program = ImageRenderer.ProgramName(language);
                entries.Add(
                    new ExportEntry(
                        ExportEntryKind.Image,
                        $"image-{language.ToString().ToLowerInvariant()}-{ImageRenderer.Extension(format)[1..]}: the diagram as {format.ToString().ToUpperInvariant()} (with {program})",
                        null,
                        language,
                        format,
                        ExportGroups.Images,
                        (language == DiagramLanguage.Dot ? "dot" : "mermaid") + ImageRenderer.Extension(format)
                    )
                );
            }
        }

        return entries;
    }

    /// <summary>Gets the one line that names the image renderers to install, in Core's own words.</summary>
    /// <returns>The line, or an empty string when both renderers are found.</returns>
    public string ImageNote()
    {
        return string.Join(
            " ",
            new[] { DiagramLanguage.Mermaid, DiagramLanguage.Dot }
                .Where(l => ImageRenderer.FindProgram(l, this.ImageSearchPath) is null)
                .Select(this.MissingProgram)
        );
    }

    /// <summary>Makes the text of one artifact for the text of the editor.</summary>
    /// <param name="artifact">The artifact.</param>
    /// <returns>The text, or the diagnostics.</returns>
    public ToolResult<string> Render(ArtifactDefinition artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        ToolResult<LoadedRule> rule = this.CurrentRule();
        return rule.TryGetValue(out LoadedRule? loaded, out EquatableArray<Diagnostic> diagnostics)
            ? ArtifactExporter.Render(loaded, artifact, this.Options)
            : Results.Failure<string>(diagnostics);
    }

    /// <summary>Puts one artifact on the clipboard.</summary>
    /// <param name="artifact">The artifact.</param>
    /// <returns><see langword="true"/> when the clipboard took the text.</returns>
    public bool CopyArtifact(ArtifactDefinition artifact)
    {
        ToolResult<string> rendered = this.Render(artifact);
        if (!rendered.TryGetValue(out string? text, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(this.Failure(artifact, diagnostics));
            return false;
        }

        if (!this.Services.Clipboard.TrySet(text))
        {
            this.workbench.SetStatus("The clipboard is not available here. Save to a file instead.");
            return false;
        }

        this.workbench.SetStatus($"Copied {artifact.Id} to the clipboard{Warnings(rendered.Diagnostics)}.");
        return true;
    }

    /// <summary>Writes one artifact to a file, and asks for the path through the save dialog when none is given.</summary>
    /// <param name="artifact">The artifact.</param>
    /// <param name="path">The file path, or <see langword="null"/> to ask.</param>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool SaveArtifact(ArtifactDefinition artifact, string? path = null)
    {
        ToolResult<string> rendered = this.Render(artifact);
        if (!rendered.TryGetValue(out string? text, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(this.Failure(artifact, diagnostics));
            return false;
        }

        if (this.PathOrAsk(path, artifact.FileName) is not { } target)
        {
            return false;
        }

        ToolResult<string> written = OutputWriter.WriteFile(target, text);
        if (!written.TryGetValue(out string? file, out diagnostics))
        {
            this.workbench.SetStatus("Not saved: " + First(diagnostics));
            return false;
        }

        this.workbench.SetStatus($"Saved {artifact.Id} to {file}{Warnings(rendered.Diagnostics)}.");
        return true;
    }

    /// <summary>
    /// Renders the diagram of the rule as an image with Graphviz or the Mermaid CLI. A program that is not installed is a message on
    /// the status line that names the program and where to get it; no dialog opens.
    /// </summary>
    /// <param name="language">The diagram language.</param>
    /// <param name="format">The image format.</param>
    /// <param name="path">The file path, or <see langword="null"/> to ask through the save dialog.</param>
    /// <returns><see langword="true"/> when the image file was written.</returns>
    public bool SaveImage(DiagramLanguage language, ImageFormat format, string? path = null)
    {
        // The program check comes first, so a missing program never asks for a path.
        if (ImageRenderer.FindProgram(language, this.ImageSearchPath) is null)
        {
            this.workbench.SetStatus(this.MissingProgram(language));
            return false;
        }

        string id = language == DiagramLanguage.Dot ? "dot" : "mermaid";
        ArtifactDefinition diagram = ArtifactCatalog.All.First(a => a.Id == id);
        ToolResult<string> rendered = this.Render(diagram);
        if (!rendered.TryGetValue(out string? text, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(this.Failure(diagram, diagnostics));
            return false;
        }

        if (this.PathOrAsk(path, id + ImageRenderer.Extension(format)) is not { } target)
        {
            return false;
        }

        ToolResult<string> image = ImageRenderer.Render(
            text,
            language,
            target,
            new ImageRenderOptions { Format = format, SearchPath = this.ImageSearchPath }
        );
        if (!image.TryGetValue(out string? file, out diagnostics))
        {
            this.workbench.SetStatus("No image: " + First(diagnostics));
            return false;
        }

        this.workbench.SetStatus($"Saved the {id} image to {file}.");
        return true;
    }

    /// <summary>
    /// Asks for a folder and writes every artifact there, with <c>manifest.json</c>. The images join the export when both image
    /// programs are installed; otherwise the status line names the missing program and the export goes on without images.
    /// </summary>
    /// <param name="folder">The folder, or <see langword="null"/> to ask.</param>
    /// <returns><see langword="true"/> when the folder was written.</returns>
    public bool ExportAll(string? folder = null)
    {
        ToolResult<LoadedRule> rule = this.CurrentRule();
        if (!rule.TryGetValue(out LoadedRule? loaded, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus("Not exported: " + First(diagnostics));
            return false;
        }

        folder ??= this.Services.Dialogs.AskFolder("Export every artifact to a folder");
        if (folder is null)
        {
            return false;
        }

        string missing = string.Join(
            " ",
            new[] { DiagramLanguage.Mermaid, DiagramLanguage.Dot }
                .Where(l => ImageRenderer.FindProgram(l, this.ImageSearchPath) is null)
                .Select(this.MissingProgram)
        );
        ExportOptions options = this.Options with
        {
            Images = missing.Length == 0 ? new ImageRenderOptions { SearchPath = this.ImageSearchPath } : null,
        };
        ToolResult<ExportManifest> exported = ArtifactExporter.ExportAll(loaded, folder, options);
        if (!exported.TryGetValue(out ExportManifest? manifest, out diagnostics))
        {
            this.workbench.SetStatus("Not exported: " + First(diagnostics));
            return false;
        }

        string message = $"Exported {manifest.Artifacts.Count} files and {ExportManifest.FileName} to {folder}.";
        if (manifest.Skipped.Count > 0)
        {
            message +=
                $" Left out: {string.Join(", ", manifest.Skipped.Select(s => s.Id))} (over the truth-table cap; the export menu can raise it).";
        }

        if (missing.Length > 0)
        {
            message += " No images. " + missing;
        }

        this.workbench.SetStatus(message);
        return true;
    }

    /// <summary>
    /// Shows the export dialog and does what it asks: copies or saves one artifact or image, or writes every artifact to a
    /// folder. The larger-table option of the dialog stays for the next export.
    /// </summary>
    /// <returns><see langword="true"/> when something was exported.</returns>
    public bool ShowMenu()
    {
        string directory = Path.GetDirectoryName(this.workbench.Document.Path) ?? Environment.CurrentDirectory;
        ExportDialogModel model = new(
            this.Entries(),
            directory,
            this.AllowLargerTruthTable,
            $"Make truth tables over {TruthTableOptions.DefaultTermCap} terms (up to {TruthTableOptions.HardTermLimit})",
            this.TruthTableTermLimit,
            this.ImageNote()
        );
        if (this.Services.Dialogs.AskExport(model) is not { } request)
        {
            return false;
        }

        this.AllowLargerTruthTable = request.AllowLargerTruthTable;
        this.TruthTableTermLimit = request.TruthTableTermLimit;
        if (request.Entry is null)
        {
            return this.ExportAll(request.Path);
        }

        if (request.Entry.Kind == ExportEntryKind.Image)
        {
            return this.SaveImage(request.Entry.Language, request.Entry.Format, request.Path);
        }

        return request.Action == ExportAction.Copy
            ? this.CopyArtifact(request.Entry.Artifact!)
            : this.SaveArtifact(request.Entry.Artifact!, request.Path);
    }

    private static string First(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? "unknown problem." : $"{first.Code}: {first.Message}";
    }

    private static string Warnings(IEnumerable<Diagnostic> diagnostics)
    {
        string[] warnings =
        [
            .. diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => $"{d.Code}: {d.Message}"),
        ];
        return warnings.Length == 0 ? string.Empty : " (" + string.Join(" ", warnings) + ")";
    }

    // The group of the export dialog that holds an artifact.
    private static string GroupOf(ArtifactDefinition artifact)
    {
        return artifact.Id switch
        {
            "rule-text" or "rule-text-symbolic" or "rule-text-pretty" or "json" or "yaml" => ExportGroups.Rule,
            "tree" or "mermaid" or "dot" or "html" => ExportGroups.Diagram,
            "report" => ExportGroups.Report,
            string id when id.StartsWith("equation-", StringComparison.Ordinal) => ExportGroups.Equation,
            string id when id.StartsWith("truth-table-", StringComparison.Ordinal) => ExportGroups.TruthTable,
            _ => ExportGroups.Analysis,
        };
    }

    // The editor text as a loaded rule in the format of the document, so the artifacts are made from what the author sees.
    private ToolResult<LoadedRule> CurrentRule()
    {
        RuleDocument document = this.workbench.Document;
        return RuleLoader.LoadString(document.Text, "<editor>", new RuleLoadOptions { Format = document.Format });
    }

    // The message of a refused artifact. A refused truth table says how to raise the cap.
    private string Failure(ArtifactDefinition artifact, IEnumerable<Diagnostic> diagnostics)
    {
        string message = $"Cannot make {artifact.Id}: {First(diagnostics)}";
        if (
            artifact.Id.StartsWith("truth-table", StringComparison.Ordinal)
            && !this.AllowLargerTruthTable
            && this.TruthTableTermLimit is null
        )
        {
            message += " Open the export dialog (F5) and tick the larger truth tables option or set a term limit.";
        }

        return message;
    }

    // A path from the export dialog is final, except that a file that exists needs a yes. No path means the save dialog, which
    // asks about an existing file itself.
    private string? PathOrAsk(string? path, string fileName)
    {
        if (path is null)
        {
            string suggested = Path.Combine(
                Path.GetDirectoryName(this.workbench.Document.Path) ?? Environment.CurrentDirectory,
                fileName
            );
            return this.workbench.Prompts.AskSavePath(suggested);
        }

        return File.Exists(path) && !this.Services.Dialogs.Confirm("Overwrite file", $"{path} exists. Replace it?", "Replace")
            ? null
            : path;
    }

    // Core's own words name the program and where to get it.
    private string MissingProgram(DiagramLanguage language)
    {
        ToolResult<string> probe = ImageRenderer.Render(
            string.Empty,
            language,
            Path.Combine(Path.GetTempPath(), "truthweaver-probe"),
            new ImageRenderOptions { SearchPath = this.ImageSearchPath }
        );
        return First(probe.Diagnostics);
    }
}
