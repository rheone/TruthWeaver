namespace TruthWeaver.Tool.Tui.Tests.Views;

using System.Drawing;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;

/// <summary>The tick-box dialog, driven without a running application: its buttons accept and the dialog reports what ended it.</summary>
[Collection(TerminalCollection.Name)]
public sealed class TickBoxDialogTests
{
    private static readonly string[] Items = ["One", "Two", "Three"];

    /// <summary>OK with no box ticked does not end the dialog and returns nothing.</summary>
    [Fact]
    public void OkButton_NothingTicked_KeepsTheDialogOpen_Test()
    {
        using TickBoxDialog dialog = Open([], out Func<bool> closed);

        _ = dialog.OkButton.InvokeCommand(Terminal.Gui.Input.Command.Accept);

        Assert.False(closed());
        Assert.Null(dialog.Ticked);
    }

    /// <summary>OK with boxes ticked ends the dialog and returns their indexes.</summary>
    [Fact]
    public void OkButton_SomeTicked_EndsWithTheirIndexes_Test()
    {
        using TickBoxDialog dialog = Open([0, 2], out Func<bool> closed);

        _ = dialog.OkButton.InvokeCommand(Terminal.Gui.Input.Command.Accept);

        Assert.True(closed());
        Assert.Equal([0, 2], dialog.Ticked);
    }

    /// <summary>Cancel ends the dialog with no result.</summary>
    [Fact]
    public void CancelButton_Pressed_EndsWithNoResult_Test()
    {
        using TickBoxDialog dialog = Open([0], out Func<bool> closed);

        _ = dialog.CancelButton.InvokeCommand(Terminal.Gui.Input.Command.Accept);

        Assert.True(closed());
        Assert.Null(dialog.Ticked);
    }

    /// <summary>The dialog shows the hint that explains why OK with nothing ticked keeps it open.</summary>
    [Fact]
    public void Constructor_Always_ShowsTheHintText_Test()
    {
        using TickBoxDialog dialog = Open([0], out _);

        Assert.Contains(dialog.SubViews.OfType<Label>(), label => label.Text == TickBoxDialog.HintText);
    }

    private static TickBoxDialog Open(int[] initial, out Func<bool> closed)
    {
        TickBoxDialog dialog = new("Pick", Items, initial, "_OK");
        bool isClosed = false;
        dialog.Closed += (_, _) => isClosed = true;
        dialog.BeginInit();
        dialog.EndInit();
        dialog.Layout(new Size(100, 30));
        closed = () => isClosed;
        return dialog;
    }
}
