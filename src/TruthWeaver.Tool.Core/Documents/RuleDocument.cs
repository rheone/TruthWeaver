namespace TruthWeaver.Tool.Core.Documents;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Yaml;

/// <summary>
/// One rule open for editing: its text and format, the file it belongs to, and the result of compiling the text. Every
/// change compiles the text again. When the new text does not compile, the document keeps the last tree that compiled
/// (<see cref="Rule"/>) and reports the errors of the new text (<see cref="Diagnostics"/>), so a view of the tree stays
/// usable while the author types. Each change is one step of undo and redo.
/// </summary>
/// <remarks>
/// This is the UI-free model of the terminal UI editor. It has no thread affinity and no locks: call it from one thread.
/// It compiles permissively, as <see cref="RuleConverter"/> does, so any predicate name and any argument compiles.
/// </remarks>
public sealed class RuleDocument
{
    /// <summary>The number of undo steps the document keeps. The oldest step is dropped first.</summary>
    public const int UndoLimit = 1000;

    private readonly ToolSettings settings;
    private readonly RuleCompiler<StubAssignment> compiler;
    private readonly LinkedList<Snapshot> undo = new();
    private readonly Stack<Snapshot> redo = new();
    private Snapshot current;
    private Snapshot saved;
    private RuleDefinitions? definitions;

    private RuleDocument(string text, RuleFormat format, string? path, ToolSettings? settings)
    {
        this.settings = settings ?? ToolSettings.Default;
        this.compiler = RuleConverter.CreateCompiler(this.settings);
        this.current = new Snapshot(text, format);
        this.saved = this.current;
        this.Path = path;
        this.Compile();
    }

    /// <summary>Occurs after the text, the format, the path or the saved state changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the text of the rule as the author wrote it.</summary>
    public string Text => this.current.Text;

    /// <summary>Gets the format of <see cref="Text"/>.</summary>
    public RuleFormat Format => this.current.Format;

    /// <summary>Gets the full path of the file of the document, or <see langword="null"/> for a document that was never saved.</summary>
    public string? Path { get; private set; }

    /// <summary>Gets a value indicating whether the text or format differs from the last open or save.</summary>
    public bool IsDirty => this.current != this.saved;

    /// <summary>Gets the diagnostics of the current <see cref="Text"/>. Their spans point into <see cref="Text"/>.</summary>
    public EquatableArray<Diagnostic> Diagnostics { get; private set; } = new([]);

    /// <summary>
    /// Gets the last rule that compiled: the rule of the current text when <see cref="IsRuleCurrent"/> is
    /// <see langword="true"/>, otherwise the rule of an earlier text. It is <see langword="null"/> when no text of this
    /// document compiled yet.
    /// </summary>
    public CompiledRule<StubAssignment>? Rule { get; private set; }

    /// <summary>Gets a value indicating whether <see cref="Rule"/> is the rule of the current <see cref="Text"/>.</summary>
    public bool IsRuleCurrent { get; private set; }

    /// <summary>
    /// Gets the <c>let</c> definitions and uses of the current <see cref="Text"/>, read without expanding them (see
    /// <see cref="RuleDefinitions"/>). It is read when first asked for after a change. When the text has a syntax error,
    /// <see cref="RuleDefinitions.Succeeded"/> is <see langword="false"/>.
    /// </summary>
    public RuleDefinitions Definitions => this.definitions ??= YamlRuleDefinitions.ReadAs(this.Text, this.Format);

    /// <summary>Gets a value indicating whether <see cref="Undo"/> has a step to undo.</summary>
    public bool CanUndo => this.undo.Count > 0;

    /// <summary>Gets a value indicating whether <see cref="Redo"/> has a step to redo.</summary>
    public bool CanRedo => this.redo.Count > 0;

