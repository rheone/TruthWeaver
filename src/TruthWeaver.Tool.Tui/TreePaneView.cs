namespace TruthWeaver.Tool.Tui;

using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>The view of <see cref="TreePane"/>: a line with the selected node above the list of the nodes of the tree.</summary>
internal sealed class TreePaneView : View
{
    private readonly StructureController controller;
    private readonly Label header;
    private readonly Label coverage;
    private readonly ObservableCollection<string> lines = [];
    private bool showing;

    /// <summary>Initializes a new instance of the <see cref="TreePaneView"/> class.</summary>
    /// <param name="controller">The logic behind the pane.</param>
    public TreePaneView(StructureController controller)
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
            Height = 1,
        };
        this.coverage = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
        };
        this.List = new ListView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        this.List.SetSource(this.lines);

        // The letters are the edit keys of the pane, not a search in the list.
        this.List.KeystrokeNavigator = null;
        this.Add(this.header, this.coverage, this.List);

        // Moving in the list selects the node for the edits; a refresh of the list does not.
        this.List.ValueChanged += (_, _) =>
        {
            if (!this.showing && this.List.Value is int index)
            {
                this.controller.SelectIndex(index);
            }
        };
    }

    /// <summary>Gets the list of the nodes.</summary>
    public ListView List { get; }

    /// <summary>Gets the line above the list.</summary>
    public string HeaderText => this.header.Text;

    /// <summary>Gets the line under the header: the coverage of the saved cases, or the reason there is none.</summary>
    public string CoverageText => this.coverage.Text;

    /// <summary>Gets the lines of the list.</summary>
    public IReadOnlyList<string> Lines => this.lines;

    /// <summary>Shows the tree and the selected node of the controller.</summary>
    public void Show()
    {
        this.showing = true;
        try
        {
            this.lines.Clear();
            foreach (string line in this.controller.Lines)
            {
                this.lines.Add(line);
            }

            if (this.controller.SelectedIndex >= 0)
            {
                this.List.Value = this.controller.SelectedIndex;
            }

            this.header.Text = this.controller.Header;
            this.coverage.Text = this.controller.CoverageSummary;
        }
        finally
        {
            this.showing = false;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        // The base class disposes the subviews; disposing a view twice does nothing.
        base.Dispose(disposing);
        if (disposing)
        {
            this.header.Dispose();
            this.coverage.Dispose();
            this.List.Dispose();
        }
    }
}
