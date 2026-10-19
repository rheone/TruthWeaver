namespace TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The menu of the menu bar that holds a command. The order of the values is the order of the menus, left to right. A group
/// with no command and no choice has no menu.
/// </summary>
internal enum CommandGroup
{
    /// <summary>Open, save, import, the rules inside a document, and quit.</summary>
    File,

    /// <summary>Undo, redo and the formatting of the rule text.</summary>
    Edit,

    /// <summary>Entries that put new text into the rule.</summary>
    Insert,

    /// <summary>Convert and rewrite the rule, and the values and the policy that the rule is tried with.</summary>
    Rule,

    /// <summary>Validation, comparisons and the rule tests.</summary>
    Analyze,

    /// <summary>Write the rule and its artifacts to files or the clipboard.</summary>
    Export,

    /// <summary>The focus, the panes, the size of the regions and the theme.</summary>
    View,

    /// <summary>The menus and the key reference.</summary>
    Help,
}
