namespace TruthWeaver.Tool.Tui.Views;

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

/// <summary>
/// A dialog with one tick box for each entry: the author ticks any set of entries. The dialog ends only with a non-empty set
/// or with Cancel. OK with nothing ticked keeps the dialog open, as the hint line says.
/// </summary>
internal sealed class TickBoxDialog : Dialog
{
    private readonly Label hint;

    /// <summary>Initializes a new instance of the <see cref="TickBoxDialog"/> class.</summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="items">The text of each tick box.</param>
    /// <param name="initial">The indexes of the boxes that are ticked first.</param>
    /// <param name="accept">The text of the default button, with <c>_</c> before its hot key.</param>
    public TickBoxDialog(string title, IReadOnlyList<string> items, IReadOnlyList<int> initial, string accept)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(initial);

        this.Title = title;
        this.Width = Dim.Percent(60);
        this.Height = items.Count + 8;
        this.Boxes =
        [
            .. items.Select(
                (text, index) =>
                    new CheckBox
                    {
                        Text = text,
                        X = 1,
                        Y = index,
                        Value = initial.Contains(index) ? CheckState.Checked : CheckState.UnChecked,
                    }
            ),
        ];
        this.hint = new Label
        {
            Text = "Tick at least one.",
            X = 1,
            Y = items.Count + 1,
        };
        this.OkButton = new Button { Text = accept, IsDefault = true };
        this.CancelButton = new Button { Text = "_Cancel" };
        this.OkButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            int[] ticked = [.. Enumerable.Range(0, this.Boxes.Count).Where(i => this.Boxes[i].Value == CheckState.Checked)];

            // Nothing ticked: the dialog stays open, as the hint says.
            if (ticked.Length > 0)
            {
                this.Ticked = ticked;
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        };
        this.CancelButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            this.Closed?.Invoke(this, EventArgs.Empty);
        };
        foreach (CheckBox box in this.Boxes)
        {
            this.Add(box);
        }

        this.Add(this.hint);
        this.AddButton(this.OkButton);
        this.AddButton(this.CancelButton);
    }

    /// <summary>Raised when the author accepted a non-empty set (<see cref="Ticked"/> is set) or cancelled (it is not).</summary>
    public event EventHandler? Closed;

    /// <summary>Gets the tick boxes, in the order of the entries.</summary>
    public IReadOnlyList<CheckBox> Boxes { get; }

    /// <summary>Gets the default button.</summary>
    public Button OkButton { get; }

    /// <summary>Gets the Cancel button.</summary>
    public Button CancelButton { get; }

    /// <summary>Gets the indexes of the ticked entries, or <see langword="null"/> while the dialog is open or after Cancel.</summary>
    public IReadOnlyList<int>? Ticked { get; private set; }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            foreach (CheckBox box in this.Boxes)
            {
                box.Dispose();
            }

            this.hint.Dispose();
            this.OkButton.Dispose();
            this.CancelButton.Dispose();
        }
    }
}