    /// <summary>Creates a document that has no file yet.</summary>
    /// <param name="text">The rule text, or an empty string.</param>
    /// <param name="format">The format of <paramref name="text"/>.</param>
    /// <param name="settings">The settings (limits and operator style), or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The document, compiled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static RuleDocument Create(string text = "", RuleFormat format = RuleFormat.Text, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format.");
        }

        return new RuleDocument(text, format, null, settings);
    }

    /// <summary>
    /// Opens a rule file. <see cref="RuleLoader"/> reads it and finds its format (the extension, then the content). A file
    /// that is read but does not compile still opens, with its diagnostics.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <param name="format">The format of the file, or <see langword="null"/> (the default) to find it from the extension and the content.</param>
    /// <returns>The document, or the diagnostics when the file cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static ToolResult<RuleDocument> Open(string path, ToolSettings? settings = null, RuleFormat? format = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (
            !RuleLoader
                .LoadFile(path, new RuleLoadOptions { Format = format })
                .TryGetValue(out LoadedRule? loaded, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            return ToolResults.Failure<RuleDocument>(diagnostics);
        }

        RuleDocument document = new(loaded.Text, loaded.Format, System.IO.Path.GetFullPath(path), settings);
        return ToolResults.Success(document, diagnostics);
    }

    /// <summary>
    /// Replaces the text and compiles it. The change is one undo step and clears the redo steps. Text equal to the
    /// current text changes nothing.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        this.Apply(this.current with { Text = text });
    }

    /// <summary>
    /// Replaces the text and its format together and compiles the text in the new format. The change is one undo step and
    /// clears the redo steps, so undo gives back both the text and the format. Equal text in the same format changes nothing.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <param name="format">The format of <paramref name="text"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public void SetText(string text, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Not a defined rule format.");
        }

        this.Apply(new Snapshot(text, format));
    }

    /// <summary>
    /// Replaces the text with text that <see cref="Check"/> compiled, without compiling it again. It is the same change as
    /// <see cref="SetText(string)"/>: one undo step that clears the redo steps. When the format of the document is no longer
    /// the format that was checked, the text is compiled again.
    /// </summary>
    /// <param name="checkedText">The result of <see cref="Check"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="checkedText"/> is <see langword="null"/>.</exception>
    public void SetText(CheckedText checkedText)
    {
        ArgumentNullException.ThrowIfNull(checkedText);

        this.Apply(
            this.current with
            {
                Text = checkedText.Text,
            },
            checkedText.Format == this.Format ? checkedText.Result : null
        );
    }

    /// <summary>
    /// Compiles text without changing the document, so a front end can compile off its UI thread and apply the result with
    /// <see cref="SetText(CheckedText)"/>. The method reads no state of the document except the compiler, which keeps no
    /// state between calls, so any thread may call it.
    /// </summary>
    /// <param name="text">The text to compile.</param>
    /// <param name="format">The format of <paramref name="text"/>.</param>
    /// <returns>The text with its format and compilation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public CheckedText Check(string text, RuleFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);

        return new CheckedText(text, format, this.compiler.CompileAs(text, format));
    }

    /// <summary>Goes back one step: the text and format before the last change.</summary>
    /// <returns><see langword="true"/> when a step was undone; <see langword="false"/> when there was none.</returns>
    public bool Undo()
    {
        if (this.undo.Last is not { } last)
        {
            return false;
        }

        this.undo.RemoveLast();
        this.redo.Push(this.current);
        this.MoveTo(last.Value);
        return true;
    }

    /// <summary>Goes forward one step that <see cref="Undo"/> went back.</summary>
    /// <returns><see langword="true"/> when a step was redone; <see langword="false"/> when there was none.</returns>
    public bool Redo()
    {
        if (!this.redo.TryPop(out Snapshot next))
        {
            return false;
        }

        this.undo.AddLast(this.current);
        this.MoveTo(next);
        return true;
    }

    /// <summary>
    /// Formats the text in its own format, as one undo step. The canonical form is one line for rule text and JSON; the
    /// pretty form breaks rule text at <see cref="RulePrinter.PrettyWidth"/> characters and indents JSON by two
    /// spaces. YAML is one node per line in both forms. Rule text uses the operator style of the settings. Notes are kept.
    /// </summary>
    /// <param name="pretty"><see langword="true"/> for the pretty form; <see langword="false"/> for the canonical form.</param>
    /// <returns>
    /// The formatted text, or no text and the diagnostics when the current text does not compile. The text does not change
    /// then: formatting from the last valid tree would drop the edits that broke it.
    /// </returns>
    public ToolResult<string> FormatText(bool pretty)
    {
        if (!this.IsRuleCurrent || this.Rule is null)
        {
            return ToolResults.Failure<string>(this.Diagnostics);
        }

        ToolResult<string> printed = this.PrintCurrent(this.Format, pretty);
        if (!printed.Succeeded)
        {
            return printed;
        }

        this.Apply(this.current with { Text = printed.Value });
        return ToolResults.Success(printed.Value, this.Diagnostics);
    }

    /// <summary>
    /// Saves the document. The extension of the path selects the format (<c>.json</c>, <c>.yaml</c> or <c>.yml</c>,
    /// <c>.rule</c>, <c>.tw</c> or <c>.txt</c>); another extension keeps the current format. Saving in another format
    /// converts the rule first, which needs text that compiles, and the conversion is one undo step. The file is written
    /// as <see cref="OutputWriter"/> writes: UTF-8 with no byte order mark, line feeds and one final newline.
    /// </summary>
    /// <param name="path">The file path, or <see langword="null"/> to save to <see cref="Path"/>.</param>
    /// <returns>The full path of the file, or the diagnostics.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> is <see langword="null"/> and the document has no <see cref="Path"/>.</exception>
    public ToolResult<string> Save(string? path = null)
    {
        string target =
            path ?? this.Path ?? throw new InvalidOperationException("The document has no file; pass the path to save to.");
        RuleFormat format = FormatOf(target) ?? this.Format;
        if (format != this.Format)
        {
            if (!this.IsRuleCurrent || this.Rule is null)
            {
                return ToolResults.Failure<string>(this.Diagnostics);
            }

            ToolResult<string> converted = this.PrintCurrent(format, pretty: false);
            if (!converted.Succeeded)
            {
                return converted;
            }

            this.Apply(new Snapshot(converted.Value, format));
        }

        ToolResult<string> written = OutputWriter.WriteFile(target, this.Text);
        if (written.Succeeded)
        {
            this.Path = written.Value;
            this.saved = this.current;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        return written;
    }

    /// <summary>
    /// Marks the current text as saved, so <see cref="IsDirty"/> is <see langword="false"/> until the next change. A front end
    /// calls this after it wrote the text somewhere other than <see cref="Path"/>, such as into a larger document.
    /// </summary>
    public void MarkSaved()
    {
        this.saved = this.current;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Converts the text to another format, as one undo step. The tree and the notes stay the same; the layout, the spelling
    /// (operator case and aliases) and YAML comments of the old text do not. A conversion needs text that compiles.
    /// </summary>
    /// <param name="format">The format to convert to. The current format changes nothing.</param>
    /// <returns>The converted text, or no text and the diagnostics when the current text does not compile. The text does not change then.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public ToolResult<string> ConvertTo(RuleFormat format)
    {
        return this.ConvertTo(format, pretty: false);
    }

    /// <summary>
    /// Converts the text to another format, or to the pretty form of the same format, as one undo step. The pretty form is a
    /// property of rule text: it wraps the text at the page width of the <c>rule-text-pretty</c> artifact and keeps the notes.
    /// The tree, the notes and the definitions stay the same, as for <see cref="ConvertTo(RuleFormat)"/>.
    /// </summary>
    /// <param name="format">The format to convert to.</param>
    /// <param name="pretty"><see langword="true"/> for the pretty form of rule text; JSON and YAML have one form and ignore it.</param>
    /// <returns>The converted text, or no text and the diagnostics when the current text does not compile. The text does not change then.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public ToolResult<string> ConvertTo(RuleFormat format, bool pretty)
    {
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "The format is not a defined RuleFormat.");
        }

        bool formatted = pretty && format == RuleFormat.Text;
        if (format == this.Format && !formatted)
        {
            return ToolResults.Success(this.Text);
        }

        if (!this.IsRuleCurrent || this.Rule is null)
        {
            return ToolResults.Failure<string>(this.Diagnostics);
        }

        ToolResult<string> converted = this.PrintCurrent(format, formatted);
        if (!converted.Succeeded)
        {
            return converted;
        }

        this.Apply(new Snapshot(converted.Value, format));
        return ToolResults.Success(converted.Value, this.Diagnostics);
    }

    /// <summary>
    /// Puts another rule in place of the text, as one undo step: a structural edit or an accepted rewrite. The rule is
    /// printed in the format of the document with its notes, in the pretty form when the text has more than one line and in
    /// the canonical form otherwise; the layout and spelling of the old text are not kept. The printed text must compile
    /// with the limits of the settings, or the text does not change.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <returns>The new text, or no text and the diagnostics of the printed text when it does not compile.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public ToolResult<string> ReplaceRule(CompiledRule<StubAssignment> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        string text = RulePrinter.Print(
            rule,
            this.Format,
            this.settings,
            pretty: this.Text.Trim().Contains('\n', StringComparison.Ordinal)
        );
        CompilationResult<StubAssignment> check = this.compiler.CompileAs(text, this.Format);
        if (check.Rule is null)
        {
            return ToolResults.Failure<string>(check.Diagnostics);
        }

        this.Apply(this.current with { Text = text });
        return ToolResults.Success(text, this.Diagnostics);
    }

    /// <summary>
    /// Puts the result of a structural edit in place of the text, as one undo step. An edit of a document with
    /// <c>let</c> definitions (<see cref="RuleTreeEdit.Document"/> is set) is printed with its definitions and uses; any
    /// other edit is put in as <see cref="ReplaceRule"/> puts a rule. The layout follows <see cref="ReplaceRule"/>: the
    /// pretty form when the text has more than one line. The printed text must compile, or the text does not change.
    /// </summary>
    /// <param name="edit">An edit that succeeded.</param>
    /// <returns>The new text, or no text and the diagnostics when the edit cannot be printed or the printed text does not compile.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="edit"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="edit"/> was refused: it has no rule.</exception>
    public ToolResult<string> ApplyEdit(RuleTreeEdit edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        if (!edit.Succeeded)
        {
            throw new ArgumentException("A refused edit has no rule to put in.", nameof(edit));
        }

        if (edit.Document is null)
        {
            return this.ReplaceRule(edit.Rule);
        }

        bool pretty = this.Text.Trim().Contains('\n', StringComparison.Ordinal);
        ToolResult<string> printed = this.PrintDefinitions(edit.Document, this.Format, pretty);
        if (!printed.Succeeded)
        {
            return printed;
        }

        CompilationResult<StubAssignment> check = this.compiler.CompileAs(printed.Value, this.Format);
        if (check.Rule is null)
        {
            return ToolResults.Failure<string>(check.Diagnostics);
        }

        this.Apply(this.current with { Text = printed.Value }, check);
        return ToolResults.Success(printed.Value, this.Diagnostics);
    }

    // The format that an extension names, or null for an extension the loader does not know.
    private static RuleFormat? FormatOf(string path)
    {
        FormatDetection detection = FormatDetector.Detect(string.Empty, path);
        return detection.Evidence == FormatEvidence.Extension ? detection.Format : null;
    }

    // Prints the current rule in a format: with its definitions and uses when the text has any, otherwise from the tree.
    private ToolResult<string> PrintCurrent(RuleFormat format, bool pretty)
    {
        return this.Definitions is { Succeeded: true, HasDefinitions: true } read && read.PrintJson() is { } json
            ? this.PrintDefinitions(json, format, pretty)
            : ToolResults.Success(RulePrinter.Print(this.Rule!, format, this.settings, pretty));
    }

    private ToolResult<string> PrintDefinitions(string document, RuleFormat format, bool pretty)
    {
        string? printed = RulePrinter.PrintDefinitions((JsonObject)JsonNode.Parse(document)!, format, this.settings, pretty);
        return printed is null
            ? ToolResults.Failure<string>(
                ToolDiagnosticCodes.WriteBackFailed,
                "The rule cannot be printed with its definitions: a definition body or the rule does not compile on its own."
            )
            : ToolResults.Success(printed);
    }

    // A change made by the author: one undo step, and no redo steps left.
    private void Apply(Snapshot next, CompilationResult<StubAssignment>? precompiled = null)
    {
        if (next == this.current)
        {
            return;
        }

        this.undo.AddLast(this.current);
        if (this.undo.Count > UndoLimit)
        {
            this.undo.RemoveFirst();
        }

        this.redo.Clear();
        this.MoveTo(next, precompiled);
    }

    private void MoveTo(Snapshot next, CompilationResult<StubAssignment>? precompiled = null)
    {
        this.current = next;
        this.Compile(precompiled);
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Compile(CompilationResult<StubAssignment>? precompiled = null)
    {
        this.definitions = null;
        CompilationResult<StubAssignment> result = precompiled ?? this.compiler.CompileAs(this.Text, this.Format);
        this.Diagnostics = result.Diagnostics;
        this.IsRuleCurrent = result.Rule is not null;
        if (result.Rule is not null)
        {
            this.Rule = result.Rule;
        }
    }

    /// <summary>One state of the undo history: the text and its format.</summary>
    private readonly record struct Snapshot(string Text, RuleFormat Format);
}
