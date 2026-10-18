namespace TruthWeaver.Tool.Tui;

/// <summary>The three regions of the workbench window. Each region shows one pane at a time and can hold several.</summary>
internal enum PaneSlot
{
    /// <summary>The left region: the rule text editor.</summary>
    Editor,

    /// <summary>The right region: views of the rule, such as the canonical text (later the structural tree).</summary>
    Side,

    /// <summary>The region below the other two: diagnostics (later assignments, decision and trace).</summary>
    Bottom,
}
