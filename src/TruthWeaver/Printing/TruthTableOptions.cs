namespace TruthWeaver.Printing;

/// <summary>The options for <see cref="Evaluation.CompiledRule{TContext}.PrintTruthTable(TruthTableOptions?)"/>.</summary>
public sealed record TruthTableOptions
{
    /// <summary>
    /// The number of terms that a table can have without <see cref="AllowLargerTable"/>: 8. A Strong K3 table of 8 terms has
    /// 6,561 rows.
    /// </summary>
    public const int DefaultTermCap = 8;

    /// <summary>
    /// The default value of <see cref="TermLimit"/>: 12. A Strong K3 table of 12 terms has 531,441 rows. A rule with more
    /// terms is refused, whatever else the options say.
    /// </summary>
    public const int HardTermLimit = 12;

    private readonly int termLimit = HardTermLimit;

    /// <summary>Gets the default options: a Strong K3 table in Markdown, with the terms as calls and the constants as letters.</summary>
    public static TruthTableOptions Default { get; } = new();

    /// <summary>Gets the format of the table. The default is <see cref="TruthTableFormat.Markdown"/>.</summary>
    public TruthTableFormat Format { get; init; } = TruthTableFormat.Markdown;

    /// <summary>
    /// Gets a value indicating whether the table is the classical one: only <c>False</c> and <c>True</c> for each term, so
    /// 2<sup>n</sup> rows instead of 3<sup>n</sup>. A rule can still give <c>Unknown</c> in a classical row, for example when
    /// it holds the constant <c>Unknown</c>. The default is <see langword="false"/>, the Strong K3 table.
    /// </summary>
    public bool Classical { get; init; }

    /// <summary>
    /// Gets a value indicating whether a rule with more than <see cref="DefaultTermCap"/> terms is printed anyway, up to
    /// <see cref="TermLimit"/>. The result then carries a <c>TRE0034</c> warning. The default is <see langword="false"/>.
    /// </summary>
    public bool AllowLargerTable { get; init; }

    /// <summary>
    /// Gets the most distinct terms that a table can have. A rule with more terms is refused with a <c>TRE0035</c> error,
    /// whatever <see cref="AllowLargerTable"/> says. The default is <see cref="HardTermLimit"/> (12). There is no ceiling:
    /// a larger value is allowed, and the caller owns the cost, because a Strong K3 table of n terms has 3<sup>n</sup>
    /// rows (2<sup>n</sup> when <see cref="Classical"/>). Write to a <see cref="TextWriter"/> to keep memory flat. A value
    /// below the <see cref="DefaultTermCap"/> lowers the cap that applies without <see cref="AllowLargerTable"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int TermLimit
    {
        get => this.termLimit;
        init => this.termLimit = AtLeastOne(value);
    }

    /// <summary>
    /// Gets a value indicating whether each term is a letter (<c>p</c>, <c>q</c>, <c>r</c>) in the header instead of its call.
    /// The letters are the same as the ones that <see cref="Evaluation.CompiledRule{TContext}.PrintEquationWithLegend(EquationOptions?)"/>
    /// uses, so its legend names them. The default is <see langword="false"/>.
    /// </summary>
    public bool SimpleVariables { get; init; }

    /// <summary>
    /// Gets a value indicating whether a term header shows its argument values, for example <c>hasCrust(crust: "thin")</c>.
    /// When <see langword="false"/>, the header is the predicate name only. The default is <see langword="true"/>.
    /// </summary>
    public bool ShowArgumentValues { get; init; } = true;

    /// <summary>
    /// Gets how the constants <c>True</c>, <c>False</c> and <c>Unknown</c> are written in the cells. The default is
    /// <see cref="ConstantStyle.Letters"/> (<c>T</c>, <c>F</c>, <c>U</c>).
    /// </summary>
    public ConstantFormat Constants { get; init; } = new() { Style = ConstantStyle.Letters };

    private static int AtLeastOne(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return value;
    }
}
