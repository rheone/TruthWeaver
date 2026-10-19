namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Hosting;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Theme;

/// <summary>
/// The state that every pane shares: the open <see cref="RuleDocument"/>, the settings, the layout and the status message,
/// and the actions behind the keys (open, save, undo, redo, format). It has no view, so the panes stay in sync through
/// its events and a test drives it without a terminal.
/// </summary>
internal sealed partial class Workbench
{
    private RuleDocument document;

    /// <summary>Initializes a new instance of the <see cref="Workbench"/> class.</summary>
    /// <param name="document">The document to start with.</param>
    /// <param name="settings">The resolved tool settings, used for every document the workbench opens.</param>
    /// <param name="prompts">The questions to the author.</param>
    /// <param name="theme">The color set to start with, or <see langword="null"/> to take the <c>theme</c> setting (<c>auto</c> then gives dark: only the program asks the terminal).</param>
    /// <param name="noColor"><see langword="true"/> to draw without color (<c>NO_COLOR</c>).</param>
    public Workbench(
        RuleDocument document,
        ToolSettings settings,
        IWorkbenchPrompts prompts,
        ThemeVariant? theme = null,
        bool noColor = false
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(prompts);

        this.document = document;
        this.Settings = settings;
        this.Prompts = prompts;
        this.Theme = theme ?? ThemeChoice.Resolve(settings.Theme, static () => null);
        this.NoColor = noColor;
        this.document.Changed += this.OnDocumentChanged;
    }

    /// <summary>
    /// Occurs after the document changes (its text, format, path or saved state) and after another document replaces it.
    /// A pane subscribes here, not to <see cref="RuleDocument.Changed"/>, so it follows a newly opened document too.
    /// </summary>
    public event EventHandler? DocumentChanged;

    /// <summary>Occurs after <see cref="Status"/> changes.</summary>
    public event EventHandler? StatusChanged;

    /// <summary>Occurs when a pane asks the editor to show a span of the text, such as the span of a diagnostic.</summary>
    public event EventHandler<SourceSpan>? SpanRequested;

    /// <summary>Gets the open document.</summary>
    /// <remarks>
    /// Reading the document applies the text that waits for its background compile (see <see cref="EditText"/>), so the
    /// caller always sees the text the author typed.
    /// </remarks>
    public RuleDocument Document
    {
        get
        {
            this.FlushPendingText();
            return this.document;
        }
    }

    /// <summary>Gets the resolved tool settings.</summary>
    public ToolSettings Settings { get; }

    /// <summary>Gets the questions to the author.</summary>
    public IWorkbenchPrompts Prompts { get; }

    /// <summary>
    /// Gets or sets the action that <see cref="Save"/> runs in place of writing the document to its own file, or
    /// <see langword="null"/> for the normal save. A rule that lives inside a larger document sets it, so Ctrl+S writes the rule
    /// back into that document. The action returns <see langword="true"/> when it saved.
    /// </summary>
    public Func<bool>? SaveOverride { get; set; }

    /// <summary>
    /// Gets or sets a name for the status line in place of the file name, or <see langword="null"/>. A rule that lives inside
    /// a larger document shows the document and the path of the rule, such as <c>policies.yaml:$.rules.publish</c>.
    /// </summary>
    public string? DocumentLabel { get; set; }

    /// <summary>Gets the sizes of the regions and the maximised region.</summary>
    public PaneLayout Layout { get; } = new();

    /// <summary>
    /// Gets or sets the file of the choices that outlive the session (the theme and the sizes of the regions), or
    /// <see langword="null"/> to keep nothing. The program sets it; a test leaves it unset so no test writes a user file.
    /// </summary>
    public WorkbenchPreferences? Preferences { get; set; }

    /// <summary>Gets the paths of the recently opened files, newest first, at most ten. It is empty when no <see cref="Preferences"/> file is set.</summary>
    public IReadOnlyList<string> RecentFiles => this.Preferences?.RecentFiles ?? [];

    /// <summary>
    /// Gets a value indicating whether the rule has changes that no save has written: the saved state of the document differs, or
    /// an edit still waits for its compile. A rule that is typed and then typed back to the saved text reads as changed until the
    /// compile ends.
    /// </summary>
    public bool HasUnsavedChanges => this.IsChecking || this.document.IsDirty;

    /// <summary>Gets the last message for the status line, such as the result of a save, or an empty string.</summary>
    public string Status { get; private set; } = string.Empty;

    /// <summary>Gets the name of a format for the status line: rule text, JSON or YAML.</summary>
    /// <param name="format">The format.</param>
    /// <returns>The name.</returns>
    public static string FormatName(Compilation.RuleFormat format)
    {
        return format switch
        {
            Compilation.RuleFormat.Json => "JSON",
            Compilation.RuleFormat.Yaml => "YAML",
            _ => "rule text",
        };
    }

