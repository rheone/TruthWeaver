namespace TruthWeaver.Tool.Tui.Theme;

/// <summary>The names of the <see cref="SyntaxKind"/> values, and the kind of a line of a palette list.</summary>
internal static class SyntaxKinds
{
    /// <summary>Gets every kind, in the order of the legend.</summary>
    public static IReadOnlyList<SyntaxKind> All { get; } = Enum.GetValues<SyntaxKind>();

    /// <summary>Gets the lower-case name of a kind, as the legend and the help write it.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name, such as <c>predicate</c>.</returns>
    public static string NameOf(SyntaxKind kind)
    {
        return kind.ToString().ToLowerInvariant();
    }

    /// <summary>Gets the legend line: the names of the kinds, in the order of <see cref="All"/>.</summary>
    /// <returns>The text, such as <c>predicate  operator  variable  constant  description</c>.</returns>
    public static string LegendText()
    {
        return string.Join("  ", All.Select(NameOf));
    }

    /// <summary>
    /// Gets the kind of a line of a predicate list. The line starts with the source of the entry
    /// (<c>PaletteEntry.Display</c>): <c>operator</c>, <c>constant</c>, or a predicate source (<c>schema</c>, <c>catalog</c>,
    /// <c>rule</c>, <c>let</c>). Any other line, such as a group line, has no kind.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns>The kind, or <see langword="null"/>.</returns>
    public static SyntaxKind? OfPaletteLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        string source = line.Split(' ', 2)[0];
        return source switch
        {
            "operator" => SyntaxKind.Operator,
            "constant" => SyntaxKind.Constant,
            "schema" or "catalog" or "rule" or "let" => SyntaxKind.Predicate,
            _ => null,
        };
    }
}
