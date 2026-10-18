namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// A workbench window with no terminal: the views are initialized and laid out at a fixed size, the editor has the focus,
/// and keys go in through <see cref="Press"/> as a terminal would send them.
/// </summary>
internal sealed class WorkbenchHarness : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="WorkbenchHarness"/> class.</summary>
    /// <param name="text">The rule text to start with.</param>
    /// <param name="panes">The panes, or <see langword="null"/> for the default panes.</param>
    public WorkbenchHarness(string text = "", IReadOnlyList<IWorkbenchPane>? panes = null)
    {
        this.Workbench = new Workbench(RuleDocument.Create(text), ToolSettings.Default, this.Prompts);
        this.Window = new WorkbenchWindow(this.Workbench, panes);
        this.Window.BeginInit();
        this.Window.EndInit();
        this.Window.Layout(new Size(100, 30));
        this.Window.FocusRegion(PaneSlot.Editor);
    }

    /// <summary>Gets the scripted answers to the workbench questions.</summary>
    public ScriptedPrompts Prompts { get; } = new();

    /// <summary>Gets the shared state.</summary>
    public Workbench Workbench { get; }

    /// <summary>Gets the window.</summary>
    public WorkbenchWindow Window { get; }

    /// <summary>Sends keys to the window, as the terminal does: the focused view gets each key first.</summary>
    /// <param name="keys">The keys.</param>
    public void Press(params Key[] keys)
    {
        foreach (Key key in keys)
        {
            this.Window.NewKeyDownEvent(key);
        }

        this.Window.Layout(new Size(100, 30));
    }

    /// <summary>Clicks a view with the left button, as the terminal reports it: a press, then a release and a click.</summary>
    /// <param name="view">The view.</param>
    /// <param name="x">The column inside the view.</param>
    /// <param name="y">The row inside the view.</param>
    public void Click(View view, int x, int y)
    {
        foreach (
            MouseFlags flags in new[]
            {
                MouseFlags.LeftButtonPressed,
                MouseFlags.LeftButtonReleased,
                MouseFlags.LeftButtonClicked,
            }
        )
        {
            _ = view.NewMouseEvent(
                new Mouse
                {
                    Flags = flags,
                    Position = new Point(x, y),
                    View = view,
                }
            );
        }

        this.Window.Layout(new Size(100, 30));
    }

    /// <summary>Turns the mouse wheel over a view.</summary>
    /// <param name="view">The view.</param>
    /// <param name="down"><see langword="true"/> to turn it down; <see langword="false"/> for up.</param>
    /// <param name="steps">The number of wheel steps.</param>
    public void Wheel(View view, bool down, int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            _ = view.NewMouseEvent(
                new Mouse
                {
                    Flags = down ? MouseFlags.WheeledDown : MouseFlags.WheeledUp,
                    Position = new Point(1, 1),
                    View = view,
                }
            );
        }

        this.Window.Layout(new Size(100, 30));
    }

    /// <summary>Types text into the focused view, one key per character.</summary>
    /// <param name="text">The text.</param>
    public void Type(string text)
    {
        this.Press([.. text.Select(c => new Key(c))]);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Window.Dispose();
    }
}
