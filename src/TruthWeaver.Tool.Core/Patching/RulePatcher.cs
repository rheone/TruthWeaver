namespace TruthWeaver.Tool.Core.Patching;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;

/// <summary>
/// Writes edited rules back into their document in the document's own format: rule text, JSON or YAML. In the default
/// <see cref="WriteBackMode.Patch"/> mode only the values that changed are replaced, inside the rule's own span, so
/// comments, key order, indentation and every byte outside the rule stay as they were. Notes are values like any other.
/// </summary>
/// <remarks>
/// <para>
/// The patcher compares the tree-format form of the rule in the document with the edited rule. When the two have the same
/// shape (the same nodes, keys and operand counts) and each changed value is a plain, quoted or JSON scalar in the
/// document, each value is replaced in place. When the shape changed, or a value cannot be replaced by itself (a YAML block
/// scalar, alias, anchor or tag), or the result does not read back as the edited rule, the patcher writes the rule's whole
/// span again in the document's format and reports it with a <see cref="ToolDiagnosticCodes.RuleRewritten"/> warning. Rule
/// text has no positions for its parts, so a changed rule text is always written again. Every result is read back and
/// compiled before it is returned.
/// </para>
/// <para>
/// <see cref="WriteBackMode.RoundTrip"/> serializes the whole document again instead, with a
/// <see cref="ToolDiagnosticCodes.RoundTripWriteBack"/> warning.
/// </para>
/// </remarks>
public static class RulePatcher
{
    /// <summary>Writes one edited rule back into the document it was loaded from.</summary>
    /// <typeparam name="TContext">The context type of the edited rule.</typeparam>
    /// <param name="rule">The rule as it was loaded, which names the document, its format and the rule's path.</param>
    /// <param name="edited">The edited rule.</param>
    /// <param name="mode">How to write the rule back; <see cref="WriteBackMode.Patch"/> by default.</param>
    /// <param name="settings">The settings for compiling, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The new document text and the outcome, or the diagnostics when the rule cannot be written back.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="edited"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined <see cref="WriteBackMode"/>.</exception>
    public static ToolResult<DocumentPatch> Patch<TContext>(
        LoadedRule rule,
        CompiledRule<TContext> edited,
        WriteBackMode mode = WriteBackMode.Patch,
        ToolSettings? settings = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(edited);

        return Patch(rule.Source, rule.Detection.Format, [new RuleEdit<TContext>(rule.Path, edited)], mode, settings);
    }

