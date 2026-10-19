namespace TruthWeaver.Tool.Tui.Workbench;

using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Views;

/// <summary>
/// The inspect part of the window: the inspect key and the mouse hover show the description of one part of the rule in the
/// same box. Both ask <see cref="InspectController"/> for the text, so they never differ.
/// </summary>
internal sealed partial class WorkbenchWindow
{
    /// <summary>The time the pointer must rest on a part before the box shows.</summary>
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(500);

    private readonly InspectPanel inspectPanel = new();
    private object? hoverTimer;

    // What the pointer rests on: a tree path or an editor offset, as text, so a move inside the same part restarts nothing.
    private string? hoverTarget;

    /// <summary>Gets the text of the box while it shows, or an empty string.</summary>
    public string InspectText => this.inspectPanel.Text;

    /// <summary>
    /// Shows the description of the part of the rule that has the focus: the place of the cursor in the editor, or the
    /// selected node of the tree. It is the keyboard route; the hover shows the same text.
    /// </summary>
    public void Inspect()
    {
        View? focused = this.MostFocused;
        InspectController controller = InspectController.For(this.Workbench);
        string? text = focused is RuleEditorView editor
            ? controller.DescribeOffset(editor.CaretOffset)
            : controller.DescribeNode(this.Workbench.SelectedNode);
        if (text is null)
        {
            this.HideInspect();
            this.Workbench.SetStatus(
                focused is RuleEditorView
                    ? "Nothing to inspect here: put the cursor on an operator, a predicate, a variable, a constant, a note or an error."
                    : "Nothing to inspect: the rule text has errors, so there is no tree."
            );
            return;
        }

        this.CancelHover();
        this.inspectPanel.ShowText(text, this.Viewport.Size, BottomRows, TopRows, null);
    }

    /// <summary>
    /// Shows the box for the part of the rule under a point, as the hover does after its delay. A point that is not on the
    /// editor text or a line of the tree shows nothing.
    /// </summary>
    /// <param name="view">The deepest view under the point.</param>
    /// <param name="screen">The point on the screen.</param>
    /// <returns><see langword="true"/> when the box shows.</returns>
    public bool InspectAt(View? view, Point screen)
    {
        string? text = this.TextAt(view, screen);
        if (text is null)
        {
            return false;
        }

        this.inspectPanel.ShowText(text, this.Viewport.Size, BottomRows, TopRows, this.ScreenToViewport(screen));
        return true;
    }

    /// <summary>Hides the box.</summary>
    public void HideInspect()
    {
        this.CancelHover();
        this.inspectPanel.HideText();
    }

    // The target of a point, as text: "n:path" for a line of the tree and "o:offset" for a place in the editor text.
    private (string Target, Func<string?> Describe)? TargetAt(View? view, Point screen)
    {
        InspectController controller = InspectController.For(this.Workbench);
        for (View? current = view; current is not null; current = current.SuperView)
        {
            if (current is RuleEditorView editor)
            {
                int offset = editor.OffsetAt(editor.ScreenToViewport(screen));
                return ($"o:{offset}", () => controller.DescribeOffset(offset));
            }

            if (current is TreePaneView { List.Source: { } source } tree)
            {
                int row = tree.List.Viewport.Y + tree.List.ScreenToViewport(screen).Y;
                if (
                    row >= 0
                    && row < source.Count
                    && StructureController.For(this.Workbench).Tree is { } nodes
                    && row < nodes.Nodes.Count
                )
                {
                    string path = nodes.Nodes[row].Path;
                    return ($"n:{path}", () => controller.DescribeNode(path));
                }

                return null;
            }
        }

        return null;
    }

    private string? TextAt(View? view, Point screen)
    {
        return this.TargetAt(view, screen)?.Describe();
    }

    // A move of the pointer: when it rests on another part, the box goes away and the delay starts again.
    private void OnHover(Mouse mouse)
    {
        (string Target, Func<string?> Describe)? target = this.TargetAt(mouse.View, mouse.ScreenPosition);
        if (target?.Target == this.hoverTarget)
        {
            return;
        }

        this.HideInspect();
        this.hoverTarget = target?.Target;
        if (target is not { } found || this.App is not { } app)
        {
            return;
        }

        Point screen = mouse.ScreenPosition;
        this.hoverTimer = app.AddTimeout(
            HoverDelay,
            () =>
            {
                this.hoverTimer = null;
                if (found.Describe() is { } text)
                {
                    this.inspectPanel.ShowText(text, this.Viewport.Size, BottomRows, TopRows, this.ScreenToViewport(screen));
                }

                return false;
            }
        );
    }

    private void CancelHover()
    {
        if (this.hoverTimer is { } timer)
        {
            this.hoverTimer = null;
            _ = this.App?.RemoveTimeout(timer);
        }

        this.hoverTarget = null;
    }
}
