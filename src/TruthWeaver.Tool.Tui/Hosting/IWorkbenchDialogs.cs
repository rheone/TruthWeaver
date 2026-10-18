namespace TruthWeaver.Tool.Tui.Hosting;

/// <summary>
/// The questions the view, embedded-rule and export panes ask beyond <see cref="IWorkbenchPrompts"/>: a line of text, a
/// choice from a list, a folder, and a plain yes or no. The terminal implementation shows Terminal.Gui dialogs; a test
/// supplies its own answers.
/// </summary>
internal interface IWorkbenchDialogs
{
    /// <summary>Asks for one line of text.</summary>
    /// <param name="title">The title of the question.</param>
    /// <param name="initial">The text to start with.</param>
    /// <returns>The text, or <see langword="null"/> when the author cancels.</returns>
    public string? AskText(string title, string initial);

    /// <summary>Asks the author to choose one entry of a list.</summary>
    /// <param name="title">The title of the question.</param>
    /// <param name="items">The entries.</param>
    /// <returns>The index of the entry, or <see langword="null"/> when the author cancels.</returns>
    public int? Choose(string title, IReadOnlyList<string> items);

    /// <summary>Asks for a folder.</summary>
    /// <param name="title">The title of the question.</param>
    /// <returns>The folder path, or <see langword="null"/> when the author cancels.</returns>
    public string? AskFolder(string title);

    /// <summary>Asks a question with a confirming and a cancelling answer.</summary>
    /// <param name="title">The title of the question.</param>
    /// <param name="message">The question.</param>
    /// <param name="confirm">The label of the confirming answer, such as "Overwrite".</param>
    /// <returns><see langword="true"/> when the author confirms.</returns>
    public bool Confirm(string title, string message, string confirm);
}
