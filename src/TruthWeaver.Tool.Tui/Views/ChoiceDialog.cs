namespace TruthWeaver.Tool.Tui.Views;

using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Theme;
using Attribute = Terminal.Gui.Drawing.Attribute;

/// <summary>
/// A list dialog: the author picks one entry, with the keyboard or the mouse. With a filter it also has a search box above the
/// list. The dialog ends only when a real entry is chosen: a double click on blank space, on a group header line or on an
/// empty filter result does nothing, and Enter on a header moves the highlight to the first entry of its group.
/// </summary>
internal sealed class ChoiceDialog : Dialog
{
    private readonly IReadOnlyList<string> items;
    private readonly Func<ThemePalette> palette;
    private readonly Label? prompt;
    private readonly Label? detail;
    private readonly SyntaxLegend? legend;
    private IReadOnlyList<int> visible;

    /// <summary>Initializes a new instance of the <see cref="ChoiceDialog"/> class.</summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="items">The entries. A line made by <see cref="HeaderLine"/> is a group header, not an entry.</param>
    /// <param name="accept">The text of the default button, with <c>_</c> before its hot key.</param>
    /// <param name="filter">Gets the text of the search box and returns the indexes of <paramref name="items"/> to show; <see langword="null"/> for a dialog without a search box.</param>
    /// <param name="palette">Gets the colors of the current theme.</param>
    public ChoiceDialog(
        string title,
        IReadOnlyList<string> items,
        string accept,
        Func<string, IReadOnlyList<int>>? filter,
        Func<ThemePalette> palette
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(palette);

        this.items = items;
        this.palette = palette;
        this.visible = filter is null ? [.. Enumerable.Range(0, items.Count)] : filter(string.Empty);
        this.Title = title;
        this.Width = Dim.Percent(80);
        this.Height = Dim.Percent(80);

        this.List = new ListView
        {
            X = 0,
            Y = filter is null ? 0 : 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(filter is null ? 2 : 4),
        };
        this.ShowVisible();
        this.OkButton = new Button { Text = accept, IsDefault = true };
        this.CancelButton = new Button { Text = "_Cancel" };

        // A double click on a real row chooses it, as Enter does. Every other double click is consumed here, so it never reaches
        // the default button: blank space below the rows, a header and an empty result do nothing.
        this.List.MouseEvent += (_, args) =>
        {
            if (args.Flags != MouseFlags.LeftButtonDoubleClicked)
            {
                return;
            }

            args.Handled = true;
            int row = this.List.Viewport.Y + args.Position.GetValueOrDefault().Y;
            if (row >= 0 && row < this.visible.Count)
            {
                this.List.Value = row;
                this.Activate();
            }
        };
        this.List.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Activate();
        };
        this.List.RowRender += (_, args) => this.ColorRow(args);
        this.OkButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Activate();
        };
        this.CancelButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Closed?.Invoke(this, EventArgs.Empty);
        };

        if (filter is not null)
        {
            this.prompt = new Label
            {
                Text = "Search:",
                X = 0,
                Y = 0,
            };
            this.SearchBox = new TextField
            {
                X = 8,
                Y = 0,
                Width = Dim.Fill(),
            };

            // The description of the chosen row has its own color, and the legend names every color, so no color stands alone.
            this.detail = new Label
            {
                X = 0,
                Y = Pos.Bottom(this.List),
                Width = Dim.Fill(),
            };
            this.detail.SetScheme(SyntaxLegend.SchemeOf(SyntaxKind.Description, palette()));
            this.legend = new SyntaxLegend(palette()) { X = 0, Y = Pos.Bottom(this.List) + 1 };

            // Enter in the box takes the highlighted row, like Enter in the list. The dialog handles it here, because an unhandled
            // Enter would bubble to a button of the dialog on its own.
            this.SearchBox.Accepting += (_, args) =>
            {
                args.Handled = true;
                this.Activate();
            };

            // Each change of the box rebuilds the list from the indexes the filter returns. Replacing the source clears the
            // list's own selection, so without this the highlight would vanish on every keystroke and Enter would do nothing
            // until the author pressed an arrow key first. Selecting the first row keeps a visible highlight and makes Enter
            // take the top match, the same row Activate() would move to anyway if that row turns out to be a family header.
            this.SearchBox.TextChanged += (_, _) =>
            {
                this.visible = filter(this.SearchBox.Text ?? string.Empty);
                this.ShowVisible();
                if (this.visible.Count > 0)
                {
                    this.List.Value = 0;
                }

                this.ShowDetail();
            };
            this.List.ValueChanged += (_, _) => this.ShowDetail();
            this.ShowDetail();
            this.Add(this.prompt, this.SearchBox, this.List, this.detail, this.legend);
        }
        else
        {
            this.Add(this.List);
        }

        this.AddButton(this.OkButton);
        this.AddButton(this.CancelButton);
    }

    /// <summary>Raised when the author chose an entry (<see cref="Choice"/> is set) or cancelled (it is not).</summary>
    public event EventHandler? Closed;

    /// <summary>Gets the list of the entries that match the search box.</summary>
    public ListView List { get; }

    /// <summary>Gets the search box, or <see langword="null"/> for a dialog without a filter.</summary>
    public TextField? SearchBox { get; }

    /// <summary>Gets the default button.</summary>
    public Button OkButton { get; }

    /// <summary>Gets the Cancel button.</summary>
    public Button CancelButton { get; }

    /// <summary>Gets the index in the entries of the chosen entry, or <see langword="null"/> while the dialog is open or after Cancel.</summary>
    public int? Choice { get; private set; }

    /// <summary>Makes the line of a group header, which the dialog never takes as a choice.</summary>
    /// <param name="group">The name of the group.</param>
    /// <returns>The header line.</returns>
    public static string HeaderLine(string group)
    {
        return $"--- {group} ---";
    }

    /// <summary>Tells whether a line is a group header made by <see cref="HeaderLine"/>.</summary>
    /// <param name="line">The line.</param>
    /// <returns><see langword="true"/> for a header.</returns>
    public static bool IsHeader(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return line.StartsWith("--- ", StringComparison.Ordinal) && line.EndsWith(" ---", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.prompt?.Dispose();
            this.SearchBox?.Dispose();
            this.detail?.Dispose();
            this.legend?.Dispose();
            this.List.Dispose();
            this.OkButton.Dispose();
            this.CancelButton.Dispose();
        }
    }

    private void ShowVisible()
    {
        this.List.SetSource(new ObservableCollection<string>([.. this.visible.Select(i => this.items[i])]));
    }

    // Takes the highlighted row: an entry ends the dialog; a header moves the highlight to the first entry of its group; no
    // row (an empty result) does nothing. The dialog stays open in the last two cases, so the search text and the scroll
    // position stay.
    private void Activate()
    {
        if (this.List.Value is not int row || row < 0 || row >= this.visible.Count)
        {
            return;
        }

        if (IsHeader(this.items[this.visible[row]]))
        {
            if (row + 1 < this.visible.Count && !IsHeader(this.items[this.visible[row + 1]]))
            {
                this.List.Value = row + 1;
            }

            return;
        }

        this.Choice = this.visible[row];
        this.Closed?.Invoke(this, EventArgs.Empty);
    }

    // The part of the chosen row after " - " is its description.
    private void ShowDetail()
    {
        string? line =
            this.List.Value is int row && row >= 0 && row < this.visible.Count ? this.items[this.visible[row]] : null;
        int cut = line is null ? -1 : line.IndexOf(" - ", StringComparison.Ordinal);
        this.detail!.Text = cut < 0 ? string.Empty : line![(cut + 3)..];
    }

    // Paints a row in the color of its syntax kind. The chosen row keeps the attribute of the list, so the selection stays
    // visible; the kind is also the first word of the row, so the color is never the only mark.
    private void ColorRow(ListViewRowEventArgs args)
    {
        if (
            this.List.Value == args.Row
            || args.Row >= this.visible.Count
            || SyntaxKinds.OfPaletteLine(this.items[this.visible[args.Row]]) is not { } kind
        )
        {
            return;
        }

        ThemePalette colors = this.palette();
        Attribute normal = this.List.GetAttributeForRole(VisualRole.Normal);
        args.RowAttribute = new Attribute(
            colors.Of(kind),
            colors.HasColor ? normal.Background : Color.None,
            colors.StyleOf(kind)
        );
    }
}
