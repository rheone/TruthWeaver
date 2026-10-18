namespace TruthWeaver.Tool.Tui.Hosting;

using Terminal.Gui.App;
using Terminal.Gui.Views;

/// <summary>The questions to the author as Terminal.Gui dialogs: a file dialog to open and to save, and a message box to confirm.</summary>
/// <param name="app">The running application.</param>
internal sealed class TerminalPrompts(IApplication app) : IWorkbenchPrompts
{
    /// <inheritdoc />
    public string? AskOpenPath()
    {
        using OpenDialog dialog = new() { Title = "Open a rule (rule text, JSON or YAML)" };
        app.Run(dialog);
        return dialog.Canceled || dialog.FilePaths.Count == 0 ? null : dialog.FilePaths[0];
    }

    /// <inheritdoc />
    public string? AskSavePath(string? currentPath)
    {
        using SaveDialog dialog = new()
        {
            Title = "Save as (.rule, .json, .yaml)",
            Path = currentPath ?? Path.Combine(Environment.CurrentDirectory, "rule.rule"),
        };
        app.Run(dialog);
        return dialog.Canceled || string.IsNullOrWhiteSpace(dialog.FileName) ? null : dialog.FileName;
    }

    /// <inheritdoc />
    public bool ConfirmDiscard(string action)
    {
        int? answer = MessageBox.Query(
            app,
            "Unsaved changes",
            $"The rule has unsaved changes. Drop them and {action}?",
            "_Drop changes",
            "_Cancel"
        );
        return answer == 0;
    }
}
