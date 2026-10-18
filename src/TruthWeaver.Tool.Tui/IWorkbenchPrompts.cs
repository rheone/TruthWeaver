namespace TruthWeaver.Tool.Tui;

/// <summary>
/// The questions the workbench asks the author: which file to open or save to, and whether to drop unsaved changes. The
/// terminal implementation shows Terminal.Gui dialogs; a test supplies its own answers, so the workbench logic runs
/// without a terminal.
/// </summary>
internal interface IWorkbenchPrompts
{
    /// <summary>Asks for a rule file to open.</summary>
    /// <returns>The path, or <see langword="null"/> when the author cancels.</returns>
    public string? AskOpenPath();

    /// <summary>Asks for the path to save the document to.</summary>
    /// <param name="currentPath">The current path of the document, or <see langword="null"/> for a new document.</param>
    /// <returns>The path, or <see langword="null"/> when the author cancels.</returns>
    public string? AskSavePath(string? currentPath);

    /// <summary>Asks whether to drop the unsaved changes of the document.</summary>
    /// <param name="action">What the author is about to do, such as "quit" or "open another file".</param>
    /// <returns><see langword="true"/> to drop the changes and go on; <see langword="false"/> to stay.</returns>
    public bool ConfirmDiscard(string action);
}
