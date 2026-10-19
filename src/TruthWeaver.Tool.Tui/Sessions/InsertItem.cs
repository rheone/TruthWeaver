namespace TruthWeaver.Tool.Tui.Sessions;

/// <summary>One entry of the Insert menu: the text that goes into the editor, with a one-line description.</summary>
/// <param name="Name">The name in the menu, such as <c>AND</c> or <c>True</c>.</param>
/// <param name="Text">The rule text to insert. It uses the placeholder terms <c>a</c>, <c>b</c> and <c>c</c> for the operands.</param>
/// <param name="Description">One line that says what the entry means. It comes from the operator table of the library.</param>
/// <param name="Context">
/// The rule text that the entry compiles in, with <c>{0}</c> in the place of <paramref name="Text"/>. Most entries are a whole
/// rule, so the default is the text itself; a variable is an argument and a definition needs a use.
/// </param>
internal sealed record InsertItem(string Name, string Text, string Description, string Context = "{0}")
{
    /// <summary>Gets the text of a rule that holds this entry, for a check that it compiles.</summary>
    public string InRule => this.Context.Replace("{0}", this.Text, StringComparison.Ordinal);
}
