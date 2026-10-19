namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The logic behind the step mode of the Tree pane, with no view of its own: it walks the trace of the current evaluation
/// one node at a time, in evaluation order (the operands of a node, then the node). It only reads the trace that the engine
/// already made, so it changes no result. A change of the rule or of the values ends the mode, because the steps would
/// no longer describe the rule.
/// </summary>
internal sealed class EvaluationStepController
{
    private static readonly ConditionalWeakTable<Workbench, EvaluationStepController> Controllers = [];

    private readonly Workbench workbench;
    private IReadOnlyList<EvaluationStep> steps = [];
    private int index = -1;

    /// <summary>Initializes a new instance of the <see cref="EvaluationStepController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public EvaluationStepController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        workbench.DocumentChanged += (_, _) => this.End();
        workbench.AssignmentChanged += (_, _) => this.End();
    }

    /// <summary>Occurs after the mode starts or ends or the current step changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether the step mode is on.</summary>
    public bool IsActive => this.index >= 0;

    /// <summary>Gets the steps of the evaluation while the mode is on; otherwise an empty list.</summary>
    public IReadOnlyList<EvaluationStep> Steps => this.steps;

    /// <summary>Gets the index of the current step, or -1 when the mode is off.</summary>
    public int Index => this.index;

    /// <summary>Gets the step the mode is at, or <see langword="null"/> when it is off.</summary>
    public EvaluationStep? Current => this.IsActive ? this.steps[this.index] : null;

    /// <summary>Gets the controller of a workbench. The pane, the keys and the window use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static EvaluationStepController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new EvaluationStepController(key));
    }

    /// <summary>
    /// Gets what the current evaluation returned at each node of the rule, by path, with no step mode: the value as a word or
    /// <c>skipped</c>, and the fault of a term. The inspect text reads it.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="outcome">The outcome of the current assignment.</param>
    /// <returns>The results, or an empty dictionary when no rule was evaluated.</returns>
    public static IReadOnlyDictionary<string, EvaluationStep> ResultsOf(RuleTree tree, AssignmentOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(outcome);

        if (!outcome.HasRule || outcome.Trace is not { } trace || tree.Nodes.Count == 0)
        {
            return new Dictionary<string, EvaluationStep>();
        }

        return BuildSteps(tree.Nodes, trace, node => FaultOf(outcome, node))
            .GroupBy(s => s.Path, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    /// <summary>Goes to the next step. When the mode is off, it starts at the first step.</summary>
    /// <returns><see langword="true"/> when a step is shown.</returns>
    public bool Next()
    {
        if (!this.IsActive)
        {
            return this.Begin();
        }

        if (this.index >= this.steps.Count - 1)
        {
            this.workbench.SetStatus(
                this.steps[this.index].Describe(this.index, this.steps.Count) + " (last step; Esc leaves step mode)"
            );
            return true;
        }

        this.Go(this.index + 1);
        return true;
    }

    /// <summary>Goes to the previous step. When the mode is off, it starts at the first step.</summary>
    /// <returns><see langword="true"/> when a step is shown.</returns>
    public bool Previous()
    {
        if (!this.IsActive)
        {
            return this.Begin();
        }

        this.Go(Math.Max(0, this.index - 1));
        return true;
    }

    /// <summary>Ends the step mode. The tree shows its normal lines again.</summary>
    public void Exit()
    {
        if (this.IsActive)
        {
            this.End();
            this.workbench.SetStatus("Left step mode.");
        }
    }

    /// <summary>
    /// Gets the end of the line of a node for the tree: the value that the node returned, or <c>skipped</c>, when the mode
    /// has reached the node. It is text, so it does not depend on the color.
    /// </summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The text, or an empty string when the mode is off or has not reached the node.</returns>
    public string MarkOf(string path)
    {
        for (int i = 0; this.IsActive && i <= this.index; i++)
        {
            if (string.Equals(this.steps[i].Path, path, StringComparison.Ordinal))
            {
                return "  => " + this.steps[i].Result;
            }
        }

        return string.Empty;
    }

    // Makes the steps from the trace of the evaluation and the nodes of the tree.
    private static List<EvaluationStep> BuildSteps(
        IReadOnlyList<RuleTreeNode> nodes,
        TraceLine trace,
        Func<RuleTreeNode, string?> faultOf
    )
    {
        List<EvaluationStep> steps = [];
        _ = Visit(0, trace);
        return steps;

        // The index of the node after the subtree of a node.
        int End(int at)
        {
            int next = at + 1;
            for (int i = 0; i < nodes[at].OperandCount && next < nodes.Count; i++)
            {
                next = End(next);
            }

            return next;
        }

        // Adds the steps of a subtree, operands first, and returns the index of the node after it.
        int Visit(int at, TraceLine line)
        {
            RuleTreeNode node = nodes[at];
            int end = End(at);
            if (line.Value is not { } value)
            {
                // A skipped node is one step; its operands were never reached.
                steps.Add(new EvaluationStep(node.Path, node.Label, "skipped", null));
                return end;
            }

            // A trace with other operands than the tree has is one step, with no steps for its operands.
            if (line.Children.Count == node.OperandCount)
            {
                int next = at + 1;
                foreach (TraceLine child in line.Children)
                {
                    next = Visit(next, child);
                }
            }

            steps.Add(new EvaluationStep(node.Path, node.Label, value.ToString(), faultOf(node)));
            return end;
        }
    }

    // The message of the fault of a term, such as the exception of its predicate, or null.
    private static string? FaultOf(AssignmentOutcome outcome, RuleTreeNode node)
    {
        if (node.Kind != RuleTreeNodeKind.Term || outcome.Terms.FirstOrDefault(t => t.Display == node.Label) is not { } term)
        {
            return null;
        }

        return outcome
            .Faults.Where(f => f.StartsWith(term.Display + ":", StringComparison.Ordinal))
            .Select(f => f[(term.Display.Length + 1)..].Trim())
            .FirstOrDefault();
    }

    private bool Begin()
    {
        AssignmentOutcome outcome = this.workbench.Outcome;
        if (
            StructureController.For(this.workbench).Tree is not { Nodes.Count: > 0 } tree
            || !outcome.HasRule
            || outcome.Trace is not { } trace
        )
        {
            this.workbench.SetStatus("No evaluation to step through: the rule text has errors.");
            return false;
        }

        this.steps = BuildSteps(tree.Nodes, trace, node => FaultOf(outcome, node));
        this.Go(0);
        return true;
    }

    private void Go(int to)
    {
        this.index = to;
        this.workbench.SelectNode(this.steps[to].Path);
        this.workbench.SetStatus(this.steps[to].Describe(to, this.steps.Count));
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    private void End()
    {
        if (!this.IsActive)
        {
            return;
        }

        this.index = -1;
        this.steps = [];
        this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