    /// <summary>
    /// Writes several edited rules back into one document. The edits apply in order, each one to the text the previous one
    /// gave, so the rules patch independently. When one edit fails, the result has no text.
    /// </summary>
    /// <typeparam name="TContext">The context type of the edited rules.</typeparam>
    /// <param name="document">The document as <see cref="SourceReader"/> read it.</param>
    /// <param name="format">The format of the document.</param>
    /// <param name="edits">The edits.</param>
    /// <param name="mode">How to write the rules back; <see cref="WriteBackMode.Patch"/> by default.</param>
    /// <param name="settings">The settings for compiling, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The new document text and one outcome for each edit, or the diagnostics when an edit cannot be written back.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> or <paramref name="edits"/> is <see langword="null"/>, or an edit is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="mode"/> is not defined.</exception>
    public static ToolResult<DocumentPatch> Patch<TContext>(
        SourceText document,
        RuleFormat format,
        IEnumerable<RuleEdit<TContext>> edits,
        WriteBackMode mode = WriteBackMode.Patch,
        ToolSettings? settings = null
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(edits);
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, "The format is not a defined RuleFormat.");
        }

        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The mode is not a defined WriteBackMode.");
        }

        List<RuleEdit<TContext>> list = [.. edits];
        if (list.Any(edit => edit is null))
        {
            throw new ArgumentNullException(nameof(edits), "An edit is null.");
        }

        WriteBack context = new(document, format, settings);
        return mode == WriteBackMode.RoundTrip ? RoundTrip(context, list) : PatchInPlace(context, list);
    }

    /// <summary>Encodes the text of a patched document in the encoding of the original, with its byte order mark.</summary>
    /// <param name="patch">The patched document.</param>
    /// <returns>The bytes to write.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patch"/> is <see langword="null"/>.</exception>
    public static byte[] Encode(DocumentPatch patch)
    {
        ArgumentNullException.ThrowIfNull(patch);

        byte[] mark = patch.Encoding switch
        {
            SourceEncoding.Utf8WithBom => [0xEF, 0xBB, 0xBF],
            SourceEncoding.Utf16LittleEndian => [0xFF, 0xFE],
            SourceEncoding.Utf16BigEndian => [0xFE, 0xFF],
            SourceEncoding.Utf32LittleEndian => [0xFF, 0xFE, 0x00, 0x00],
            SourceEncoding.Utf32BigEndian => [0x00, 0x00, 0xFE, 0xFF],
            _ => [],
        };
        return [.. mark, .. SourceReader.StrictEncoding(patch.Encoding).GetBytes(patch.Text)];
    }

    /// <summary>
    /// Writes a patched document to a file, in the encoding of the original and with its line endings, so the bytes outside
    /// the patched values are the bytes of the original file.
    /// </summary>
    /// <param name="path">The file path, usually the file the document was read from.</param>
    /// <param name="patch">The patched document.</param>
    /// <returns>The full path of the file, or a <see cref="ToolDiagnosticCodes.OutputFailed"/> error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="patch"/> is <see langword="null"/>.</exception>
    public static ToolResult<string> WriteFile(string path, DocumentPatch patch)
    {
        ArgumentNullException.ThrowIfNull(path);

        return OutputWriter.WriteBytes(path, Encode(patch));
    }

    private static ToolResult<DocumentPatch> PatchInPlace<TContext>(WriteBack context, List<RuleEdit<TContext>> edits)
    {
        string text = context.Document.Text;
        List<RulePatchOutcome> outcomes = [];
        foreach (RuleEdit<TContext> edit in edits)
        {
            if (!context.TryLoad(text, edit.Path, out LoadedRule? loaded))
            {
                return context.Failure();
            }

            PrintedRule printed = PrintedRule.Of(edit.Rule);
            string target = printed.Json;
            CompiledRule<StubAssignment>? original = RuleConverter.Compile(loaded, context.Settings).Rule;
            string? originalTree = original?.PrintJson(includeNotes: true);
            if (originalTree == target)
            {
                outcomes.Add(new RulePatchOutcome(loaded.Path, RulePatchKind.Unchanged));
                continue;
            }

            string? reason;
            string? patched = null;
            if (loaded.Format == RuleFormat.Text)
            {
                reason = "it is rule text, which has no positions for its parts";
            }
            else if (originalTree is null)
            {
                reason = "the rule in the document does not compile, so the changed values cannot be found";
            }
            else
            {
                patched = ValuePatch.TryApply(text, context.Format, loaded.Path ?? "$", originalTree, target, out reason);
                if (patched is not null && !context.ReadsBackAs(patched, loaded.Path, target))
                {
                    patched = null;
                    reason = "the patched values do not read back as the edited rule";
                }
            }

            if (patched is not null)
            {
                text = patched;
                outcomes.Add(new RulePatchOutcome(loaded.Path, RulePatchKind.Patched));
                continue;
            }

            string? rewritten = RuleRewrite.TryApply(text, context.Format, loaded, printed, out string? problem);
            if (rewritten is null || !context.ReadsBackAs(rewritten, loaded.Path, target))
            {
                string failure =
                    $"The rule at '{loaded.Path ?? "$"}' cannot be written back into '{context.Document.Origin}': "
                    + (problem ?? "the written document does not read back as the edited rule.")
                    + " Use the round-trip mode, or edit the document by hand.";
                context.Add(ToolResults.Error(ToolDiagnosticCodes.WriteBackFailed, failure, loaded.Path));
                return context.Failure();
            }

            string warning =
                $"The rule at '{loaded.Path ?? "$"}' was written again in full, because {reason}. Comments and layout "
                + "inside the rule are not kept; the text outside the rule is unchanged.";
            context.Add(ToolResults.Warning(ToolDiagnosticCodes.RuleRewritten, warning, loaded.Path));
            text = rewritten;
            outcomes.Add(new RulePatchOutcome(loaded.Path, RulePatchKind.RuleRewritten));
        }

        return context.Success(text, outcomes);
    }

    private static ToolResult<DocumentPatch> RoundTrip<TContext>(WriteBack context, List<RuleEdit<TContext>> edits)
    {
        string original = context.Document.Text;
        List<(LoadedRule Loaded, PrintedRule Rule)> loaded = [];
        foreach (RuleEdit<TContext> edit in edits)
        {
            if (!context.TryLoad(original, edit.Path, out LoadedRule? rule))
            {
                return context.Failure();
            }

            loaded.Add((rule, PrintedRule.Of(edit.Rule)));
        }

        string? text = context.Format switch
        {
            RuleFormat.Json => DocumentRewrite.Json(original, loaded),
            RuleFormat.Yaml => DocumentRewrite.Yaml(original, loaded),
            _ => loaded.Count == 0 ? original : RuleRewrite.ReplaceText(original, loaded[^1].Rule.Text),
        };

        bool readsBack = text is not null && loaded.All(item => context.ReadsBackAs(text, item.Loaded.Path, item.Rule.Json));
        if (!readsBack)
        {
            string failure =
                $"The document '{context.Document.Origin}' cannot be written again with the edited rules: the written "
                + "document does not read back as the edited rules.";
            context.Add(ToolResults.Error(ToolDiagnosticCodes.WriteBackFailed, failure));
            return context.Failure();
        }

        string warning =
            $"The document '{context.Document.Origin}' was serialized again in full (round-trip mode). YAML comments, "
            + "key layout, quoting and indentation can be lost; the patch mode keeps them.";
        context.Add(ToolResults.Warning(ToolDiagnosticCodes.RoundTripWriteBack, warning));
        return context.Success(
            text!,
            loaded.Select(item => new RulePatchOutcome(item.Loaded.Path, RulePatchKind.DocumentRewritten))
        );
    }

    /// <summary>The state of one write-back: the document, its format, the settings and the diagnostics so far.</summary>
    private sealed class WriteBack(SourceText document, RuleFormat format, ToolSettings? settings)
    {
        private readonly List<Diagnostic> diagnostics = [];

        public SourceText Document { get; } = document;

        public RuleFormat Format { get; } = format;

        public ToolSettings? Settings { get; } = settings;

        public void Add(Diagnostic diagnostic)
        {
            this.diagnostics.Add(diagnostic);
        }

        // Loads the rule at 'path' from the current text, the same way RuleLoader loads it for an edit.
        public bool TryLoad(string text, string? path, [NotNullWhen(true)] out LoadedRule? loaded)
        {
            RuleLoadOptions options = new() { Format = this.Format, Path = path };
            if (
                RuleLoader
                    .LoadString(text, this.Document.Origin, options)
                    .TryGetValue(out loaded, out EquatableArray<Diagnostic> found)
            )
            {
                return true;
            }

            this.diagnostics.AddRange(found);
            return false;
        }

        // The written text holds, at the path, a rule that compiles to the edited tree with the edited notes.
        public bool ReadsBackAs(string text, string? path, string target)
        {
            RuleLoadOptions options = new() { Format = this.Format, Path = path };
            return RuleLoader.LoadString(text, this.Document.Origin, options).TryGetValue(out LoadedRule? loaded, out _)
                && RuleConverter.Compile(loaded, this.Settings).Rule?.PrintJson(includeNotes: true) == target;
        }

        public ToolResult<DocumentPatch> Success(string text, IEnumerable<RulePatchOutcome> outcomes)
        {
            DocumentPatch patch = new(text, this.Document.Encoding, new EquatableArray<RulePatchOutcome>(outcomes));
            return ToolResults.Success(patch, this.diagnostics);
        }

        public ToolResult<DocumentPatch> Failure()
        {
            return ToolResults.Failure<DocumentPatch>(this.diagnostics);
        }
    }
}
