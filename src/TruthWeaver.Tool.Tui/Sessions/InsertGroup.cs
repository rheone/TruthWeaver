namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>A submenu of the Insert menu: a title and the entries under it.</summary>
/// <param name="Title">The title, such as <c>Connectives</c>.</param>
/// <param name="Items">The entries.</param>
internal sealed record InsertGroup(string Title, IReadOnlyList<InsertItem> Items);
