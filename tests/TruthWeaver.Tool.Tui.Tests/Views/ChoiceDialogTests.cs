namespace TruthWeaver.Tool.Tui.Tests.Views;

using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;

/// <summary>
/// The list dialogs, driven the way the terminal drives them: keys go through the key pipeline of the dialog and the mouse
/// through the mouse pipeline of the view under the pointer. No terminal runs.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class ChoiceDialogTests
{
    private static readonly string[] Rows =
    [
        "constant true - the constant",
        ChoiceDialog.HeaderLine("Numeric"),
        "catalog a - first",
        "catalog b - second",
    ];

    /// <summary>A double click on blank space below the rows does nothing: the dialog stays open and returns no choice.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoubleClick_BlankSpaceBelowTheRows_KeepsTheDialogOpen_Test(bool filtered)
    {
        using DialogSession session = new(filtered);

        session.DoubleClick(session.Dialog.List, 2, 10);

        Assert.Null(session.Dialog.Choice);
        Assert.False(session.Closed);
    }

    /// <summary>A double click on a group header does nothing: the highlight stays on the header, the dialog stays open and returns no choice.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoubleClick_GroupHeader_KeepsTheDialogOpen_Test(bool filtered)
    {
        using DialogSession session = new(filtered);

        // The clicks of the pair select the header row, as a click on any row does; the double click must not go on to the entry.
        session.DoubleClick(session.Dialog.List, 2, 1);

        Assert.Equal(1, session.Dialog.List.Value);
        Assert.Null(session.Dialog.Choice);
        Assert.False(session.Closed);
    }

    /// <summary>The first click of a double click on a group header selects the header row, as a click on any row does, and ends nothing.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Click_GroupHeader_MovesTheHighlightToTheHeaderAndKeepsTheDialogOpen_Test(bool filtered)
    {
        using DialogSession session = new(filtered);

        session.Click(session.Dialog.List, 2, 1);

        Assert.Equal(1, session.Dialog.List.Value);
        Assert.Null(session.Dialog.Choice);
        Assert.False(session.Closed);
    }

    /// <summary>A double click on a real row chooses that row and closes the dialog.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DoubleClick_RealRow_ChoosesIt_Test(bool filtered)
    {
        using DialogSession session = new(filtered);

        session.DoubleClick(session.Dialog.List, 2, 3);

        Assert.Equal(3, session.Dialog.Choice);
        Assert.True(session.Closed);
    }

    /// <summary>Enter on a real row chooses that row and closes the dialog.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Enter_RealRow_ChoosesIt_Test(bool filtered)
    {
        using DialogSession session = new(filtered);
        session.Dialog.List.Value = 2;

        session.Press(Key.Enter);

        Assert.Equal(2, session.Dialog.Choice);
        Assert.True(session.Closed);
    }

    /// <summary>Enter on a group header moves the highlight to the first entry of the group and keeps the dialog open.</summary>
    /// <param name="filtered">Whether the dialog has a search box.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Enter_GroupHeader_MovesTheHighlightToTheFirstEntry_Test(bool filtered)
    {
        using DialogSession session = new(filtered);
        session.Dialog.List.Value = 1;

        session.Press(Key.Enter);

        Assert.Equal(2, session.Dialog.List.Value);
        Assert.Null(session.Dialog.Choice);
        Assert.False(session.Closed);
    }

    /// <summary>Enter on a header keeps the text of the search box.</summary>
    [Fact]
    public void Enter_GroupHeaderInAFilteredList_KeepsTheSearchText_Test()
    {
        using DialogSession session = new(true);
        session.Dialog.SearchBox!.Text = "catalog";
        session.Dialog.List.Value = 0;

        session.Press(Key.Enter);

        Assert.Equal("catalog", session.Dialog.SearchBox.Text);
        Assert.False(session.Closed);
    }

    /// <summary>Enter and a double click on an empty filter result do nothing.</summary>
    [Fact]
    public void Filtered_EmptyResult_EnterAndDoubleClickDoNothing_Test()
    {
        using DialogSession session = new(true);
        session.Dialog.SearchBox!.Text = "zzz";
        Assert.Equal(0, session.Dialog.List.Source!.Count);

        session.Press(Key.Enter);
        session.DoubleClick(session.Dialog.List, 2, 0);

        Assert.Null(session.Dialog.Choice);
        Assert.False(session.Closed);
        Assert.Equal("zzz", session.Dialog.SearchBox.Text);
    }

    /// <summary>The index of a chosen row is the index in the entries, not in the filtered list.</summary>
    [Fact]
    public void Filtered_RowOfTheResult_ReturnsTheIndexInTheEntries_Test()
    {
        using DialogSession session = new(true);
        session.Dialog.SearchBox!.Text = "second";
        Assert.Equal(1, session.Dialog.List.Source!.Count);
        session.Dialog.List.Value = 0;

        session.Press(Key.Enter);

        Assert.Equal(3, session.Dialog.Choice);
    }

    /// <summary>
    /// A filter change replaces the list source, which on its own drops the ListView's selection entirely (no row highlighted,
    /// <see cref="Terminal.Gui.Views.ListView.Value"/> null) rather than keeping or resetting it. The dialog must put the
    /// highlight back on the first visible row so Enter still takes a choice right after typing.
    /// </summary>
    [Fact]
    public void TextChanged_NarrowsPastThePreviousSelection_SelectsTheFirstVisibleRow_Test()
    {
        using DialogSession session = new(true);
        session.Dialog.List.Value = 3; // "catalog b - second", the last unfiltered row

        session.Dialog.SearchBox!.Text = "constant"; // only "constant true - the constant" matches

        Assert.Equal(0, session.Dialog.List.Value);
        session.Press(Key.Enter);
        Assert.Equal(0, session.Dialog.Choice);
    }

    /// <summary>
    /// A filter change whose top match is a family header still gets a highlight on that header row, and Enter on it moves to
    /// the first entry of the family rather than doing nothing.
    /// </summary>
    [Fact]
    public void TextChanged_TopMatchIsAHeader_EnterMovesToTheFirstEntryOfTheFamily_Test()
    {
        using DialogSession session = new(true);

        session.Dialog.SearchBox!.Text = "catalog"; // visible: header, "catalog a", "catalog b"

        Assert.Equal(0, session.Dialog.List.Value);
        session.Press(Key.Enter);
        Assert.Equal(1, session.Dialog.List.Value);
        Assert.Null(session.Dialog.Choice);
    }

    /// <summary>Cancel closes the dialog with no choice.</summary>
    [Fact]
    public void Cancel_Hotkey_ClosesWithNoChoice_Test()
    {
        using DialogSession session = new(false);

        session.Press(Key.C.WithAlt);

        Assert.Null(session.Dialog.Choice);
        Assert.True(session.Closed);
    }

    /// <summary>A list dialog in a window of 100 by 30 cells, with the keys and the mouse sent as a terminal sends them.</summary>
    private sealed class DialogSession : IDisposable
    {
        public DialogSession(bool filtered)
        {
            this.Dialog = new ChoiceDialog("Pick", Rows, "_OK", filtered ? Filter : null, () => ThemePalette.Dark);
            this.Dialog.Closed += (_, _) => this.Closed = true;
            this.Dialog.BeginInit();
            this.Dialog.EndInit();
            this.Dialog.Layout(new Size(100, 30));
            this.Dialog.List.Value = 0;
            _ = (filtered ? this.Dialog.SearchBox! : (View)this.Dialog.List).SetFocus();
        }

        public ChoiceDialog Dialog { get; }

        public bool Closed { get; private set; }

        public void Press(Key key)
        {
            _ = this.Dialog.NewKeyDownEvent(key);
            this.Dialog.Layout(new Size(100, 30));
        }

        // The first click of the pair: press, release and click.
        public void Click(View view, int x, int y)
        {
            this.Send(view, x, y, MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked);
        }

        // The terminal sends the pair of clicks and then the double-click event.
        public void DoubleClick(View view, int x, int y)
        {
            this.Send(view, x, y, MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked);
            this.Send(
                view,
                x,
                y,
                MouseFlags.LeftButtonPressed,
                MouseFlags.LeftButtonReleased,
                MouseFlags.LeftButtonDoubleClicked
            );
        }

        public void Dispose()
        {
            this.Dialog.Dispose();
        }

        // An empty text shows every row; otherwise the entries that contain the text and the header of a group that has one.
        private static List<int> Filter(string text)
        {
            bool Matches(int i)
            {
                return text.Length == 0
                    || (!ChoiceDialog.IsHeader(Rows[i]) && Rows[i].Contains(text, StringComparison.Ordinal));
            }

            return
            [
                .. Enumerable
                    .Range(0, Rows.Length)
                    .Where(i => Matches(i) || (ChoiceDialog.IsHeader(Rows[i]) && i + 1 < Rows.Length && Matches(i + 1))),
            ];
        }

        private void Send(View view, int x, int y, params MouseFlags[] flags)
        {
            foreach (MouseFlags flag in flags)
            {
                _ = view.NewMouseEvent(
                    new Mouse
                    {
                        Flags = flag,
                        Position = new Point(x, y),
                        View = view,
                    }
                );
            }

            this.Dialog.Layout(new Size(100, 30));
        }
    }
}
