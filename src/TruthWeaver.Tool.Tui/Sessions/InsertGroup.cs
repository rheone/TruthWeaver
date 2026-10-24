namespace TruthWeaver.Tool.Tui.Sessions;

using TruthWeaver.Testing.Fuzzing;

/// <summary>
/// A submenu of the Insert menu: a title, the entries under it and the generator families it stands for. It is the one list
/// of groups: the menu and the Generate rule dialog (<c>RuleGeneration</c>) both read it, so a new group is one new value in
/// <c>InsertItems</c>.
/// </summary>
/// <param name="Title">The title that the menu shows, such as <c>Connectives</c>.</param>
/// <param name="Families">The generator families that ticking this group in the Generate rule dialog allows. It must not be empty.</param>
/// <param name="Items">The entries.</param>
/// <exception cref="ArgumentException"><paramref name="Families"/> is <see cref="RuleOperatorFamilies.None"/>.</exception>
internal sealed record InsertGroup(string Title, RuleOperatorFamilies Families, IReadOnlyList<InsertItem> Items)
{
    /// <summary>
    /// Gets the generator families that ticking this group in the Generate rule dialog allows. The check runs when the group is
    /// created, so a group that stands for no family fails at once and names itself, and not later in the Generate dialog.
    /// </summary>
    public RuleOperatorFamilies Families { get; init; } = Validated(Families, Title);

    private static RuleOperatorFamilies Validated(RuleOperatorFamilies families, string title)
    {
        if (families == RuleOperatorFamilies.None)
        {
            throw new ArgumentException(
                $"The Insert group '{title}' must stand for at least one generator family.",
                nameof(families)
            );
        }

        return families;
    }
}
