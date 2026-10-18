namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using TruthWeaver.Tool.Tui.Hosting;

/// <summary>Answers the workbench questions from fixed values and counts the questions, so no dialog opens.</summary>
internal sealed class ScriptedPrompts : IWorkbenchPrompts
{
    /// <summary>Gets or sets the path that <see cref="AskOpenPath"/> returns.</summary>
    public string? OpenPath { get; set; }

    /// <summary>Gets or sets the path that <see cref="AskSavePath"/> returns.</summary>
    public string? SavePath { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="ConfirmDiscard"/> agrees to drop the changes.</summary>
    public bool Discard { get; set; }

    /// <summary>Gets the number of times <see cref="ConfirmDiscard"/> was asked.</summary>
    public int DiscardQuestions { get; private set; }

    /// <inheritdoc />
    public string? AskOpenPath()
    {
        return this.OpenPath;
    }

    /// <inheritdoc />
    public string? AskSavePath(string? currentPath)
    {
        return this.SavePath;
    }

    /// <inheritdoc />
    public bool ConfirmDiscard(string action)
    {
        this.DiscardQuestions++;
        return this.Discard;
    }
}
