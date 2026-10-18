namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// The part of the workbench state that the structural editor and the rewrites share: the selected node of the tree, and
/// the step that puts an edited or rewritten rule into the document.
/// </summary>
internal sealed partial class Workbench
{
    private string selectedNode = "$";

    /// <summary>Occurs after <see cref="SelectedNode"/> changes.</summary>
    public event EventHandler? SelectedNodeChanged;

    /// <summary>
    /// Gets the path of the selected node of the tree, such as <c>$.operands[1]</c> (see <see cref="RuleTreeNode.Path"/>).
    /// It is <c>$</c>, the root, until a pane selects another node. A pane that shows the tree selects with
    /// <see cref="SelectNode"/>; the structural edits act on this node.
    /// </summary>
    public string SelectedNode => this.selectedNode;

    /// <summary>Selects a node of the tree.</summary>
    /// <param name="path">The path of the node.</param>
    public void SelectNode(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.Equals(path, this.selectedNode, StringComparison.Ordinal))
        {
            return;
        }

        this.selectedNode = path;
        this.SelectedNodeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Puts a rule into the document as one undo step (see <see cref="RuleDocument.ReplaceRule"/>) and reports the outcome on
    /// the status line.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="done">The message for the status line when the rule was put in.</param>
    /// <returns><see langword="true"/> when the document changed.</returns>
    public bool ReplaceRule(Evaluation.CompiledRule<StubAssignment> rule, string done)
    {
        ArgumentNullException.ThrowIfNull(rule);

        ToolResult<string> replaced = this.document.ReplaceRule(rule);
        if (!replaced.Succeeded)
        {
            this.SetStatus(Describe(replaced.Diagnostics, "Not applied: the printed rule does not compile."));
            return false;
        }

        this.SetStatus(done);
        return true;
    }

    /// <summary>
    /// Puts the result of a structural edit into the document as one undo step (see <see cref="RuleDocument.ApplyEdit"/>),
    /// with the <c>let</c> definitions of the text when the edit kept them, and reports the outcome on the status line.
    /// </summary>
    /// <param name="edit">An edit that succeeded.</param>
    /// <param name="done">The message for the status line when the edit was put in.</param>
    /// <returns><see langword="true"/> when the document changed.</returns>
    public bool ApplyEdit(RuleTreeEdit edit, string done)
    {
        ArgumentNullException.ThrowIfNull(edit);

        ToolResult<string> applied = this.document.ApplyEdit(edit);
        if (!applied.Succeeded)
        {
            this.SetStatus(Describe(applied.Diagnostics, "Not applied: the printed rule does not compile."));
            return false;
        }

        this.SetStatus(done);
        return true;
    }

    /// <summary>
    /// Checks that the text of the document compiles, so a change of the tree starts from what the author sees. When it does
    /// not, the status line says so: an edit of the last valid tree would drop the text the author typed since.
    /// </summary>
    /// <param name="action">What the author is about to do, such as "edit the tree".</param>
    /// <returns><see langword="true"/> when the rule of the document is the rule of its text.</returns>
    public bool RequireCurrentRule(string action)
    {
        if (this.document.IsRuleCurrent && this.document.Rule is not null)
        {
            return true;
        }

        Diagnostic? first = this.document.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        this.SetStatus(
            $"Cannot {action}: the rule text has errors{(first is null ? "." : $" ({first.Code}).")} Fix them first, or undo."
        );
        return false;
    }
}
