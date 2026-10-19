namespace TruthWeaver.Tool.Tui.Theme;

/// <summary>
/// The kinds of text that the workbench colors in the rule text, in the predicate lists and in the Insert menu. Each kind has
/// its own color in <see cref="ThemePalette"/> and its own style when there is no color. The name of a kind is also written
/// (see <see cref="SyntaxKinds.NameOf"/>), so color is never the only mark.
/// </summary>
internal enum SyntaxKind
{
    /// <summary>A predicate: a call such as <c>isAdult(age: 18)</c>, or a name that stands for a term.</summary>
    Predicate,

    /// <summary>An operator: a word such as <c>AND</c> or <c>AtLeast</c>, or a symbol such as <c>&amp;&amp;</c>.</summary>
    Operator,

    /// <summary>A variable: a <c>from(...)</c> read, a definition parameter or the variable of a quantifier.</summary>
    Variable,

    /// <summary>A constant: <c>True</c>, <c>False</c> or <c>Unknown</c>, or the letter form.</summary>
    Constant,

    /// <summary>Description text: a comment in the rule text, or the description of a list entry.</summary>
    Description,
}
