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

    private readonly InspectPanel inspectPanel = new(TopRows, BottomRows);
    private object? hoverTimer;

    // What the pointer rests on: a tree node or an editor offset. A move inside the same part restarts nothing.
    private HoverTarget? hoverTarget;

    // The part the hover waits to describe, and where the pointer was: set when the pointer rests, cleared when the box shows
    // or the wait is cancelled.
    private (Func<string?> Describe, Point Screen)? pendingHover;

    // True while the box shows because of the inspect key. Only Esc, a click or a change of the rule or the values closes it;
    // a move of the pointer does not.
    private bool inspectPinned;

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
            ? controller.DescribeCaret(editor.CaretOffset)
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

        // A pending hover would replace the box. The part under the pointer stays known, so a move inside it changes nothing.
        this.CancelPendingHover();
        this.inspectPinned = true;
        this.inspectPanel.ShowText(text, this.Viewport.Size, null);
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
        if (this.TargetAt(view, screen)?.Describe() is not { } text)
        {
            return false;
        }

        this.inspectPinned = false;
        this.inspectPanel.ShowText(text, this.Viewport.Size, this.ScreenToViewport(screen));
        return true;
    }

    /// <summary>
    /// Hides the box and forgets what the pointer rests on, so the next move of the pointer starts the hover again. A click
    /// and a change of the rule or of the values use it.
    /// </summary>
    public void HideInspect()
    {
        this.CloseInspect();
        this.hoverTarget = null;
    }

    /// <summary>
    /// Closes the box for Esc. The part under the pointer stays known, so a pointer that stays or moves inside the same part
    /// does not open the box again; it opens again after the pointer moves to another part.
    /// </summary>
    public void CloseInspect()
    {
        this.CancelPendingHover();
        this.inspectPinned = false;
        this.inspectPanel.HideText();
    }

    // Handles a pointer at rest on a point: starts the wait for the hover box when the point is on another part than before.
    // Internal so a test can drive it without a mouse.
    internal void HoverAt(View? view, Point screen)
    {
        (HoverTarget Target, Func<string?> Describe)? target = this.TargetAt(view, screen);
        if (target?.Target == this.hoverTarget)
        {
            return;
        }

        this.hoverTarget = target?.Target;
        this.CancelPendingHover();
        if (this.inspectPinned)
        {
            return;
        }

        this.inspectPanel.HideText();
        if (target is not { } found)
        {
            return;
        }

        this.pendingHover = (found.Describe, screen);
        if (this.App is { } app)
        {
            this.hoverTimer = app.AddTimeout(
                HoverDelay,
                () =>
                {
                    this.hoverTimer = null;
                    this.ShowPendingHover();
                    return false;
                }
            );
        }
    }

    // Shows the box for the part the pointer rests on, as the delay does when it ends. Internal for the same reason.
    internal void ShowPendingHover()
    {
        if (this.pendingHover is not { } pending)
        {
            return;
        }

        this.pendingHover = null;
        if (pending.Describe() is { } text)
        {
            this.inspectPinned = false;
            this.inspectPanel.ShowText(text, this.Viewport.Size, this.ScreenToViewport(pending.Screen));
        }
    }

    // The part of the rule under a point, and how to describe it: a node of the tree, or a place in the editor text.
    private (HoverTarget Target, Func<string?> Describe)? TargetAt(View? view, Point screen)
    {
        InspectController controller = InspectController.For(this.Workbench);
        for (View? current = view; current is not null; current = current.SuperView)
        {
            if (current is RuleEditorView editor)
            {
                int offset = editor.OffsetAt(editor.ScreenToViewport(screen));
                return (HoverTarget.OfOffset(controller.PartStartAt(offset)), () => controller.DescribeOffset(offset));
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
                    return (HoverTarget.OfNode(path), () => controller.DescribeNode(path));
                }

                return null;
            }
        }

        return null;
    }

    // A move of the pointer: when it rests on another part, the hover box goes away and the delay starts again. A box that the
    // inspect key opened stays; the pointer only changes which part the hover would describe.
    private void OnHover(Mouse mouse)
    {
        this.HoverAt(mouse.View, mouse.ScreenPosition);
    }

    // Stops the wait for a hover box. The part the pointer rests on stays known.
    private void CancelPendingHover()
    {
        this.pendingHover = null;
        if (this.hoverTimer is { } timer)
        {
            this.hoverTimer = null;
            _ = this.App?.RemoveTimeout(timer);
        }
    }
}
