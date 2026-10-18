namespace TruthWeaver.Tool.Tui.Views;

/// <summary>What the view pane shows: the printed text, or the reason that nothing could be printed.</summary>
/// <param name="Text">The printed text, or <see langword="null"/> when the view cannot print.</param>
/// <param name="Reason">Why the view cannot print, or <see langword="null"/> when it printed.</param>
/// <param name="Notice">A line to show above the text, such as the last valid tree notice, or <see langword="null"/>.</param>
internal sealed record ViewOutput(string? Text, string? Reason, string? Notice = null)
{
    /// <summary>Gets a value indicating whether the view printed.</summary>
    public bool Printed => this.Text is not null;

    /// <summary>Gets the text that a screen shows: the notice, then the text or the reason.</summary>
    public string Display =>
        (this.Notice is null ? string.Empty : this.Notice + "\n\n") + (this.Text ?? "Cannot show this view: " + this.Reason);
}
