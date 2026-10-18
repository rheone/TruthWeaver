namespace TruthWeaver.Tool.Tui;

using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>The view of <see cref="ViewPane"/>: a line of toggles above a read-only editor view.</summary>
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
        this.Body = new Editor
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
            GutterOptions = GutterOptions.None,
        };
        this.Add(this.header, this.Body);
    }

    /// <summary>Gets the editor view that shows the printed rule.</summary>
    public Editor Body { get; }

    /// <summary>Gets the text that the editor view shows.</summary>
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
