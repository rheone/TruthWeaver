namespace TruthWeaver.Tool.Tui;

using System.Runtime.CompilerServices;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// The logic behind the import and conversion keys: open a file or the clipboard in rule text, JSON or YAML (detected, or
/// with the format the author names), and convert the open rule between the three forms. It has no view.
/// </summary>
internal sealed class ImportController
{
    private static readonly ConditionalWeakTable<Workbench, ImportController> Controllers = [];
    private static readonly RuleFormat[] Cycle = [RuleFormat.Text, RuleFormat.Json, RuleFormat.Yaml];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;

    /// <summary>Initializes a new instance of the <see cref="ImportController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public ImportController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
    }

    /// <summary>Gets the text of the banner after the last conversion, or an empty string.</summary>
    public string Banner { get; private set; } = string.Empty;

    private WorkbenchServices Services => this.services ?? WorkbenchServices.Current;

    /// <summary>Gets the controller of a workbench. The keys use the same controller each time.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static ImportController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new ImportController(key));
    }

    /// <summary>Gets the format that a menu entry names, or <see langword="null"/> for "detect".</summary>
    /// <param name="choice">The index of the entry: 0 detect, 1 rule text, 2 JSON, 3 YAML.</param>
    /// <returns>The format.</returns>
    public static RuleFormat? FormatOfChoice(int choice)
    {
        return choice switch
        {
            1 => RuleFormat.Text,
            2 => RuleFormat.Json,
            3 => RuleFormat.Yaml,
            _ => null,
        };
    }

    /// <summary>Asks where to import from (the clipboard or a file) and in which format, and imports.</summary>
    /// <returns><see langword="true"/> when a rule was imported.</returns>
    public bool Import()
    {
        IWorkbenchDialogs dialogs = this.Services.Dialogs;
        int? source = dialogs.Choose("Import a rule from", ["The clipboard", "A file"]);
        if (source is null)
        {
            return false;
        }

        int? how = dialogs.Choose("Format of the input", ["Detect it", "Rule text", "JSON", "YAML"]);
        if (how is null)
        {
            return false;
        }

        RuleFormat? format = FormatOfChoice(how.Value);
        if (source == 0)
        {
            return this.FromClipboard(format);
        }

        if (this.workbench.Document.IsDirty && !this.workbench.Prompts.ConfirmDiscard("open another file"))
        {
            return false;
        }

        return this.workbench.Prompts.AskOpenPath() is { } path && this.FromFile(path, format);
    }

    /// <summary>
    /// Opens the clipboard text as the rule. Only a valid expression gets in: text that does not compile reports its first
    /// error and leaves the open rule alone. Unsaved changes need the author's consent first.
    /// </summary>
    /// <param name="format">The format the author names, or <see langword="null"/> to detect it.</param>
    /// <returns><see langword="true"/> when the rule was replaced.</returns>
    public bool FromClipboard(RuleFormat? format)
    {
        if (!this.Services.Clipboard.TryGet(out string? text) || text.Trim().Length == 0)
        {
            this.workbench.SetStatus("The clipboard has no text, or it is not available here.");
            return false;
        }

        ToolResult<ImportedRule> imported = RuleImport.FromText(
            text,
            RuleImport.ClipboardOrigin,
            format,
            this.workbench.Settings,
            requireValid: true
        );
        if (!imported.TryGetValue(out ImportedRule? rule, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus("Not imported: " + First(diagnostics));
            return false;
        }

        if (this.workbench.Document.IsDirty && !this.workbench.Prompts.ConfirmDiscard("replace the rule with the clipboard"))
        {
            return false;
        }

        this.workbench.ReplaceDocument(
            rule.Document,
            $"Imported the clipboard as {Workbench.FormatName(rule.Document.Format)} ({Evidence(rule.Detection)})."
        );
        return true;
    }

    /// <summary>Opens a file with the format the author names. A file that does not compile still opens, with its diagnostics.</summary>
    /// <param name="path">The file.</param>
    /// <param name="format">The format the author names, or <see langword="null"/> to detect it.</param>
    /// <returns><see langword="true"/> when the file was opened.</returns>
    public bool FromFile(string path, RuleFormat? format)
    {
        ArgumentNullException.ThrowIfNull(path);

        ToolResult<RuleDocument> opened = RuleDocument.Open(path, this.workbench.Settings, format);
        if (!opened.TryGetValue(out RuleDocument? document, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus($"Cannot open '{path}': {First(diagnostics)}");
            return false;
        }

        this.workbench.ReplaceDocument(document, $"Opened {document.Path} as {Workbench.FormatName(document.Format)}.");
        return true;
    }

    /// <summary>Converts the open rule to the next (or previous) format in the order rule text, JSON, YAML.</summary>
    /// <param name="direction">1 for the next format; -1 for the previous one.</param>
    /// <returns><see langword="true"/> when the rule was converted.</returns>
    public bool CycleFormat(int direction)
    {
        int index = Array.IndexOf(Cycle, this.workbench.Document.Format);
        return this.ConvertTo(Cycle[(index + direction + Cycle.Length) % Cycle.Length]);
    }

    /// <summary>
    /// Converts the open rule to another format as one undo step. The tree and the notes stay; the banner says what does not.
    /// Text that does not compile is not converted.
    /// </summary>
    /// <param name="format">The format to convert to.</param>
    /// <returns><see langword="true"/> when the rule was converted.</returns>
    public bool ConvertTo(RuleFormat format)
    {
        RuleDocument document = this.workbench.Document;
        RuleFormat from = document.Format;
        string oldText = document.Text;
        ToolResult<string> converted = document.ConvertTo(format);
        if (!converted.Succeeded)
        {
            this.Banner = "Not converted: the rule text has errors. Fix them first, or undo to the last valid text.";
            this.workbench.SetStatus(this.Banner);
            return false;
        }

        if (from == format)
        {
            this.Banner = $"The rule is already {Workbench.FormatName(format)}.";
            this.workbench.SetStatus(this.Banner);
            return true;
        }

        this.Banner = ConversionNotice.Describe(from, format, oldText);
        this.workbench.SetStatus(this.Banner);
        return true;
    }

    private static string First(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? "unknown problem." : $"{first.Code}: {first.Message}";
    }

    private static string Evidence(FormatDetection detection)
    {
        return detection.Evidence switch
        {
            FormatEvidence.Override => "the format you named",
            FormatEvidence.Extension => "from the extension",
            _ => "detected from the content",
        };
    }
}
