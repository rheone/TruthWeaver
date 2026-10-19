namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Tui.Sessions;

/// <summary>
/// The part of the workbench that puts a whole new text in place of the rule inside the undo history: a sample today, and
/// any other source of ready text later.
/// </summary>
internal sealed partial class Workbench
{
    /// <summary>
    /// Puts text and its format in place of the rule of the document as one undo step. Unlike <see cref="ReplaceDocument"/>,
    /// the file, the history and the saved state of the document stay, so Ctrl+Z gives the old rule back. The text that waits
    /// for its compile is applied first, so it is its own step. No consent is asked: that is the job of the caller.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <param name="format">The format of <paramref name="text"/>.</param>
    /// <param name="status">The message for the status line.</param>
    public void ReplaceText(string text, RuleFormat format, string status)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(status);

        this.FlushPendingText();
        this.document.SetText(text, format);
        this.SelectNode("$");
        this.SetStatus(status);
    }

    /// <summary>
    /// Puts a sample rule in place of the rule as one undo step. Unsaved changes need the author's consent first, as for
    /// <see cref="Open"/>.
    /// </summary>
    /// <param name="sample">The sample.</param>
    /// <returns><see langword="true"/> when the sample replaced the rule.</returns>
    public bool OpenSample(SampleRule sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        this.FlushPendingText();
        if (this.document.IsDirty && !this.Prompts.ConfirmDiscard($"replace the rule with the sample '{sample.Name}'"))
        {
            return false;
        }

        this.ReplaceText(sample.Text, RuleFormat.Text, $"Sample '{sample.Name}': {sample.Description} Ctrl+Z undoes it.");
        return true;
    }
}
