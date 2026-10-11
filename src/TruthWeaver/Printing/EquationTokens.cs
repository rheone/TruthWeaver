namespace TruthWeaver.Printing;

using System.Globalization;
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
            FormatConstant = (value, constants) => constants.Text(value),
            FormatFunctionName = name => name,
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : letter + ToSubscriptDigits(subscript),
            Wrap = equation => equation,
        };

    /// <summary>Gets the table for the AsciiMath dialect: quoted terms between backticks.</summary>
    public static EquationTokens AsciiMath { get; } =
        new()
        {
            And = "^^",
            Or = "vv",
            Not = "not ",
            Xor = "oplus",
            Equivalent = "<=>",
            Implies = "=>",
            Nand = "uarr",
            Nor = "darr",
            FormatTerm = (identity, showArgumentValues) =>
                AsciiMathText.Quote(showArgumentValues ? identity.ToString() : identity.PredicateName),
            FormatConstant = (value, constants) =>
                constants.IsLogicSymbol(value)
                    ? ConstantFormat.SymbolCommand(value, "top", "bot")
                    : AsciiMathText.Quote(constants.Text(value)),
            FormatFunctionName = AsciiMathText.Quote,
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : $"{letter}_{subscript}",
            Wrap = equation => $"`{equation}`",
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
    /// Gets the formatter for <c>If(condition, whenTrue, whenFalse)</c> as a ternary, given the three operands already
    /// printed (and parenthesized, where their own context needs it). <see langword="null"/> (the default for every
    /// dialect but <see cref="EquationDialect.CStyle"/>) means <c>If</c> prints in function-call form instead.
    /// </summary>
    public Func<string, string, string, string>? FormatIf { get; init; }

    /// <summary>
    /// Gets the term formatter. It takes the term's identity and whether to show argument values, and returns the
    /// finished term text, escaped for the dialect.
    /// </summary>
    public required Func<TermIdentity, bool, string> FormatTerm { get; init; }

    /// <summary>
    /// Gets the constant formatter. It takes the value and the <see cref="ConstantFormat"/> and returns the dialect's text
    /// for the constant, escaped for the dialect. A dialect writes its own command for <c>⊤</c> and <c>⊥</c>
    /// (<see cref="ConstantFormat.IsLogicSymbol"/>) and escapes every other text as a literal.
    /// </summary>
    public required Func<TruthValue, ConstantFormat, string> FormatConstant { get; init; }

    /// <summary>
    /// Gets the formatter for the name of an operator that prints in function-call form, for example <c>AtLeast</c>.
    /// It takes the DSL spelling and returns the dialect's text for it.
    /// </summary>
    public required Func<string, string> FormatFunctionName { get; init; }

    /// <summary>
    /// Gets the variable formatter for simple-variable mode. It takes the letter and its numeric subscript (zero for
    /// none) and returns the dialect's text, for example <c>p₁</c>, <c>p_{1}</c> or <c>p_1</c>.
    /// </summary>
    public required Func<char, int, string> FormatVariable { get; init; }

    /// <summary>
    /// Gets the envelope function. It takes the finished equation and returns it inside the dialect's delimiters, for
    /// example <c>$$...$$</c>. A dialect with no envelope returns the equation unchanged.
    /// </summary>
    public required Func<string, string> Wrap { get; init; }

    /// <summary>
    /// Gets the table for the C-family dialect: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>, <c>^</c>, <c>==</c>, and a
    /// ternary for <c>If</c>. <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c> have no C-family spelling and keep their
    /// word form; <c>COALESCE</c> keeps the function-call form in every dialect (<see cref="FormatIf"/> is the only
    /// per-dialect restructuring the printer supports).
    /// </summary>
    public static EquationTokens CStyle { get; } =
        new()
        {
            And = "&&",
            Or = "||",
            Not = "!",
            Xor = "^",
            Equivalent = "==",
            Implies = "IMPLIES",
            Nand = "NAND",
            Nor = "NOR",
            FormatIf = (condition, whenTrue, whenFalse) => $"{condition} ? {whenTrue} : {whenFalse}",
            FormatTerm = (identity, showArgumentValues) => showArgumentValues ? identity.ToString() : identity.PredicateName,
            FormatConstant = (value, constants) => constants.Text(value),
            FormatFunctionName = name => name,
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : letter + ToSubscriptDigits(subscript),
            Wrap = equation => equation,
        };

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
            FormatConstant = (value, constants) =>
                constants.IsLogicSymbol(value)
                    ? ConstantFormat.SymbolCommand(value, @"\top", @"\bot")
                    : LatexText.Text(constants.Text(value), mathJaxSafe),
            FormatFunctionName = name => LatexText.Text(name, mathJaxSafe),
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : $"{letter}_{{{subscript}}}",
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
            EquationDialect.AsciiMath => AsciiMath,
            EquationDialect.CStyle => CStyle,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Dialect, "Unhandled equation dialect."),
        };
    }

    /// <summary>Writes a non-negative number with the Unicode subscript digits <c>₀</c> to <c>₉</c>.</summary>
    private static string ToSubscriptDigits(int number)
    {
        return string.Concat(number.ToString(CultureInfo.InvariantCulture).Select(digit => (char)('₀' + (digit - '0'))));
    }
}
