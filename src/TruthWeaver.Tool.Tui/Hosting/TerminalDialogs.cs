namespace TruthWeaver.Tool.Tui.Hosting;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Views;

/// <summary>The extra questions as Terminal.Gui dialogs.</summary>
/// <param name="app">The running application.</param>
internal sealed class TerminalDialogs(IApplication app) : IWorkbenchDialogs
{
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

        int? result = null;
        using Dialog dialog = new()
        {
            Title = title,
            Width = Dim.Percent(80),
            Height = Dim.Percent(80),
        };
        using ListView list = new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));
        using Button ok = new() { Text = accept, IsDefault = true };
        using Button cancel = new() { Text = "_Cancel" };
        ok.Accepting += (_, args) =>
        {
            result = list.Value;
            args.Handled = true;
            app.RequestStop();
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            app.RequestStop();
        };
        dialog.Add(list);
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        list.SetFocus();
        app.Run(dialog);
        return result;
    }

    /// <inheritdoc />
    public int? ChooseFiltered(string title, IReadOnlyList<string> items, Func<string, IReadOnlyList<int>> filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filter);

        int? result = null;
        IReadOnlyList<int> visible = filter(string.Empty);
        using Dialog dialog = new()
        {
            Title = title,
            Width = Dim.Percent(80),
            Height = Dim.Percent(80),
        };
        using Label prompt = new()
        {
            Text = "Search:",
            X = 0,
            Y = 0,
        };
        using TextField box = new()
        {
            X = 8,
            Y = 0,
            Width = Dim.Fill(),
        };
        using ListView list = new()
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>([.. visible.Select(i => items[i])]));
        using Button ok = new() { Text = "_OK", IsDefault = true };
        using Button cancel = new() { Text = "_Cancel" };

        // Each change of the box rebuilds the list from the indexes the filter returns.
        box.TextChanged += (_, _) =>
        {
            visible = filter(box.Text ?? string.Empty);
            list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>([.. visible.Select(i => items[i])]));
        };
        ok.Accepting += (_, args) =>
        {
            result = list.Value is int row && row >= 0 && row < visible.Count ? visible[row] : null;
            args.Handled = true;
            app.RequestStop();
        };
        cancel.Accepting += (_, args) =>
        {
            args.Handled = true;
            app.RequestStop();
        };
        dialog.Add(prompt, box, list);
        dialog.AddButton(ok);
        dialog.AddButton(cancel);
        box.SetFocus();
        app.Run(dialog);
        return result;
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
}
