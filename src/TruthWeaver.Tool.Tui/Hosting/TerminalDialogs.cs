namespace TruthWeaver.Tool.Tui.Hosting;

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

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
    public int? Choose(string title, IReadOnlyList<string> items)
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
        using Button ok = new() { Text = "_OK", IsDefault = true };
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
}
