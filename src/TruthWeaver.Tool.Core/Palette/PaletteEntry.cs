namespace TruthWeaver.Tool.Core.Palette;

/// <summary>
/// One entry of a <see cref="RulePalette"/>: an operator, a constant, or a predicate as a template with its arguments.
/// </summary>
/// <param name="Source">Where the entry comes from.</param>
/// <param name="Name">
/// The name: the operator name in the JSON tree format (such as <c>atLeast</c>), the constant (<c>True</c>, <c>False</c>
/// or <c>Unknown</c>), or the predicate name.
/// </param>
/// <param name="Description">What the entry means, or an empty string.</param>
/// <param name="Arguments">The arguments of a predicate; empty for an operator or a constant.</param>
public sealed record PaletteEntry(
    PaletteSource Source,
    string Name,
    string Description,
    IReadOnlyList<PaletteArgument> Arguments
)
{
    /// <summary>Gets a line for a list: the source, the name and the arguments with their kinds.</summary>
    public string Display
    {
        get
        {
            string source = this.Source switch
            {
                PaletteSource.Operator => "operator",
                PaletteSource.Constant => "constant",
                PaletteSource.SchemaFile => "schema",
                PaletteSource.Catalog => "catalog",
                PaletteSource.Definition => "let",
                _ => "rule",
            };
            string arguments =
                this.Arguments.Count == 0
                    ? string.Empty
                    : "("
                        + string.Join(
                            ", ",
                            this.Arguments.Select(a => $"{a.Name}: {a.Kind}{(a.Required ? string.Empty : "?")}")
                        )
                        + ")";
            return source.PadRight(8) + " " + this.Name + arguments;
        }
    }
}
