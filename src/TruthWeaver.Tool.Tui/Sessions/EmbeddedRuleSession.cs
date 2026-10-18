namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Patching;

/// <summary>
/// One rule that lives inside a larger JSON or YAML document: the file, the path of the rule, and what the rule looked like
/// when it was read. It has no view. It reads the rule, lists the other rules of the document, and writes an edited rule back
/// with <see cref="RulePatcher"/>: by default only the rule's own span changes, so every byte outside it stays.
/// </summary>
internal sealed class EmbeddedRuleSession
{
    private LoadedRule baseline;

    private EmbeddedRuleSession(string filePath, LoadedRule baseline)
    {
        this.FilePath = filePath;
        this.baseline = baseline;
    }

    /// <summary>Gets the full path of the document file.</summary>
    public string FilePath { get; }

    /// <summary>Gets the normalized path of the rule in the document, such as <c>$.rules.publish</c>.</summary>
    public string RulePath => this.baseline.Path!;

    /// <summary>Gets the format of the rule: rule text for a string, otherwise the format of the document.</summary>
    public RuleFormat RuleFormat => this.baseline.Format;

    /// <summary>Gets the name of the rule for the status line: the file name and the path.</summary>
    public string Label => $"{Path.GetFileName(this.FilePath)}:{this.RulePath}";

    /// <summary>Reads the rule at a path of a document file.</summary>
    /// <param name="filePath">The document file.</param>
    /// <param name="rulePath">The path of the rule. It must name one value.</param>
    /// <returns>The session, or the diagnostics: the path selects nothing, the value is not a rule, or the file cannot be read.</returns>
    public static ToolResult<EmbeddedRuleSession> Open(string filePath, string rulePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(rulePath);

        string full = Path.GetFullPath(filePath);
        ToolResult<LoadedRule> loaded = RuleLoader.LoadFile(full, new RuleLoadOptions { Path = rulePath });
        return loaded.TryGetValue(out LoadedRule? rule, out EquatableArray<Diagnostic> diagnostics)
            ? Results.Success(new EmbeddedRuleSession(full, rule))
            : Results.Failure<EmbeddedRuleSession>(diagnostics);
    }

    /// <summary>Lists the places of the document where a rule can be, read from the file now.</summary>
    /// <param name="filePath">The document file.</param>
    /// <returns>The entries, or the diagnostics when the file cannot be read or is not a JSON or YAML document.</returns>
    public static ToolResult<IReadOnlyList<DocumentNode>> Outline(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        return DocumentOutline.Read(Path.GetFullPath(filePath));
    }

    /// <summary>Creates the editable document of the rule, with its own text.</summary>
    /// <param name="settings">The settings.</param>
    /// <returns>The document. It has no file of its own: the session saves it.</returns>
    public RuleDocument CreateDocument(ToolSettings settings)
    {
        return RuleDocument.Create(this.baseline.Text, this.baseline.Format, settings);
    }

    /// <summary>
    /// Computes what writing an edited rule back would give, without writing it. The document is read again, so the patch
    /// applies to the file as it is now.
    /// </summary>
    /// <param name="edited">The edited document. Its text must compile.</param>
    /// <param name="mode">How to write the rule back.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The patch and what it does, or the diagnostics.</returns>
    public ToolResult<PreparedWrite> Prepare(RuleDocument edited, WriteBackMode mode, ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(edited);
        ArgumentNullException.ThrowIfNull(settings);

        if (!edited.IsRuleCurrent || edited.Rule is not { } rule)
        {
            return Results.Failure<PreparedWrite>(edited.Diagnostics);
        }

        ToolResult<LoadedRule> current = RuleLoader.LoadFile(this.FilePath, new RuleLoadOptions { Path = this.RulePath });
        if (!current.TryGetValue(out LoadedRule? fresh, out EquatableArray<Diagnostic> diagnostics))
        {
            return Results.Failure<PreparedWrite>(diagnostics);
        }

        ToolResult<DocumentPatch> patch = RulePatcher.Patch(fresh, rule, mode, settings);
        if (!patch.TryGetValue(out DocumentPatch? value, out diagnostics))
        {
            return Results.Failure<PreparedWrite>(diagnostics);
        }

        bool changedOnDisk = fresh.Text != this.baseline.Text;
        return Results.Success(new PreparedWrite(value, fresh, changedOnDisk), patch.Diagnostics);
    }

    /// <summary>Writes a prepared patch to the document file, in the encoding and with the line endings of the file.</summary>
    /// <param name="write">The prepared write.</param>
    /// <returns>The full path of the file, or the diagnostics. After a write, the session treats the written rule as the one in the file.</returns>
    public ToolResult<string> Commit(PreparedWrite write)
    {
        ArgumentNullException.ThrowIfNull(write);

        ToolResult<string> written = RulePatcher.WriteFile(this.FilePath, write.Patch);
        if (
            written.Succeeded
            && RuleLoader.LoadFile(this.FilePath, new RuleLoadOptions { Path = this.RulePath }).Value is { } reread
        )
        {
            this.baseline = reread;
        }

        return written;
    }

    /// <summary>A patch that is ready to write, and what it will do.</summary>
    /// <param name="Patch">The patched document.</param>
    /// <param name="Current">The rule as the file has it now.</param>
    /// <param name="ChangedOnDisk">Whether the rule in the file differs from the rule when the session read it.</param>
    internal sealed record PreparedWrite(DocumentPatch Patch, LoadedRule Current, bool ChangedOnDisk)
    {
        /// <summary>Gets the kind of change the patch makes to the single rule.</summary>
        public RulePatchKind Kind => this.Patch.Rules[0].Kind;
    }
}
