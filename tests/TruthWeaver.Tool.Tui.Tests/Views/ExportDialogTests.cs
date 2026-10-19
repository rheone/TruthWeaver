namespace TruthWeaver.Tool.Tui.Tests.Views;

using System.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The export dialog, driven the way the terminal drives it: keys go through the key pipeline of the dialog and the mouse
/// through the mouse pipeline of the view under the pointer. No terminal runs.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class ExportDialogTests
{
    /// <summary>The dialog opens on the first output type with its first format chosen, and Copy is the default action.</summary>
    [Fact]
    public void Open_Defaults_ChoosesTheFirstTypeAndMakesCopyTheDefault_Test()
    {
        using DialogSession session = new();

        Assert.Equal(ExportGroups.Rule, session.Dialog.ChosenType);
        Assert.Equal("rule-text", session.Dialog.ChosenEntry!.Artifact!.Id);
        Assert.True(session.Dialog.CopyButton.IsDefault);
        Assert.False(session.Dialog.FileButton.IsDefault);
        Assert.EndsWith("rule-text.rule", session.Dialog.PathField.Text, StringComparison.Ordinal);
    }

    /// <summary>The cursor keys move through the output types and the formats, and Enter takes the default action, Copy.</summary>
    [Fact]
    public void Keyboard_ArrowsAndEnter_ChooseTheEntryAndCopyIt_Test()
    {
        using DialogSession session = new();
        session.Dialog.Types.SetFocus();

        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);

        Assert.Equal(ExportGroups.TruthTable, session.Dialog.ChosenType);
        Assert.Equal(5, session.Dialog.Variants.Source!.Count);

        session.Dialog.Variants.SetFocus();
        session.Press(Key.CursorDown);
        session.Press(Key.Enter);

        Assert.NotNull(session.Dialog.Request);
        Assert.Equal(ExportAction.Copy, session.Dialog.Request.Action);
        Assert.Equal("truth-table-csv", session.Dialog.Request.Entry!.Artifact!.Id);
        Assert.True(session.Closed);
    }

    /// <summary>A double click on a format selects it and does not end the dialog.</summary>
    [Fact]
    public void Mouse_DoubleClickOnAFormat_SelectsItAndKeepsTheDialogOpen_Test()
    {
        using DialogSession session = new();
        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);

        session.DoubleClick(session.Dialog.Variants, 2, 2);

        Assert.Equal("truth-table-json", session.Dialog.ChosenEntry!.Artifact!.Id);
        Assert.Null(session.Dialog.Request);
        Assert.False(session.Closed);
    }

    /// <summary>A double click on an output type selects it and does not end the dialog.</summary>
    [Fact]
    public void Mouse_DoubleClickOnAType_SelectsItAndKeepsTheDialogOpen_Test()
    {
        using DialogSession session = new();

        session.DoubleClick(session.Dialog.Types, 2, 3);

        Assert.Equal(ExportGroups.TruthTable, session.Dialog.ChosenType);
        Assert.Null(session.Dialog.Request);
        Assert.False(session.Closed);
    }

    /// <summary>A click on the Copy button copies the chosen entry.</summary>
    [Fact]
    public void Mouse_ClickOnCopy_ReturnsACopyRequest_Test()
    {
        using DialogSession session = new();
        session.Click(session.Dialog.Types, 2, 1);

        session.Click(session.Dialog.CopyButton, 2, 0);

        Assert.Equal(ExportAction.Copy, session.Dialog.Request!.Action);
        Assert.Equal(string.Empty, session.Dialog.Request.Path);
        Assert.True(session.Closed);
    }

    /// <summary>To File uses the path field of the same dialog, which the author edited.</summary>
    [Fact]
    public void ToFile_EditedPath_ReturnsThatPathInTheSameDialog_Test()
    {
        using DialogSession session = new();
        session.Dialog.PathField.SetFocus();
        session.Dialog.PathField.Text = "/tmp/out/rule.txt";

        session.Press(Key.T.WithAlt);

        Assert.Equal(ExportAction.ToFile, session.Dialog.Request!.Action);
        Assert.Equal("/tmp/out/rule.txt", session.Dialog.Request.Path);
        Assert.True(session.Closed);
    }

    /// <summary>To File with an empty path keeps the dialog open and moves the focus to the field.</summary>
    [Fact]
    public void ToFile_EmptyPath_StaysOpenAndFocusesTheField_Test()
    {
        using DialogSession session = new();
        session.Dialog.PathField.Text = string.Empty;

        session.Click(session.Dialog.FileButton, 2, 0);

        Assert.Null(session.Dialog.Request);
        Assert.False(session.Closed);
        Assert.Same(session.Dialog.PathField, session.Dialog.MostFocused);
    }

    /// <summary>The suggested file name follows the chosen entry until the author edits the path.</summary>
    [Fact]
    public void PathField_ChosenEntryChanges_FollowsUntilEdited_Test()
    {
        using DialogSession session = new();
        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);
        session.Press(Key.CursorDown);
        Assert.EndsWith("truth-table-markdown.md", session.Dialog.PathField.Text, StringComparison.Ordinal);

        session.Dialog.PathField.Text = "/my/own.txt";
        session.Press(Key.CursorDown);

        Assert.Equal("/my/own.txt", session.Dialog.PathField.Text);
    }

    /// <summary>Cancel returns no request and closes the dialog.</summary>
    [Fact]
    public void Cancel_Hotkey_ClosesWithNoRequest_Test()
    {
        using DialogSession session = new();

        session.Press(Key.L.WithAlt);

        Assert.Null(session.Dialog.Request);
        Assert.True(session.Closed);
    }

    /// <summary>The equation type shows dropdowns for the dialect, the envelope and the constants; the choice picks the artifact.</summary>
    [Fact]
    public void Equation_DropdownChoices_PickTheMatchingArtifact_Test()
    {
        using DialogSession session = new();
        session.Press(Key.CursorDown);
        Assert.Equal(ExportGroups.Equation, session.Dialog.ChosenType);
        Assert.True(session.Dialog.Dialect.Visible);
        Assert.False(session.Dialog.Variants.Visible);
        Assert.Equal("equation-unicode-letters", session.Dialog.ChosenEntry!.Artifact!.Id);

        session.Dialog.Dialect.Text = "latex";
        session.Dialog.Envelope.Text = "doubledollar";
        session.Dialog.Constants.Text = "word";

        Assert.Equal("equation-latex-doubledollar-word", session.Dialog.ChosenEntry.Artifact.Id);
        Assert.True(session.Dialog.Envelope.Enabled);

        session.Dialog.Dialect.Text = "mathml";

        Assert.Equal("equation-mathml-word", session.Dialog.ChosenEntry.Artifact.Id);
        Assert.False(session.Dialog.Envelope.Enabled);
    }

    /// <summary>The folder export and the image entries cannot be copied, so To File becomes the default action.</summary>
    [Fact]
    public void FolderAndImage_NoCopy_MakeToFileTheDefault_Test()
    {
        using DialogSession session = new();
        session.Dialog.Types.Value = session.Dialog.Types.Source!.Count - 1;

        Assert.Equal(ExportDialog.FolderType, session.Dialog.ChosenType);
        Assert.Null(session.Dialog.ChosenEntry);
        Assert.False(session.Dialog.CopyButton.Enabled);
        Assert.True(session.Dialog.FileButton.IsDefault);

        session.Dialog.Types.Value = session.Dialog.Types.Source.Count - 2;

        Assert.Equal(ExportGroups.Images, session.Dialog.ChosenType);
        Assert.False(session.Dialog.CopyButton.Enabled);
    }

    /// <summary>The larger-table option starts from the controller and is part of the request.</summary>
    [Fact]
    public void AllowLarger_Checked_IsPartOfTheRequest_Test()
    {
        using DialogSession session = new(allowLarger: true);
        Assert.Equal(Terminal.Gui.Views.CheckState.Checked, session.Dialog.AllowLarger.Value);

        session.Click(session.Dialog.CopyButton, 2, 0);

        Assert.True(session.Dialog.Request!.AllowLargerTruthTable);
    }

    /// <summary>On the headless ANSI driver the dialog draws its headings, the chosen format and the three buttons.</summary>
    [Fact]
    public void Run_OneIteration_DrawsTheHeadingsAndTheButtons_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts());
        ExportController controller = new(workbench, new ScriptedServices().Services);
        using IApplication app = Application.Create();
        app.Init(DriverRegistry.Names.ANSI);
        app.StopAfterFirstIteration = true;
        using ExportDialog dialog = new(
            new ExportDialogModel(controller.Entries(), Path.GetTempPath(), false, "Make big truth tables")
        );

        app.Run(dialog);
        Cell[,] cells = app.Driver!.Contents!;
        StringBuilder text = new();
        for (int row = 0; row < cells.GetLength(0); row++)
        {
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                text.Append(cells[row, column].Grapheme);
            }

            text.Append('\n');
        }

        string screen = text.ToString();
        foreach (
            string expected in new[]
            {
                "Output type",
                "Format",
                "Options",
                "Truth table",
                "rule-text",
                "Copy",
                "To File",
                "Cancel",
            }
        )
        {
            Assert.Contains(expected, screen, StringComparison.Ordinal);
        }
    }

    /// <summary>An export dialog in a window of 100 by 30 cells, with the keys and the mouse sent as a terminal sends them.</summary>
    private sealed class DialogSession : IDisposable
    {
        public DialogSession(bool allowLarger = false)
        {
            Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts());
            ExportController controller = new(workbench, new ScriptedServices().Services);
            this.Dialog = new ExportDialog(
                new ExportDialogModel(controller.Entries(), Path.GetTempPath(), allowLarger, "Make big truth tables")
            );
            this.Dialog.Closed += (_, _) => this.Closed = true;
            this.Dialog.BeginInit();
            this.Dialog.EndInit();
            this.Dialog.Layout(new Size(100, 30));
            this.Dialog.Types.SetFocus();
        }

        public ExportDialog Dialog { get; }

        public bool Closed { get; private set; }

        public void Press(Key key)
        {
            _ = this.Dialog.NewKeyDownEvent(key);
            this.Dialog.Layout(new Size(100, 30));
        }

        public void Click(View view, int x, int y)
        {
            this.Send(view, x, y, MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked);
        }

        // The terminal sends the pair of clicks and then the double-click event.
        public void DoubleClick(View view, int x, int y)
        {
            this.Click(view, x, y);
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
