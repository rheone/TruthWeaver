namespace TruthWeaver.Tool.Tui.Hosting;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;

/// <summary>The extra questions as Terminal.Gui dialogs.</summary>
/// <param name="app">The running application.</param>
internal sealed class TerminalDialogs(IApplication app) : IWorkbenchDialogs
{
    /// <summary>
    /// Gets or sets the colors of the current theme. The predicate lists color each row by its syntax kind with them. The
    /// program sets it from the workbench once the workbench exists.
    /// </summary>
    public Func<ThemePalette> Palette { get; set; } = () => ThemePalette.Dark;

    /// <inheritdoc />
    public string? AskText(string title, string initial)
    {
        string? result = null;
        using Dialog dialog = new()
        {
            Title = title,
            Width = Dim.Percent(70),
            Height = 8,
        };
        using TextField field = new()
        {
            Text = initial,
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
        };
        using Button ok = new() { Text = "_OK", IsDefault = true };
        using Button cancel = new() { Text = "_Cancel" };
        ok.Accepting += (_, args) =>
        {
            result = field.Text;
            args.Handled = true;
            app.RequestStop();
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            app.RequestStop();
        };
        dialog.Add(field);
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        field.SetFocus();
        app.Run(dialog);
        return result;
    }

    /// <inheritdoc />
    public int? Choose(string title, IReadOnlyList<string> items, string accept = "_OK")
    {
        ArgumentNullException.ThrowIfNull(items);

        using ChoiceDialog dialog = new(title, items, accept, null, this.Palette);
        return this.Run(dialog, dialog.List);
    }

    /// <inheritdoc />
    public IReadOnlyList<int>? ChooseMany(
        string title,
        IReadOnlyList<string> items,
        IReadOnlyList<int> initial,
        string accept = "_OK"
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(initial);

        IReadOnlyList<int>? result = null;
        using Dialog dialog = new()
        {
            Title = title,
            Width = Dim.Percent(60),
            Height = items.Count + 8,
        };
        CheckBox[] boxes =
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
        using Label hint = new()
        {
            Text = "Tick at least one.",
            X = 1,
            Y = items.Count + 1,
        };
        using Button ok = new() { Text = accept, IsDefault = true };
        using Button cancel = new() { Text = "_Cancel" };
        ok.Accepting += (_, args) =>
        {
            args.Handled = true;
            int[] ticked = [.. Enumerable.Range(0, boxes.Length).Where(i => boxes[i].Value == CheckState.Checked)];

            // Nothing ticked: the dialog stays open, as the hint says.
            if (ticked.Length > 0)
            {
                result = ticked;
                app.RequestStop();
            }
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            app.RequestStop();
        };
        foreach (CheckBox box in boxes)
        {
            dialog.Add(box);
        }

        dialog.Add(hint);
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        if (boxes.Length > 0)
        {
            boxes[0].SetFocus();
        }

        app.Run(dialog);
        foreach (CheckBox box in boxes)
        {
            box.Dispose();
        }

        return result;
    }

    /// <inheritdoc />
    public int? ChooseFiltered(string title, IReadOnlyList<string> items, Func<string, IReadOnlyList<int>> filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filter);

        using ChoiceDialog dialog = new(title, items, "_OK", filter, this.Palette);
        return this.Run(dialog, dialog.SearchBox!);
    }

    /// <inheritdoc />
    public string? AskFolder(string title)
    {
        using OpenDialog dialog = new() { Title = title, OpenMode = OpenMode.Directory };
        app.Run(dialog);
        return dialog.Canceled || dialog.FilePaths.Count == 0 ? null : dialog.FilePaths[0];
    }

    /// <inheritdoc />
    public bool Confirm(string title, string message, string confirm)
    {
        return MessageBox.Query(app, title, message, "_" + confirm, "_Cancel") == 0;
    }

    /// <inheritdoc />
    public ExportRequest? AskExport(ExportDialogModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        using ExportDialog dialog = new(model);
        dialog.Closed += (_, _) => app.RequestStop();
        dialog.Types.SetFocus();
        app.Run(dialog);
        return dialog.Request;
    }

    private int? Run(ChoiceDialog dialog, View focus)
    {
        dialog.Closed += (_, _) => app.RequestStop();
        _ = focus.SetFocus();
        app.Run(dialog);
        return dialog.Choice;
    }
}