    /// <summary>Sets the status message.</summary>
    /// <param name="message">The message.</param>
    public void SetStatus(string message)
    {
        this.Status = message;
        this.StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Asks the editor to show a span of the text and put the cursor at its start.</summary>
    /// <param name="span">The span in <see cref="RuleDocument.Text"/>.</param>
    public void RequestSpan(SourceSpan span)
    {
        this.SpanRequested?.Invoke(this, span);
    }

    /// <summary>Asks for a file and opens it in place of the document. Unsaved changes need the author's consent first.</summary>
    /// <returns><see langword="true"/> when a file was opened.</returns>
    public bool Open()
    {
        this.FlushPendingText();
        if (this.document.IsDirty && !this.Prompts.ConfirmDiscard("open another file"))
        {
            return false;
        }

        return this.Prompts.AskOpenPath() is { } path && this.OpenPath(path);
    }

    /// <summary>Opens a file in place of the document, without asking.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the file was opened; otherwise the status line says why not.</returns>
    public bool OpenPath(string path)
    {
        ToolResult<RuleDocument> opened = RuleDocument.Open(path, this.Settings);
        if (!opened.TryGetValue(out RuleDocument? next, out EquatableArray<Diagnostic> diagnostics))
        {
            this.SetStatus(Describe(diagnostics, $"Cannot open '{path}'."));
            return false;
        }

        this.ReplaceDocument(next, $"Opened {next.Path} as {FormatName(next.Format)}.");
        this.RememberRecent(next.Path ?? path);
        return true;
    }

    /// <summary>
    /// Opens a file of the recent list. Unsaved changes need the author's consent first. A file that no longer exists is taken
    /// out of the list and the status line says so; the open rule stays.
    /// </summary>
    /// <param name="path">The path from the list.</param>
    /// <returns><see langword="true"/> when the file was opened.</returns>
    public bool OpenRecent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // A file that is gone is only taken out of the list, so it needs no consent about unsaved changes.
        if (!File.Exists(path))
        {
            _ = this.RemoveRecent(path);
            this.SetStatus($"'{path}' no longer exists. It is out of the recent files.");
            return false;
        }

        this.FlushPendingText();
        if (this.document.IsDirty && !this.Prompts.ConfirmDiscard("open another file"))
        {
            return false;
        }

        return this.OpenPath(path);
    }

    /// <summary>Puts a file at the top of the recent files. Nothing happens when no <see cref="Preferences"/> file is set.</summary>
    /// <param name="path">The path of the file the author opened.</param>
    public void RememberRecent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        this.Preferences?.AddRecent(Path.GetFullPath(path));
    }

    /// <summary>Takes a file out of the recent files.</summary>
    /// <param name="path">The path.</param>
    /// <returns><see langword="true"/> when the path was in the list.</returns>
    public bool RemoveRecent(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return this.Preferences?.RemoveRecent(path) ?? false;
    }

    /// <summary>
    /// Puts another document in place of the open one, without asking, and clears <see cref="SaveOverride"/>. A caller that
    /// binds the new document to somewhere else, such as a rule inside a larger document, sets the override after this call.
    /// </summary>
    /// <param name="next">The new document.</param>
    /// <param name="status">The message for the status line.</param>
    public void ReplaceDocument(RuleDocument next, string status)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(status);

        this.DiscardPending();
        this.document.Changed -= this.OnDocumentChanged;
        this.document = next;
        this.document.Changed += this.OnDocumentChanged;
        this.SaveOverride = null;
        this.DocumentLabel = null;
        this.SetStatus(status);
        this.DocumentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Saves the document to its file, or asks for a path when it has none.</summary>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool Save()
    {
        if (this.SaveOverride is { } custom)
        {
            return custom();
        }

        return this.document.Path is null ? this.SaveAs() : this.SaveTo(null);
    }

    /// <summary>Asks for a path and saves the document there. The extension selects the format.</summary>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool SaveAs()
    {
        return this.Prompts.AskSavePath(this.document.Path) is { } path && this.SaveTo(path);
    }

    /// <summary>Undoes the last change of the document.</summary>
    public void Undo()
    {
        this.SetStatus(this.document.Undo() ? "Undone." : "Nothing to undo.");
    }

    /// <summary>Redoes the last undone change of the document.</summary>
    public void Redo()
    {
        this.SetStatus(this.document.Redo() ? "Redone." : "Nothing to redo.");
    }

    /// <summary>Formats the document in its own format.</summary>
    /// <param name="pretty"><see langword="true"/> for the pretty form; <see langword="false"/> for the canonical form.</param>
    public void Format(bool pretty)
    {
        string form = pretty ? "pretty" : "canonical";
        ToolResult<string> formatted = this.document.FormatText(pretty);
        this.SetStatus(
            formatted.Succeeded
                ? $"Formatted as {form} {FormatName(this.document.Format)}."
                : "Not formatted: the text has errors. Fix them first, or undo to the last valid text."
        );
    }

    private static string Describe(EquatableArray<Diagnostic> diagnostics, string fallback)
    {
        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? fallback : $"{first.Code}: {first.Message}";
    }

    private bool SaveTo(string? path)
    {
        ToolResult<string> saved = this.document.Save(path);
        if (!saved.TryGetValue(out string? written, out EquatableArray<Diagnostic> diagnostics))
        {
            this.SetStatus(
                Describe(diagnostics, "Not saved: the text has errors, so it cannot be converted to another format.")
            );
            return false;
        }

        this.SetStatus($"Saved {written} as {FormatName(this.document.Format)}.");
        return true;
    }

    private void OnDocumentChanged(object? sender, EventArgs e)
    {
        // Another change of the document makes an older edit stale; the result of its compile must not replace this change.
        this.DiscardPending();
        this.DocumentChanged?.Invoke(this, EventArgs.Empty);
    }
}
