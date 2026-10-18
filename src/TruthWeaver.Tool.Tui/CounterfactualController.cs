namespace TruthWeaver.Tool.Tui;

using System.Runtime.CompilerServices;
using TruthWeaver.Analysis;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core;

/// <summary>
/// The logic behind the What would make this True panel: it asks the library for the smallest changes to the assigned
/// term values that give the rule a target value (<see cref="RuleCounterfactual"/>), lays them out as lines, and applies a
/// picked set to the assignment. The search runs on a background task, so the editor stays responsive, and the result of
/// an older search never replaces a newer one. It has no view.
/// </summary>
internal sealed class CounterfactualController
{
    private static readonly ConditionalWeakTable<Workbench, CounterfactualController> Controllers = [];

    private readonly Workbench workbench;
    private int version;

    /// <summary>Initializes a new instance of the <see cref="CounterfactualController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public CounterfactualController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.Lines = [new CounterfactualLine("Not searched yet.", null)];
    }

    /// <summary>Occurs after a search starts, after its result arrives and after the target changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the value the search aims for. It starts at <see cref="TruthValue.True"/>.</summary>
    public TruthValue Target { get; private set; } = TruthValue.True;

    /// <summary>Gets a value indicating whether a search runs.</summary>
    public bool IsSearching { get; private set; }

    /// <summary>Gets the lines of the panel: a header, then a message or one line for each change set.</summary>
    public IReadOnlyList<CounterfactualLine> Lines { get; private set; }

    /// <summary>Gets the controller of a workbench. The panel and the tests use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static CounterfactualController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new CounterfactualController(key));
    }

    /// <summary>Gets the words for one change set, such as <c>a: False -> True, b: (not read) -> True</c>.</summary>
    /// <param name="set">The change set.</param>
    /// <returns>The text.</returns>
    public static string Describe(ChangeSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        return string.Join(", ", set.Changes.Select(c => $"{c.Term}: {c.From?.ToString() ?? "(not read)"} -> {c.To}"));
    }

    /// <summary>
    /// Searches for the change sets that give the target value under the current rule and values. The search runs on a
    /// background task; the lines change when it ends, on the thread that <see cref="Workbench.Post"/> posts to.
    /// </summary>
    /// <returns>A task that ends when the result is applied, or at once when no search is needed.</returns>
    public Task RefreshAsync()
    {
        int mine = ++this.version;
        AssignmentOutcome outcome = this.workbench.Outcome;
        if (this.workbench.Document.Rule is not { } rule || outcome.Decision is not { } decision)
        {
            this.Show(mine, [Header(this.Target), new CounterfactualLine(outcome.Reason, null)]);
            return Task.CompletedTask;
        }

        this.IsSearching = true;
        this.Lines = [Header(this.Target), new CounterfactualLine("Searching...", null)];
        this.Changed?.Invoke(this, EventArgs.Empty);

        TruthValue target = this.Target;
        TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Task.Run(() =>
        {
            IReadOnlyList<CounterfactualLine> lines = Search(rule, decision, target);
            this.workbench.Post(() =>
            {
                this.Show(mine, lines);
                done.SetResult();
            });
        });
        return done.Task;
    }

    /// <summary>Starts a search and does not wait for it (see <see cref="RefreshAsync"/>).</summary>
    public void Refresh()
    {
        _ = this.RefreshAsync();
    }

    /// <summary>Moves the target and starts a search, without waiting for it (see <see cref="CycleTargetAsync"/>).</summary>
    public void CycleTarget()
    {
        _ = this.CycleTargetAsync();
    }

    /// <summary>Moves the target to the next value: <c>True</c>, <c>False</c>, <c>Unknown</c>, and searches again.</summary>
    /// <returns>A task that ends when the new result is applied.</returns>
    public Task CycleTargetAsync()
    {
        this.Target = this.Target switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.Unknown,
            _ => TruthValue.True,
        };
        return this.RefreshAsync();
    }

    /// <summary>Gives the terms of a change set their new values in the assignment, so the decision follows.</summary>
    /// <param name="set">The change set.</param>
    /// <returns><see langword="true"/> when the values were set. A term that takes its value from a <c>from(...)</c> reference cannot be set; nothing changes then and the status line says why.</returns>
    public bool Apply(ChangeSet set)
    {
        ArgumentNullException.ThrowIfNull(set);

        IReadOnlyList<TermAssignment> terms = this.workbench.Outcome.Terms;
        List<(string Key, TruthValue Value)> sets = [];
        foreach (TermChange change in set.Changes)
        {
            TermAssignment? term = terms.FirstOrDefault(t => t.Key == change.Term || t.Display == change.Term);
            string? key = term is null ? change.Term : term.Key;
            if (key is null)
            {
                this.workbench.SetStatus(
                    $"Not applied: '{change.Term}' takes its value from a from(...) reference, so it cannot be set here."
                );
                return false;
            }

            sets.Add((key, change.To));
        }

        foreach ((string key, TruthValue value) in sets)
        {
            this.workbench.Assignment.Set(key, value);
        }

        this.workbench.NotifyAssignmentChanged();
        this.workbench.SetStatus($"Applied: {Describe(set)}.");
        return true;
    }

    private static CounterfactualLine Header(TruthValue target)
    {
        return new CounterfactualLine(
            $"Smallest changes that make the rule {target}. Enter applies a change, t changes the target, r searches again.",
            null
        );
    }

    // Runs on a background thread: the library search reads only the rule and the decision, which are immutable.
    private static List<CounterfactualLine> Search(CompiledRule<StubAssignment> rule, Decision decision, TruthValue target)
    {
        List<CounterfactualLine> lines = [Header(target)];
        CounterfactualResult result;
        try
        {
            result = RuleCounterfactual.FindChanges(rule, decision, new CounterfactualOptions(Target: target));
        }
        catch (ArgumentException exception)
        {
            lines.Add(new CounterfactualLine($"The search cannot read the decision: {exception.Message}", null));
            return lines;
        }

        switch (result.Outcome)
        {
            case CounterfactualOutcome.NoChangeNeeded:
                lines.Add(new CounterfactualLine($"The decision is already {target}. No change is needed.", null));
                break;
            case CounterfactualOutcome.NonePossible:
                lines.Add(new CounterfactualLine($"No change of the terms gives {target}.", null));
                break;
            case CounterfactualOutcome.Undecided:
                lines.Add(new CounterfactualLine($"Not searched: {result.Reason}", null));
                break;
            default:
                lines.AddRange(result.ChangeSets.Select(set => new CounterfactualLine(Describe(set), set)));
                if (!result.IsComplete)
                {
                    lines.Add(new CounterfactualLine(Incomplete(result.ChangeSets.Count), null));
                }

                break;
        }

        return lines;
    }

    // The library gives no reason for an incomplete result, so the count tells the two limits apart.
    private static string Incomplete(int count)
    {
        return count >= RuleCounterfactual.DefaultMaxChangeSets
            ? $"Showing the {count} smallest sets; more exist."
            : "Work limit reached: the search stopped before it checked every change. The sets shown are the smallest found; others may exist.";
    }

    // A result applies only when no newer search started since.
    private void Show(int mine, IReadOnlyList<CounterfactualLine> lines)
    {
        if (mine != this.version)
        {
            return;
        }

        this.IsSearching = false;
        this.Lines = lines;
        this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
