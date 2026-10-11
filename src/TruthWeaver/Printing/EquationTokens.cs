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

    /// <summary>Gets the opening delimiter of a group or of a function call's argument list. The default is <c>(</c>.</summary>
    public string OpenParen { get; init; } = "(";

    /// <summary>Gets the closing delimiter of a group or of a function call's argument list. The default is <c>)</c>.</summary>
    public string CloseParen { get; init; } = ")";

    /// <summary>Gets the separator between the arguments of a function call. The default is a comma and a space.</summary>
    public string ArgumentSeparator { get; init; } = ", ";

    /// <summary>Gets the token between a variable and its term in a legend line. The default is <c>=</c>.</summary>
    public string LegendEquals { get; init; } = "=";

    /// <summary>Gets the formatter for a count argument of a function call, for example the <c>2</c> of <c>AtLeast(2, a, b)</c>.</summary>
    public Func<int, string> FormatNumber { get; init; } = number => number.ToString(CultureInfo.InvariantCulture);

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

    /// <summary>Gets the table for the MathML dialect: <c>&lt;mo&gt;</c> operators and <c>&lt;mtext&gt;</c> text inside one <c>&lt;math&gt;</c> element.</summary>
    public static EquationTokens MathMl { get; } =
        new()
        {
            And = "<mo>∧</mo>",
            Or = "<mo>∨</mo>",
            Not = "<mo>¬</mo>",
            Xor = "<mo>⊕</mo>",
            Equivalent = "<mo>↔</mo>",
            Implies = "<mo>→</mo>",
            Nand = "<mo>↑</mo>",
            Nor = "<mo>↓</mo>",
            OpenParen = "<mo>(</mo>",
            CloseParen = "<mo>)</mo>",
            ArgumentSeparator = " <mo>,</mo> ",
            LegendEquals = "<mo>=</mo>",
            FormatNumber = number => $"<mn>{number.ToString(CultureInfo.InvariantCulture)}</mn>",
            FormatTerm = (identity, showArgumentValues) =>
                MathMlText.Text(showArgumentValues ? identity.ToString() : identity.PredicateName),
            FormatConstant = (value, constants) =>
                constants.IsLogicSymbol(value)
                    ? ConstantFormat.SymbolCommand(value, "<mo>⊤</mo>", "<mo>⊥</mo>")
                    : MathMlText.Text(constants.Text(value)),
            FormatFunctionName = MathMlText.Text,
            FormatVariable = (letter, subscript) =>
                subscript == 0
                    ? $"<mi>{letter}</mi>"
                    : $"<msub><mi>{letter}</mi><mn>{subscript.ToString(CultureInfo.InvariantCulture)}</mn></msub>",
            Wrap = equation => $"<math xmlns=\"{MathMlText.Namespace}\">{equation}</math>",
        };

    /// <summary>Gets the table for the Typst dialect: Typst math symbols, quoted strings for text, and <c>$</c> delimiters.</summary>
    public static EquationTokens Typst { get; } =
        new()
        {
            And = "and",
            Or = "or",
            Not = "not ",
            Xor = "xor",
            Equivalent = "<=>",
            Implies = "=>",
            Nand = "arrow.t",
            Nor = "arrow.b",
            FormatTerm = (identity, showArgumentValues) =>
                TypstText.Quote(showArgumentValues ? identity.ToString() : identity.PredicateName),
            FormatConstant = (value, constants) =>
                constants.IsLogicSymbol(value)
                    ? ConstantFormat.SymbolCommand(value, "top", "bot")
                    : TypstText.Quote(constants.Text(value)),
            FormatFunctionName = TypstText.Quote,
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : $"{letter}_{subscript}",
            Wrap = equation => $"${equation}$",
        };

    /// <summary>
    /// Gets the table for the plain ASCII dialect: <c>/\</c>, <c>\/</c>, <c>~</c>, <c>(+)</c>, <c>&lt;-&gt;</c> and
    /// <c>-&gt;</c>. <c>NAND</c> and <c>NOR</c> have no ASCII symbol and keep their word form. <see cref="AsciiText.Escape"/>
    /// escapes every text to the range U+0020 to U+007E.
    /// </summary>
    public static EquationTokens Ascii { get; } =
        new()
        {
            And = @"/\",
            Or = @"\/",
            Not = "~",
            Xor = "(+)",
            Equivalent = "<->",
            Implies = "->",
            Nand = "NAND",
            Nor = "NOR",
            FormatTerm = (identity, showArgumentValues) =>
                AsciiText.Escape(showArgumentValues ? identity.ToString() : identity.PredicateName),
            FormatConstant = (value, constants) =>
                constants.IsLogicSymbol(value)
                    ? ConstantFormat.SymbolCommand(value, "top", "bot")
                    : AsciiText.Escape(constants.Text(value)),
            FormatFunctionName = AsciiText.Escape,
            FormatVariable = (letter, subscript) => subscript == 0 ? letter.ToString() : $"{letter}_{subscript}",
            Wrap = equation => equation,
        };

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
            EquationDialect.MathML => MathMl,
            EquationDialect.Typst => Typst,
            EquationDialect.Ascii => Ascii,
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.Dialect, "Unhandled equation dialect."),
        };
    }

    /// <summary>Writes a non-negative number with the Unicode subscript digits <c>₀</c> to <c>₉</c>.</summary>
    private static string ToSubscriptDigits(int number)
    {
        return string.Concat(number.ToString(CultureInfo.InvariantCulture).Select(digit => (char)('₀' + (digit - '0'))));
    }
}
