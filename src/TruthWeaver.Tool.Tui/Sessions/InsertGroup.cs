namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>
/// The identity of a submenu of the Insert menu. It is the one list of groups: the menu, and the Generate rule families
/// (<c>RuleGeneration</c>), read it, so a new group is one new value here plus its family.
/// </summary>
internal enum InsertGroupId
{
    /// <summary>The Strong Kleene connectives, such as <c>AND</c> and <c>NOT</c>.</summary>
    Connectives,

    /// <summary>The cardinality operators, such as <c>AtLeast</c> and <c>ANY</c>.</summary>
    Cardinality,

    /// <summary>The inspection operators, such as <c>IsKnown</c>.</summary>
    Inspection,

    /// <summary>The operators that fit no other group: <c>If</c> and <c>COALESCE</c>.</summary>
    Other,
}

/// <summary>A submenu of the Insert menu: its identity, a title and the entries under it.</summary>
/// <param name="Id">The identity of the group. Code that needs to tell groups apart uses this, not the title.</param>
/// <param name="Title">The title that the menu shows, such as <c>Connectives</c>.</param>
/// <param name="Items">The entries.</param>
internal sealed record InsertGroup(InsertGroupId Id, string Title, IReadOnlyList<InsertItem> Items);
