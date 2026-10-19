namespace TruthWeaver.Tool.Tui.Hosting;

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

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
        list.RowRender += (_, args) => this.ColorRow(list, args, row => row < items.Count ? items[row] : null);
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
            Height = Dim.Fill(4),
        };
        list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>([.. visible.Select(i => items[i])]));

        // The description of the chosen row has its own color, and the legend names every color, so no color stands alone.
        using Label detail = new()
        {
            X = 0,
            Y = Pos.Bottom(list),
            Width = Dim.Fill(),
        };
        detail.SetScheme(SyntaxLegend.SchemeOf(SyntaxKind.Description, this.Palette()));
        using SyntaxLegend legend = new(this.Palette()) { X = 0, Y = Pos.Bottom(list) + 1 };
        list.RowRender += (_, args) => this.ColorRow(list, args, row => row < visible.Count ? items[visible[row]] : null);
        using Button ok = new() { Text = "_OK", IsDefault = true };
        using Button cancel = new() { Text = "_Cancel" };

        // The part of the chosen row after " - " is its description.
        void ShowDetail()
        {
            string? line = list.Value is int row && row >= 0 && row < visible.Count ? items[visible[row]] : null;
            int cut = line is null ? -1 : line.IndexOf(" - ", StringComparison.Ordinal);
            detail.Text = cut < 0 ? string.Empty : line![(cut + 3)..];
        }

        // Each change of the box rebuilds the list from the indexes the filter returns.
        box.TextChanged += (_, _) =>
        {
            visible = filter(box.Text ?? string.Empty);
            list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>([.. visible.Select(i => items[i])]));
            ShowDetail();
        };
        list.ValueChanged += (_, _) => ShowDetail();
        ShowDetail();
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
        dialog.Add(prompt, box, list, detail, legend);
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

    // Paints a row of a predicate list in the color of its syntax kind. The chosen row keeps the attribute of the list, so the
    // selection stays visible; the kind is also the first word of the row, so the color is never the only mark.
    private void ColorRow(ListView list, ListViewRowEventArgs args, Func<int, string?> lineOf)
    {
        if (list.Value == args.Row || lineOf(args.Row) is not { } line || SyntaxKinds.OfPaletteLine(line) is not { } kind)
        {
            return;
        }

        ThemePalette palette = this.Palette();
        Attribute normal = list.GetAttributeForRole(VisualRole.Normal);
        args.RowAttribute = new Attribute(
            palette.Of(kind),
            palette.HasColor ? normal.Background : Color.None,
            palette.StyleOf(kind)
        );
    }
}
