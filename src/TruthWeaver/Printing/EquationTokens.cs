namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The token table for one <see cref="EquationDialect"/>: the operator tokens, the term and constant formatters, and the
/// envelope. <see cref="EquationPrinter"/> walks the tree and asks this table for every piece of text, so a new dialect
/// is a new table and leaves the walk unchanged.
/// </summary>
internal sealed record EquationTokens
{
    /// <summary>Gets the table for the plain Unicode dialect: the <see cref="OperatorStyle.Symbolic"/> glyphs, no escaping and no envelope.</summary>
    public static EquationTokens Unicode { get; } =
        new()
        {
            And = "∧",
            Or = "∨",
            Not = "¬",
            Xor = "⊕",
            Equivalent = "↔",
            Implies = "→",
            Nand = "↑",
            Nor = "↓",
            FormatTerm = (identity, showArgumentValues) => showArgumentValues ? identity.ToString() : identity.PredicateName,
            FormatConstant = TruthValueText.Canonical,
            FormatFunctionName = name => name,
            Wrap = equation => equation,
        };

    /// <summary>Gets the infix token for <c>AND</c>.</summary>
    public required string And { get; init; }

    /// <summary>Gets the infix token for <c>OR</c>.</summary>
    public required string Or { get; init; }

    /// <summary>
    /// Gets the prefix token for <c>NOT</c>. The walk writes the operand directly after it, so a dialect whose token
    /// needs a separator (a LaTeX command, for example) includes the trailing space in the token.
    /// </summary>
    public required string Not { get; init; }

    /// <summary>Gets the infix token for <c>XOR</c>.</summary>
    public required string Xor { get; init; }

    /// <summary>Gets the infix token for <c>EQUIVALENT</c>.</summary>
    public required string Equivalent { get; init; }

    /// <summary>Gets the infix token for <c>IMPLIES</c>.</summary>
    public required string Implies { get; init; }

    /// <summary>Gets the infix token for <c>NAND</c>.</summary>
    public required string Nand { get; init; }

    /// <summary>Gets the infix token for <c>NOR</c>.</summary>
    public required string Nor { get; init; }

    /// <summary>
    /// Gets the term formatter. It takes the term's identity and whether to show argument values, and returns the
    /// finished term text, escaped for the dialect.
    /// </summary>
    public required Func<TermIdentity, bool, string> FormatTerm { get; init; }

    /// <summary>Gets the constant formatter, which returns the dialect's text for <c>True</c>, <c>False</c> or <c>Unknown</c>.</summary>
    public required Func<TruthValue, string> FormatConstant { get; init; }

    /// <summary>
    /// Gets the formatter for the name of an operator that prints in function-call form, for example <c>AtLeast</c>.
    /// It takes the DSL spelling and returns the dialect's text for it.
    /// </summary>
    public required Func<string, string> FormatFunctionName { get; init; }

    /// <summary>
    /// Gets the envelope function. It takes the finished equation and returns it inside the dialect's delimiters, for
    /// example <c>$$...$$</c>. A dialect with no envelope returns the equation unchanged.
    /// </summary>
    public required Func<string, string> Wrap { get; init; }

    /// <summary>
    /// Gets the table for the LaTeX dialect. A term and a function name are <c>\text</c> groups. The wrap mode sets the
    /// escaping and the envelope.
    /// </summary>
    /// <param name="wrap">The wrap mode.</param>
    /// <returns>The LaTeX token table.</returns>
    public static EquationTokens Latex(LatexWrapMode wrap)
    {
        bool mathJaxSafe = wrap == LatexWrapMode.MathJaxSafe;
        return new()
        {
            And = @"\land",
            Or = @"\lor",
            Not = @"\lnot ",
            Xor = @"\oplus",
            Equivalent = @"\leftrightarrow",
            Implies = @"\rightarrow",
            Nand = @"\uparrow",
            Nor = @"\downarrow",
            FormatTerm = (identity, showArgumentValues) =>
                LatexText.Text(showArgumentValues ? identity.ToString() : identity.PredicateName, mathJaxSafe),
            FormatConstant = value => LatexText.Text(TruthValueText.Canonical(value), mathJaxSafe),
            FormatFunctionName = name => LatexText.Text(name, mathJaxSafe),
            Wrap = wrap switch
            {
                LatexWrapMode.DoubleDollar => equation => $"$${equation}$$",
                LatexWrapMode.MathJaxSafe => equation => $"$`{equation}`$",
                _ => equation => equation,
            },
        };
    }

    /// <summary>Gets the table for the dialect and wrap mode in the options.</summary>
    /// <param name="options">The equation options.</param>
    /// <returns>The dialect's token table.</returns>
    public static EquationTokens For(EquationOptions options)
    {
        return options.Dialect switch
        {
            EquationDialect.Unicode => Unicode,
            EquationDialect.LaTeX => Latex(options.LatexWrap),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Dialect, "Unhandled equation dialect."),
        };
    }
}
