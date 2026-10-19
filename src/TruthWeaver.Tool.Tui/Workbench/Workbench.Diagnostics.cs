namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;

/// <summary>
/// The part of the workbench state that keeps typing responsive: the editor hands its text to <see cref="EditText"/>, and
/// after a pause the text compiles on a background task. The result replaces the text of the document only when no newer
/// edit exists.
/// </summary>
internal sealed partial class Workbench
{
    /// <summary>The pause the terminal application uses between the last edit and the compile.</summary>
    public static readonly TimeSpan DefaultDiagnosticsDelay = TimeSpan.FromMilliseconds(300);

    private Action? cancelCheck;
    private string? pendingText;
    private int editVersion;

    /// <summary>
    /// Gets or sets the pause after the last edit before the text compiles. <see cref="TimeSpan.Zero"/> (the default) compiles
    /// each edit at once on the calling thread, which keeps a test deterministic. The terminal application sets a pause.
    /// </summary>
    public TimeSpan DiagnosticsDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets how a background result gets back to the thread of the editor. The terminal application sets it to the
    /// invoke method of its loop. The default runs the action on the calling thread.
    /// </summary>
    public Action<Action> Post { get; set; } = static action => action();

    /// <summary>
    /// Gets the open document without applying the pending text. Only code that must not end the pause, such as the status
    /// line that shows <c>checking</c>, reads this; every other caller reads <see cref="Document"/>.
    /// </summary>
    public RuleDocument CommittedDocument => this.document;

    /// <summary>Gets a value indicating whether an edit waits for its compile: the pause runs or the compile runs.</summary>
    public bool IsChecking => this.pendingText is not null;

    /// <summary>
    /// Hands the text of the editor to the document. With a <see cref="DiagnosticsDelay"/>, the document keeps its old text
    /// until the pause ends and the background compile finishes; each new edit restarts the pause and drops the result of
    /// the older edit. Reading <see cref="Document"/> in the meantime applies the pending text first, so no action works on
    /// text that the author has already replaced.
    /// </summary>
    /// <param name="text">The text of the editor.</param>
    public void EditText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (this.DiagnosticsDelay <= TimeSpan.Zero)
        {
            this.DiscardPending();
            this.document.SetText(text);
            return;
        }

        this.DiscardPending();
        if (text == this.document.Text)
        {
            this.StatusChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        CancellationTokenSource cancel = new();
        this.cancelCheck = () =>
        {
            cancel.Cancel();
            cancel.Dispose();
        };
        this.pendingText = text;
        int version = ++this.editVersion;
        this.StatusChanged?.Invoke(this, EventArgs.Empty);
        _ = this.CheckAsync(this.document, text, this.document.Format, version, this.DiagnosticsDelay, cancel.Token);
    }

    /// <summary>Applies the pending text to the document now, on the calling thread, and drops the background compile.</summary>
    public void FlushPendingText()
    {
        if (this.pendingText is not { } text)
        {
            return;
        }

        this.DiscardPending();
        this.document.SetText(text);
        this.StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    // Waits for the pause, compiles off the editor thread and posts the result back.
    private async Task CheckAsync(
        RuleDocument target,
        string text,
        Compilation.RuleFormat format,
        int version,
        TimeSpan delay,
        CancellationToken cancellation
    )
    {
        // SuppressThrowing waits without raising TaskCanceledException, so a superseded compile never produces a
        // first-chance exception in a debugger. Cancellation is read from the token and the task state instead.
        await Task.Delay(delay, cancellation).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cancellation.IsCancellationRequested)
        {
            return;
        }

        Task<CheckedText> compile = Task.Run(() => target.Check(text, format), cancellation);

        // SuppressThrowing is only valid on the non-generic Task.
        await ((Task)compile).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cancellation.IsCancellationRequested || compile.Status != TaskStatus.RanToCompletion)
        {
            // A newer edit, a flush or another document replaced this one; nothing to report.
            return;
        }

        CheckedText result = await compile.ConfigureAwait(false);
        this.Post(() => this.Complete(version, target, result));
    }

    // A result applies only when it belongs to the newest edit of the document that is still open.
    private void Complete(int version, RuleDocument target, CheckedText result)
    {
        if (version != this.editVersion || !ReferenceEquals(target, this.document) || this.pendingText is null)
        {
            return;
        }

        this.DiscardPending();
        this.document.SetText(result);
        this.StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DiscardPending()
    {
        this.editVersion++;
        this.pendingText = null;
        if (this.cancelCheck is { } cancel)
        {
            this.cancelCheck = null;
            cancel();
        }
    }
}
