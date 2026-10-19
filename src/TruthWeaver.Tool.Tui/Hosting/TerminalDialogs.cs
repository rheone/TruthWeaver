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
        this.Run(dialog, h => dialog.Closed += h, dialog.List);
        return dialog.Choice;
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

        using TickBoxDialog dialog = new(title, items, initial, accept);
        this.Run(dialog, h => dialog.Closed += h, dialog.Boxes.Count > 0 ? dialog.Boxes[0] : null);
        return dialog.Ticked;
    }

    /// <inheritdoc />
    public int? ChooseFiltered(string title, IReadOnlyList<string> items, Func<string, IReadOnlyList<int>> filter)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(filter);

        using ChoiceDialog dialog = new(title, items, "_OK", filter, this.Palette);
        this.Run(dialog, h => dialog.Closed += h, dialog.SearchBox);
        return dialog.Choice;
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

    // The run steps that every list dialog shares: stop the app when the dialog reports that it closed, focus the first control
    // and run the dialog. The caller reads the result from the dialog afterwards.
    private void Run(Dialog dialog, Action<EventHandler> subscribeClosed, View? focus)
    {
        subscribeClosed((_, _) => app.RequestStop());
        _ = focus?.SetFocus();
        app.Run(dialog);
    }
}
