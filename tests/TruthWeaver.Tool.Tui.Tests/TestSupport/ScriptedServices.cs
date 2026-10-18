namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The scripted dialogs and the in-memory clipboard of a test.</summary>
internal sealed class ScriptedServices
{
    /// <summary>Gets the dialogs.</summary>
    public ScriptedDialogs Dialogs { get; } = new();

    /// <summary>Gets the clipboard.</summary>
    public ScriptedClipboard Clipboard { get; } = new();

    /// <summary>Gets the services record that a pane takes.</summary>
    public WorkbenchServices Services => new(this.Dialogs, this.Clipboard);
}
