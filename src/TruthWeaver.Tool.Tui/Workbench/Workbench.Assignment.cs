namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;

/// <summary>The part of the workbench state that tries the rule with values: the assignment, the decision and the trace.</summary>
internal sealed partial class Workbench
{
    private AssignmentSession? assignment;
    private AssignmentOutcome? outcome;

    /// <summary>
    /// Occurs after the assignment changed (a term value, the variables or the policy) and after a change of the document
    /// made the outcome stale. A pane that shows the outcome subscribes here and to <see cref="DocumentChanged"/>.
    /// </summary>
    public event EventHandler? AssignmentChanged;

    /// <summary>
    /// Gets the values the rule is tried with. After a change, call <see cref="NotifyAssignmentChanged"/> so the outcome
    /// and the panes follow.
    /// </summary>
    public AssignmentSession Assignment
    {
        get
        {
            if (this.assignment is null)
            {
                this.assignment = new AssignmentSession(this.Settings);

                // First in line of the document events, so every pane that reads Outcome after this sees a fresh one.
                this.DocumentChanged += (_, _) => this.outcome = null;
            }

            return this.assignment;
        }
    }

    /// <summary>
    /// Gets the decision, trace and terms of the rule of the document under <see cref="Assignment"/>. It is computed again
    /// after each change of the document or the assignment. A pane must read <see cref="Assignment"/> before it subscribes
    /// to <see cref="DocumentChanged"/> (this property does so), so the outcome is never stale in its handler.
    /// </summary>
    public AssignmentOutcome Outcome
    {
        get
        {
            _ = this.Assignment;

            // VSTHRD002 (blocking on a task) cannot deadlock here: the stand-in predicates and the in-memory data answer at
            // once, so the task is already complete, and the Terminal.Gui loop has no synchronization context to wait for.
#pragma warning disable VSTHRD002
            return this.outcome ??= this.Assignment.EvaluateAsync(this.Document).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }
    }

    /// <summary>Gets the words for a policy in the decision line.</summary>
    /// <param name="policy">The policy.</param>
    /// <returns>The text.</returns>
    public static string PolicyText(CollapsePolicy policy)
    {
        return policy switch
        {
            CollapsePolicy.UnknownAsFalse => "Unknown counts as False (fail-closed)",
            CollapsePolicy.UnknownAsTrue => "Unknown counts as True (fail-open)",
            _ => "Unknown is an error (rejected)",
        };
    }

    /// <summary>Tells the panes that the assignment changed, so they read <see cref="Outcome"/> again.</summary>
    public void NotifyAssignmentChanged()
    {
        this.outcome = null;
        this.AssignmentChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gives every term the value <c>Unknown</c>.</summary>
    public void ClearAssignment()
    {
        this.Assignment.ClearValues();
        this.NotifyAssignmentChanged();
        this.SetStatus("Every term is Unknown.");
    }

    /// <summary>Moves the policy that the decision is collapsed under to the next one.</summary>
    public void CyclePolicy()
    {
        this.SetStatus($"Policy: {PolicyText(this.Assignment.CyclePolicy())}.");
        this.NotifyAssignmentChanged();
    }

    /// <summary>Sets the policy that the decision is collapsed under, as the policy choice of the Rule menu does.</summary>
    /// <param name="policy">The policy.</param>
    public void SetPolicy(CollapsePolicy policy)
    {
        this.Assignment.Policy = policy;
        this.SetStatus($"Policy: {PolicyText(policy)}.");
        this.NotifyAssignmentChanged();
    }

    /// <summary>Asks for a JSON or YAML file and uses it as the variables of the <c>from(...)</c> references.</summary>
    /// <returns><see langword="true"/> when a file was loaded.</returns>
    public bool LoadVariables()
    {
        if (this.Prompts.AskOpenPath() is not { } path)
        {
            return false;
        }

        return this.LoadVariablesFile(path);
    }

    /// <summary>Uses a JSON or YAML file as the variables of the <c>from(...)</c> references, without asking.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the file was read; otherwise the status line says why not.</returns>
    public bool LoadVariablesFile(string path)
    {
        ToolResult<SourceText> read = this.Assignment.LoadVariablesFile(path);
        if (!read.Succeeded)
        {
            this.SetStatus($"Cannot read '{path}'.");
            return false;
        }

        this.NotifyAssignmentChanged();
        this.SetStatus(
            this.Assignment.VariableDiagnostics.Count == 0
                ? $"Variables loaded from {path}."
                : $"Loaded {path}, but the data has errors (see the Variables pane)."
        );
        return true;
    }
}
