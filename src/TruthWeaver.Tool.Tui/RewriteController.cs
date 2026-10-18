namespace TruthWeaver.Tool.Tui;

using System.Globalization;
using System.Runtime.CompilerServices;
using TruthWeaver.Analysis;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core;

/// <summary>
/// The logic behind the Rewrite pane, with no view of its own: it applies a rewrite to the rule and keeps the result as a
/// preview until the author accepts it into the document (one undo step) or discards it, and it compares the rule with the
/// last saved or accepted version.
/// </summary>
internal sealed class RewriteController
{
    /// <summary>The text of the pane before a rewrite or a comparison.</summary>
    public const string Idle =
        "No rewrite yet. Press r here, or Ctrl+R anywhere, to choose a rewrite; c here, or Ctrl+G, to compare the rule with the last saved or accepted version.";

    private static readonly ConditionalWeakTable<Workbench, RewriteController> Controllers = [];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;
    private RuleDocument? document;
    private (string Earlier, string Now) compared = (string.Empty, string.Empty);

    /// <summary>Initializes a new instance of the <see cref="RewriteController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public RewriteController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
        this.TakeSavedVersion();
        workbench.DocumentChanged += (_, _) => this.TakeSavedVersion();
    }

    /// <summary>Occurs after a rewrite, an accept, a discard or a comparison.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the rewrite that waits to be accepted or discarded, or <see langword="null"/>.</summary>
    public RewritePreview? Preview { get; private set; }

    /// <summary>Gets the last comparison, or <see langword="null"/>.</summary>
    public RuleComparison? Comparison { get; private set; }

    /// <summary>
    /// Gets the version of the rule that <see cref="Compare"/> compares with: the rule when the document was opened or last
    /// saved, or the last accepted rewrite, whichever came last. It is <see langword="null"/> until one of them compiled.
    /// </summary>
    public CompiledRule<StubAssignment>? Baseline { get; private set; }

    /// <summary>Gets the lines of the pane.</summary>
    public IReadOnlyList<string> Lines
    {
        get
        {
            List<string> lines = [];
            if (this.Preview is { } preview)
            {
                AddPreview(lines, preview);
            }

            if (this.Comparison is { } comparison)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                this.AddComparison(lines, comparison);
            }

            return lines.Count == 0 ? [Idle] : lines;
        }
    }

    private WorkbenchServices Services => this.services ?? WorkbenchServices.Current;

    /// <summary>Gets the controller of a workbench. The pane and the keys use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs for a new controller, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    /// <returns>The controller.</returns>
    public static RewriteController For(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new RewriteController(key, services));
    }

    /// <summary>Asks which rewrite to apply and applies it.</summary>
    /// <returns><see langword="true"/> when a rewrite ran.</returns>
    public bool Choose()
    {
        RuleRewriteKind[] kinds = Enum.GetValues<RuleRewriteKind>();
        int? choice = this.Services.Dialogs.Choose("Rewrite the rule", [.. kinds.Select(RewritePlayground.NameOf)]);
        return choice is int index && this.Run(kinds[index]);
    }

    /// <summary>Applies a rewrite to the rule and keeps the result as the preview. The document does not change.</summary>
    /// <param name="kind">The rewrite.</param>
    /// <returns><see langword="true"/> when the rewrite ran.</returns>
    public bool Run(RuleRewriteKind kind)
    {
        if (!this.workbench.RequireCurrentRule("rewrite the rule"))
        {
            return false;
        }

        RewritePreview preview = RewritePlayground.Preview(this.workbench.Document.Rule!, kind, this.workbench.Settings);
        this.Preview = preview;
        string name = RewritePlayground.NameOf(kind);
        Diagnostic? error = preview.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        string status = preview switch
        {
            { After: null } =>
                $"{name} gave no rule: {(error is null ? "see the Rewrite pane." : $"{error.Code}: {error.Message}")}",
            { Changed: false } => $"{name} does not change the rule.",
            _ => $"{name}: see the Rewrite pane. Press a there to accept, or x to discard.",
        };
        this.workbench.SetStatus(status);
        this.Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Puts the rewritten rule into the document as one undo step and makes it the version to compare with. A rule that
    /// changed after the rewrite ran is not replaced: the rewrite runs again first.
    /// </summary>
    /// <returns><see langword="true"/> when the document changed.</returns>
    public bool Accept()
    {
        if (this.Preview is not { After: { } after } preview)
        {
            this.workbench.SetStatus("Nothing to accept: run a rewrite that gives a rule first.");
            return false;
        }

        if (!ReferenceEquals(preview.Before, this.workbench.Document.Rule) || !this.workbench.Document.IsRuleCurrent)
        {
            this.workbench.SetStatus("Not accepted: the rule changed after the rewrite ran. Run the rewrite again.");
            return false;
        }

        string name = RewritePlayground.NameOf(preview.Kind);
        if (!this.workbench.ReplaceRule(after, $"Accepted {name}. Ctrl+Z undoes it."))
        {
            return false;
        }

        this.Preview = null;
        this.Baseline = this.workbench.Document.Rule;
        this.Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Drops the preview. The document does not change.</summary>
    public void Discard()
    {
        this.workbench.SetStatus(this.Preview is null ? "Nothing to discard." : "Discarded the rewrite.");
        this.Preview = null;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Compares the rule with the last saved or accepted version.</summary>
    /// <returns><see langword="true"/> when the comparison ran.</returns>
    public bool Compare()
    {
        if (!this.workbench.RequireCurrentRule("compare the rule"))
        {
            return false;
        }

        if (this.Baseline is not { } baseline)
        {
            this.workbench.SetStatus("Nothing to compare with: save the rule or accept a rewrite first.");
            return false;
        }

        CompiledRule<StubAssignment> current = this.workbench.Document.Rule!;
        this.Comparison = RewritePlayground.Compare(baseline, current, this.workbench.Settings);
        this.compared = (baseline.CanonicalText, current.CanonicalText);
        this.workbench.SetStatus(
            "Compared with the last saved or accepted version: " + Verdict(this.Comparison.Equivalence) + "."
        );
        this.Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static string Verdict(RuleEquivalenceResult equivalence)
    {
        return equivalence.Outcome switch
        {
            RuleEquivalenceOutcome.Equivalent => "equivalent",
            RuleEquivalenceOutcome.NotEquivalent => "not equivalent",
            _ => "undecided",
        };
    }

    private static string Assignment(IReadOnlyDictionary<string, TruthValue>? values)
    {
        return values is null
            ? string.Empty
            : string.Join(", ", values.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => $"{v.Key} = {v.Value}"));
    }

    private static void AddIndented(List<string> lines, string text)
    {
        lines.AddRange(text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => "  " + line));
    }

    private static void AddPreview(List<string> lines, RewritePreview preview)
    {
        lines.Add($"Rewrite: {RewritePlayground.NameOf(preview.Kind)}   a: accept   x: discard");
        if (preview.Diagnostics.Count == 0)
        {
            lines.Add("Diagnostics: none");
        }
        else
        {
            lines.Add("Diagnostics:");
            lines.AddRange(
                preview.Diagnostics.Select(d => $"  {d.Code} {d.Severity.ToString().ToLowerInvariant()}: {d.Message}")
            );
        }

        RuleMetrics before = preview.Before.Metrics;
        if (preview.After is not { } after)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"Size: {before.NodeCount} nodes, depth {before.Depth}"));
            lines.Add("Before:");
            AddIndented(lines, preview.BeforeText);
            lines.Add("After: no rule");
            return;
        }

        RuleMetrics metrics = after.Metrics;
        lines.Add(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Size: {before.NodeCount} -> {metrics.NodeCount} nodes, depth {before.Depth} -> {metrics.Depth}, {before.DistinctTermCount} -> {metrics.DistinctTermCount} terms"
            )
        );
        lines.Add(preview.Changed ? "Before:" : "Before (the rewrite does not change the rule):");
        AddIndented(lines, preview.BeforeText);
        lines.Add("After:");
        AddIndented(lines, preview.AfterText!);
        if (preview.Kind == RuleRewriteKind.Simplify)
        {
            lines.Add(preview.Steps.Count == 0 ? "Steps: none, the rule is already simple" : "Steps:");
            for (int i = 0; i < preview.Steps.Count; i++)
            {
                RewriteStep step = preview.Steps[i];
                lines.Add(
                    string.Create(CultureInfo.InvariantCulture, $"  {i + 1}. {step.Law}: {step.Before}  ->  {step.After}")
                );
            }
        }

        lines.Add("Tree before:");
        AddIndented(lines, preview.BeforeTree);
        lines.Add("Tree after:");
        AddIndented(lines, preview.AfterTree!);
    }

    private void AddComparison(List<string> lines, RuleComparison comparison)
    {
        lines.Add("Compare with the last saved or accepted version");
        RuleEquivalenceResult equivalence = comparison.Equivalence;
        lines.Add(
            equivalence.Outcome switch
            {
                RuleEquivalenceOutcome.Equivalent => "Equivalent: yes, the same value for every assignment",
                RuleEquivalenceOutcome.NotEquivalent => "Equivalent: no. They differ for "
                    + Assignment(equivalence.CounterExample),
                _ => "Equivalent: undecided. " + equivalence.Reason,
            }
        );
        RuleRelationResult relation = comparison.Relation;
        lines.Add(
            relation.Relation switch
            {
                RuleRelation.Equivalent => "Satisfied by: the same assignments",
                RuleRelation.Tighter => "Satisfied by: fewer assignments (tighter). Only the earlier version: "
                    + Assignment(relation.SatisfiesOnlyBefore),
                RuleRelation.Looser => "Satisfied by: more assignments (looser). Only the current rule: "
                    + Assignment(relation.SatisfiesOnlyAfter),
                RuleRelation.Overlapping => "Satisfied by: overlapping assignments, each rule has some the other has not",
                RuleRelation.Disjoint => "Satisfied by: no assignment satisfies both",
                _ => "Satisfied by: undecided. " + relation.Reason,
            }
        );
        lines.Add("Earlier:");
        AddIndented(lines, this.compared.Earlier);
        lines.Add("Now:");
        AddIndented(lines, this.compared.Now);
    }

    // The rule when the document was opened or saved is the version to compare with; an accepted rewrite also is, until the
    // next save.
    private void TakeSavedVersion()
    {
        RuleDocument current = this.workbench.Document;
        if (!ReferenceEquals(current, this.document))
        {
            // Another document is open: the versions of the old one mean nothing for it.
            this.document = current;
            this.Baseline = null;
            this.Preview = null;
            this.Comparison = null;
        }

        if (!current.IsDirty && current.IsRuleCurrent && current.Rule is not null)
        {
            this.Baseline = current.Rule;
        }
    }
}
