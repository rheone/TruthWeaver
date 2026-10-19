namespace TruthWeaver.Tool.Tui.Workbench;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Tool.Tui.Hosting;

/// <summary>
/// The dialogs and the clipboard that the panes use. The program sets <see cref="Current"/> once the terminal runs; until
/// then a pane gets answers that always cancel, so no pane blocks without a terminal.
/// </summary>
/// <param name="Dialogs">The dialogs.</param>
/// <param name="Clipboard">The clipboard.</param>
internal sealed record WorkbenchServices(IWorkbenchDialogs Dialogs, IClipboardAccess Clipboard)
{
    /// <summary>Gets or sets the services that panes use when they are not given their own.</summary>
    public static WorkbenchServices Current { get; set; } = new(new NoDialogs(), new NoClipboard());

    private sealed class NoDialogs : IWorkbenchDialogs
    {
        public string? AskText(string title, string initial)
        {
            return null;
        }

        public int? Choose(string title, IReadOnlyList<string> items)
        {
            return null;
        }

        public int? ChooseFiltered(string title, IReadOnlyList<string> items, Func<string, IReadOnlyList<int>> filter)
        {
            return null;
        }

        public string? AskFolder(string title)
        {
            return null;
        }

        public bool Confirm(string title, string message, string confirm)
        {
            return false;
        }
    }

    private sealed class NoClipboard : IClipboardAccess
    {
        public bool TrySet(string text)
        {
            return false;
        }

        public bool TryGet([NotNullWhen(true)] out string? text)
        {
            text = null;
            return false;
        }
    }
}
