namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// CS0618: Terminal.Gui 2.5 marks TextView obsolete in favour of the separate tui-cs/Editor package, a dependency the owner
// has not approved. The read-only text view is the only scrollable text control in the approved package. The suppression
// covers only the two TextView references below; .scratch/tui-workbench/issues/09-move-to-terminal-gui-editor-package.md records the follow-up.

/// <summary>The view of <see cref="ViewPane"/>: a line of toggles above a read-only text view.</summary>
internal sealed class ViewPaneView : View
{
    private readonly ViewController controller;
    private readonly Label header;

    /// <summary>Initializes a new instance of the <see cref="ViewPaneView"/> class.</summary>
    /// <param name="controller">The logic behind the pane.</param>
    public ViewPaneView(ViewController controller)
    {
        this.controller = controller;
        this.Width = Dim.Fill();
        this.Height = Dim.Fill();
        this.CanFocus = true;
        this.header = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 2,
        };
        this.header.TextFormatter.WordWrap = true;
#pragma warning disable CS0618 // TextView is obsolete; see the comment at the top of the file.
        this.Body = new TextView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
        };
#pragma warning restore CS0618
        this.Add(this.header, this.Body);
    }

    /// <summary>Gets the text view that shows the printed rule.</summary>
#pragma warning disable CS0618 // TextView is obsolete; see the comment at the top of the file.
    public TextView Body { get; }
#pragma warning restore CS0618

    /// <summary>Gets the text that the text view shows.</summary>
    public string BodyText => this.Body.Text;

    /// <summary>Shows the header and the printed rule of the controller.</summary>
    public void Show()
    {
        this.header.Text = this.controller.Header;
        this.Body.Text = this.controller.Output.Display;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing.
        base.Dispose(disposing);
        if (disposing)
        {
            this.header.Dispose();
            this.Body.Dispose();
        }
    }
}
