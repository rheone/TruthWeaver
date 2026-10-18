namespace TruthWeaver.Tool.Tui.Workbench;

using Terminal.Gui.Input;

/// <summary>A key of the workbench and what it does. The key help overlay lists every command.</summary>
/// <param name="Key">The key.</param>
/// <param name="Description">What the key does, for the key help overlay.</param>
/// <param name="Execute">The action.</param>
internal sealed record WorkbenchCommand(Key Key, string Description, Action<WorkbenchWindow> Execute);
