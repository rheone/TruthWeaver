namespace TruthWeaver.Printing;

using System.Globalization;
using System.Text;
using TruthWeaver.Ast;

/// <summary>
/// Renders a compiled expression tree back to canonical DSL text (ADR-0003): deterministic, and
/// parenthesized for clarity wherever an operator is mixed with a different one, even where
/// precedence alone would make the meaning unambiguous (e.g. <c>a AND b OR c</c> prints as
/// <c>(a AND b) OR c</c>). The point is to make a large nested rule easy for a human to read at a
/// glance, not merely to avoid a parser error. <c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c> are always parenthesized
/// regardless of context. This is the exact form <c>parse</c> reproduces a structurally equal tree
/// from.
/// </summary>
/// <remarks>
/// <see cref="OperatorStyle.Symbolic"/> swaps <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c>
/// for the symbols the DSL lexer itself reads for them (<c>DslParser.SymbolAlias</c>), so
/// the printed text still re-parses to an equal tree. <c>COALESCE</c> and <c>If</c> are unaffected: the DSL has no call
/// syntax for <c>??</c>, so they always print in word/function-call form. <see cref="OperatorStyle.CStyle"/> is not
/// supported here. The caller (<c>CompiledRule.PrintRuleText</c>) rejects it before this printer ever sees it, since
/// <c>^</c> and <c>==</c> are not DSL tokens.
/// </remarks>
internal static class CanonicalPrinter
{
    private enum PrintContext
    {
        Top,
        AndOperand,
        OrOperand,
        NotOperand,
        InfixOperand,
    }

    /// <summary>Prints an expression tree to canonical DSL text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <param name="grouping">
    /// The grouping delimiters to wrap operands in. <see cref="GroupingStyle.Parentheses"/> (the default) is the canonical
    /// form; <see cref="GroupingStyle.DepthCycling"/> prints the same tree with the delimiter chosen by group depth.
    /// </param>
    /// <param name="operatorStyle">
    /// How to spell the binary connectives. <see cref="OperatorStyle.Word"/> (the default) or
    /// <see cref="OperatorStyle.Symbolic"/>; the caller is expected to have already rejected
    /// <see cref="OperatorStyle.CStyle"/>, which has no DSL spelling.
    /// </param>
    /// <param name="constants">
    /// How to spell <c>True</c>, <c>False</c> and <c>Unknown</c>, or <see langword="null"/> for the word form. The caller is
    /// expected to have already rejected a format that the parser does not read back
    /// (<see cref="ConstantFormat.IsRuleTextSafe"/>).
    /// </param>
    /// <param name="notes">
    /// The notes to write as comments, or <see langword="null"/> (the default) for none. Each note is written as a leading
    /// comment, and a noted infix node is put in a group so the comment attaches to it when the text is read back.
    /// </param>
    /// <returns>The DSL text.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</exception>
    public static string Print(
        Expression root,
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null,
        NoteLookup? notes = null
    )
    {
        return PrintNode(root, PrintContext.Top, 0, grouping, operatorStyle, constants ?? ConstantFormat.Default, notes);
    }

    /// <summary>
    /// Prints an expression tree to DSL text using <paramref name="options"/>. When
    /// <see cref="RuleTextOptions.Width"/> is <see langword="null"/>, this returns exactly what
    /// <see cref="Print(Expression, GroupingStyle, OperatorStyle, ConstantFormat?, NoteLookup?)"/> returns for the same grouping,
    /// operator style and constants. Otherwise, a group that does not fit the width breaks across lines: see the
    /// breaking rule documented in <c>docs/rule-text.md</c>.
    /// </summary>
    /// <param name="root">The tree to print.</param>
    /// <param name="options">The printing options. The caller is expected to have already rejected
    /// <see cref="OperatorStyle.CStyle"/> and a <see cref="ConstantFormat"/> the parser does not read back (the same
    /// checks <c>CompiledRule.PrintRuleText</c> makes for the other overloads).</param>
    /// <param name="notes">The notes to write as comments, or <see langword="null"/> for none. <see cref="RuleTextOptions.IncludeNotes"/> is the caller's to apply.</param>
    /// <returns>The DSL text, one line or many.</returns>
    public static string Print(Expression root, RuleTextOptions options, NoteLookup? notes = null)
    {
        ConstantFormat constants = options.Constants ?? ConstantFormat.Default;
        if (options.Width is null)
        {
            return PrintNode(root, PrintContext.Top, 0, options.Grouping, options.OperatorStyle, constants, notes);
        }

        return PrintPretty(
            root,
            PrintContext.Top,
            0,
            0,
            options.Grouping,
            options.OperatorStyle,
            constants,
            options.Width,
            options.IndentSize,
            notes
        );
    }

    /// <summary>
    /// Whether a node prints with a group around it. Besides the precedence rules, a noted <c>AND</c>, <c>OR</c> or binary
    /// connective always gets one: its text otherwise starts with its first operand's text, so a leading comment would
    /// attach to that operand when the text is read back.
    /// </summary>
    private static bool NeedsWrap(Expression node, PrintContext context, string? note)
    {
        return NeedsWrap(node, context) || (note is not null && node is AndExpression or OrExpression);
    }

    private static bool NeedsWrap(Expression node, PrintContext context)
    {
        return (node, context) switch
        {
            (XorExpression, _) => true,
            (EquivalentExpression, _) => true,
            (ImpliesExpression, _) => true,
            (NandExpression, _) => true,
            (NorExpression, _) => true,
            (
                AndExpression,
                PrintContext.AndOperand
                    or PrintContext.OrOperand
                    or PrintContext.NotOperand
                    or PrintContext.InfixOperand
            ) => true,
            (
                OrExpression,
                PrintContext.AndOperand
                    or PrintContext.OrOperand
                    or PrintContext.NotOperand
                    or PrintContext.InfixOperand
            ) => true,
            _ => false,
        };
    }

    /// <summary>
    /// Writes a note as the comment that goes in front of its node: a block comment, or line comments (each ended by a line
    /// feed) when the note contains <c>*/</c>, which would end a block comment early.
    /// </summary>
    private static string NoteComment(string note)
    {
        if (!note.Contains("*/", StringComparison.Ordinal))
        {
            return $"/* {note} */ ";
        }

        StringBuilder builder = new();
        foreach (string line in note.Split('\n'))
        {
            builder.Append(line.Length == 0 ? "//" : $"// {line}").Append('\n');
        }

        return builder.ToString();
    }

    private static string PrintNode(
        Expression node,
        PrintContext context,
        int depth,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        NoteLookup? notes
    )
    {
        // A node that gets wrapped opens a group, so its operands sit one group deeper than the node itself.
        string? note = notes?.Of(node);
        bool wrap = NeedsWrap(node, context, note);
        int innerDepth = wrap ? depth + 1 : depth;
        string inner = node switch
        {
            ConstantExpression c => constants.Text(c.Value),
            TermExpression t => t.Identity.ToString(),
            NotExpression n => NotToken(operatorStyle)
                + PrintNode(n.Operand, PrintContext.NotOperand, innerDepth, grouping, operatorStyle, constants, notes),
            AndExpression => JoinOperands(
                node,
                InfixToken("AND", operatorStyle),
                PrintContext.AndOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            ),
            OrExpression => JoinOperands(
                node,
                InfixToken("OR", operatorStyle),
                PrintContext.OrOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            ),
            XorExpression x => PrintNode(
                x.Left,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            )
                + InfixToken("XOR", operatorStyle)
                + PrintNode(x.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants, notes),
            EquivalentExpression xn => PrintNode(
                xn.Left,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            )
                + InfixToken("EQUIVALENT", operatorStyle)
                + PrintNode(xn.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants, notes),
            ImpliesExpression im => PrintNode(
                im.Antecedent,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            )
                + InfixToken("IMPLIES", operatorStyle)
                + PrintNode(im.Consequent, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants, notes),
            NandExpression nd => PrintNode(
                nd.Left,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            )
                + InfixToken("NAND", operatorStyle)
                + PrintNode(nd.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants, notes),
            NorExpression nr => PrintNode(
                nr.Left,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants,
                notes
            )
                + InfixToken("NOR", operatorStyle)
                + PrintNode(nr.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants, notes),
            ParityExpression =>
                $"PARITY({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            AnyExpression =>
                $"ANY({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            AllExpression =>
                $"ALL({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            NoneExpression =>
                $"NONE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            ExactlyOneExpression =>
                $"ExactlyOne({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            InspectionExpression ins =>
                $"{ins.Kind}({PrintNode(ins.Operand, PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            IfExpression =>
                $"If({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            CoalesceExpression =>
                $"COALESCE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            BetweenExpression bt =>
                $"BETWEEN({bt.Min}, {bt.Max}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            ThresholdExpression th =>
                $"{th.Comparison}({th.K}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            QuantifierExpression q =>
                $"{QuantifierName(q)}({q.Variable} IN {q.CollectionName}, {PrintNode(q.Body, PrintContext.Top, innerDepth, grouping, operatorStyle, constants, notes)})",
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        string comment = note is null ? string.Empty : NoteComment(note);
        if (!wrap)
        {
            return comment + inner;
        }

        (char open, char close) = Delimiters(depth, grouping);
        return $"{comment}{open}{inner}{close}";
    }

    /// <summary>
    /// Prints <paramref name="node"/> the way <see cref="PrintNode"/> would (the flat, one-line form), unless that
    /// flat form does not fit <paramref name="width"/> at the current <paramref name="indentLevel"/>, in which case the
    /// node breaks across lines (documented in <c>docs/rule-text.md</c>): an <c>AND</c>/<c>OR</c> chain or a
    /// binary connective breaks after each operator; a call form breaks one argument per line. A leaf
    /// (<see cref="ConstantExpression"/>, <see cref="TermExpression"/>) always prints flat. There is nothing in a
    /// single token to break, so a width smaller than the longest token never loops or throws; it only produces a
    /// line longer than the requested width.
    /// </summary>
    private static string PrintPretty(
        Expression node,
        PrintContext context,
        int depth,
        int indentLevel,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        int? width,
        int indentSize,
        NoteLookup? notes
    )
    {
        string? note = notes?.Of(node);
        bool wrap = NeedsWrap(node, context, note);
        int innerDepth = wrap ? depth + 1 : depth;
        int innerIndentLevel = wrap ? indentLevel + 1 : indentLevel;

        string flat = PrintNode(node, context, depth, grouping, operatorStyle, constants, notes);
        bool fits = width is null || (indentLevel * indentSize) + flat.Length <= width.Value;
        if (fits || node is ConstantExpression or TermExpression)
        {
            return flat;
        }

        // The note's comment goes in front of the broken form exactly as it goes in front of the flat one.
        string broken = node switch
        {
            NotExpression n => NotToken(operatorStyle)
                + PrintPretty(
                    n.Operand,
                    PrintContext.NotOperand,
                    innerDepth,
                    indentLevel,
                    grouping,
                    operatorStyle,
                    constants,
                    width,
                    indentSize,
                    notes
                ),
            AndExpression => BreakChain(
                node,
                "AND",
                PrintContext.AndOperand,
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            OrExpression => BreakChain(
                node,
                "OR",
                PrintContext.OrOperand,
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            XorExpression x => BreakBinary(
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                x.Left,
                "XOR",
                x.Right,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            EquivalentExpression xn => BreakBinary(
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                xn.Left,
                "EQUIVALENT",
                xn.Right,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            ImpliesExpression im => BreakBinary(
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                im.Antecedent,
                "IMPLIES",
                im.Consequent,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            NandExpression nd => BreakBinary(
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                nd.Left,
                "NAND",
                nd.Right,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            NorExpression nr => BreakBinary(
                wrap,
                depth,
                innerDepth,
                indentLevel,
                innerIndentLevel,
                grouping,
                nr.Left,
                "NOR",
                nr.Right,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            ParityExpression
            or AnyExpression
            or AllExpression
            or NoneExpression
            or ExactlyOneExpression
            or CoalesceExpression
            or IfExpression
            or InspectionExpression
            or ThresholdExpression
            or BetweenExpression => BreakCall(
                node,
                innerDepth,
                indentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            QuantifierExpression q => BreakQuantifier(
                q,
                innerDepth,
                indentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            ),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
        return note is null ? broken : NoteComment(note) + broken;
    }

    /// <summary>
    /// Breaks an <c>AND</c>/<c>OR</c> chain after each operator: the group's delimiters (when
    /// <paramref name="wrap"/>) each get their own line, and every operand is printed at
    /// <paramref name="innerIndentLevel"/>, recursively broken again if it still does not fit.
    /// </summary>
    private static string BreakChain(
        Expression node,
        string opWord,
        PrintContext operandContext,
        bool wrap,
        int depth,
        int innerDepth,
        int indentLevel,
        int innerIndentLevel,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        int? width,
        int indentSize,
        NoteLookup? notes
    )
    {
        IReadOnlyList<Expression> operands = ExpressionShape.Of(node).Operands;
        string token = OperatorToken(opWord, operatorStyle);

        StringBuilder builder = new();
        if (wrap)
        {
            builder.Append(Delimiters(depth, grouping).Open);
        }

        for (int i = 0; i < operands.Count; i++)
        {
            if (i > 0 || wrap)
            {
                builder.Append('\n').Append(Indent(innerIndentLevel, indentSize));
            }

            builder.Append(
                PrintPretty(
                    operands[i],
                    operandContext,
                    innerDepth,
                    innerIndentLevel,
                    grouping,
                    operatorStyle,
                    constants,
                    width,
                    indentSize,
                    notes
                )
            );
            if (i < operands.Count - 1)
            {
                builder.Append(' ').Append(token);
            }
        }

        if (wrap)
        {
            builder.Append('\n').Append(Indent(indentLevel, indentSize)).Append(Delimiters(depth, grouping).Close);
        }

        return builder.ToString();
    }

    /// <summary>Breaks a binary connective (always wrapped) after its operator, the same way <see cref="BreakChain"/> does for a chain.</summary>
    private static string BreakBinary(
        bool wrap,
        int depth,
        int innerDepth,
        int indentLevel,
        int innerIndentLevel,
        GroupingStyle grouping,
        Expression left,
        string opWord,
        Expression right,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        int? width,
        int indentSize,
        NoteLookup? notes
    )
    {
        string token = OperatorToken(opWord, operatorStyle);
        StringBuilder builder = new();
        if (wrap)
        {
            builder.Append(Delimiters(depth, grouping).Open);
        }

        builder.Append('\n').Append(Indent(innerIndentLevel, indentSize));
        builder.Append(
            PrintPretty(
                left,
                PrintContext.InfixOperand,
                innerDepth,
                innerIndentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            )
        );
        builder.Append(' ').Append(token);
        builder.Append('\n').Append(Indent(innerIndentLevel, indentSize));
        builder.Append(
            PrintPretty(
                right,
                PrintContext.InfixOperand,
                innerDepth,
                innerIndentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            )
        );

        if (wrap)
        {
            builder.Append('\n').Append(Indent(indentLevel, indentSize)).Append(Delimiters(depth, grouping).Close);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Breaks a call form (<c>AtLeast(2, a, b, c)</c>, <c>BETWEEN(min, max, ...)</c>, <c>ANY(...)</c> and the rest) one
    /// argument per line: any leading bound (<c>k</c>, or <c>min</c>/<c>max</c>) prints first, then each
    /// operand, recursively broken again if it still does not fit. Call parentheses are always literal <c>(</c>/<c>)</c>,
    /// never the grouping delimiters, matching <see cref="PrintNode"/>.
    /// </summary>
    private static string BreakCall(
        Expression node,
        int depth,
        int indentLevel,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        int? width,
        int indentSize,
        NoteLookup? notes
    )
    {
        NodeShape shape = ExpressionShape.Of(node);
        int innerIndentLevel = indentLevel + 1;

        List<string> leading = [];
        if (shape.K is int k)
        {
            leading.Add(k.ToString(CultureInfo.InvariantCulture));
        }

        if (shape.Max is int max)
        {
            leading.Add(max.ToString(CultureInfo.InvariantCulture));
        }

        int totalItems = leading.Count + shape.Operands.Count;
        int index = 0;

        StringBuilder builder = new();
        builder.Append(CallName(node)).Append('(');

        foreach (string lead in leading)
        {
            builder.Append('\n').Append(Indent(innerIndentLevel, indentSize)).Append(lead);
            index++;
            if (index < totalItems)
            {
                builder.Append(',');
            }
        }

        foreach (Expression operand in shape.Operands)
        {
            builder.Append('\n').Append(Indent(innerIndentLevel, indentSize));
            builder.Append(
                PrintPretty(
                    operand,
                    PrintContext.Top,
                    depth,
                    innerIndentLevel,
                    grouping,
                    operatorStyle,
                    constants,
                    width,
                    indentSize,
                    notes
                )
            );
            index++;
            if (index < totalItems)
            {
                builder.Append(',');
            }
        }

        builder.Append('\n').Append(Indent(indentLevel, indentSize)).Append(')');
        return builder.ToString();
    }

    /// <summary>
    /// Breaks a quantifier (<c>FORALL(line IN lines, body)</c>) one item per line: the head
    /// (<c>line IN lines</c>) prints first, then the body, recursively broken again if it still does not fit.
    /// </summary>
    private static string BreakQuantifier(
        QuantifierExpression node,
        int depth,
        int indentLevel,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        int? width,
        int indentSize,
        NoteLookup? notes
    )
    {
        int innerIndentLevel = indentLevel + 1;
        StringBuilder builder = new();
        builder.Append(QuantifierName(node)).Append('(');
        builder
            .Append('\n')
            .Append(Indent(innerIndentLevel, indentSize))
            .Append(node.Variable)
            .Append(" IN ")
            .Append(node.CollectionName)
            .Append(',');
        builder.Append('\n').Append(Indent(innerIndentLevel, indentSize));
        builder.Append(
            PrintPretty(
                node.Body,
                PrintContext.Top,
                depth,
                innerIndentLevel,
                grouping,
                operatorStyle,
                constants,
                width,
                indentSize,
                notes
            )
        );
        builder.Append('\n').Append(Indent(indentLevel, indentSize)).Append(')');
        return builder.ToString();
    }

    /// <summary>Gets the canonical DSL word for a quantifier: <c>FORALL</c> or <c>EXISTS</c> (ADR-0010 decision 2).</summary>
    private static string QuantifierName(QuantifierExpression node)
    {
        return QuantifierText.Label(node.Kind);
    }

    /// <summary>Gets the call keyword for a call-form node, exactly as <see cref="PrintNode"/> writes it.</summary>
    private static string CallName(Expression node)
    {
        return node switch
        {
            ParityExpression => "PARITY",
            AnyExpression => "ANY",
            AllExpression => "ALL",
            NoneExpression => "NONE",
            ExactlyOneExpression => "ExactlyOne",
            CoalesceExpression => "COALESCE",
            IfExpression => "If",
            InspectionExpression ins => ins.Kind.ToString(),
            ThresholdExpression th => th.Comparison.ToString(),
            BetweenExpression => "BETWEEN",
            _ => throw new ArgumentException($"'{node.GetType()}' is not a call-form node.", nameof(node)),
        };
    }

    /// <summary>Gets the bare (unpadded) token for a binary connective, in the given <paramref name="style"/>, <see cref="InfixToken"/> without its surrounding spaces.</summary>
    private static string OperatorToken(string word, OperatorStyle style)
    {
        return InfixToken(word, style).Trim();
    }

    /// <summary>Gets <paramref name="level"/> <c>* indentSize</c> spaces, clamped to zero so a pathological (negative) input never throws.</summary>
    private static string Indent(int level, int indentSize)
    {
        return new string(' ', Math.Max(0, level * indentSize));
    }

    /// <summary>Gets the prefix token for <c>NOT</c>: <c>"NOT "</c>, or <c>"¬"</c> with no trailing space in <see cref="OperatorStyle.Symbolic"/>.</summary>
    private static string NotToken(OperatorStyle style)
    {
        return style switch
        {
            OperatorStyle.Word => "NOT ",
            OperatorStyle.Symbolic => "¬",
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, "PrintRuleText supports only Word and Symbolic."),
        };
    }

    /// <summary>
    /// Gets the infix token for a binary connective, padded with one space on each side, in the given
    /// <paramref name="style"/>. <see cref="OperatorStyle.Symbolic"/> uses the exact glyph the DSL lexer reads for
    /// <paramref name="word"/> (<c>DslParser.SymbolAlias</c>), so the printed text re-parses to an equal tree.
    /// </summary>
    private static string InfixToken(string word, OperatorStyle style)
    {
        return style switch
        {
            OperatorStyle.Word => $" {word} ",
            OperatorStyle.Symbolic => $" {SymbolFor(word)} ",
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, "PrintRuleText supports only Word and Symbolic."),
        };
    }

    /// <summary>Maps a connective's canonical word to the symbol <c>DslParser.SymbolAlias</c> reads back to it.</summary>
    private static string SymbolFor(string word)
    {
        return
            OperatorDefinitions.TryGetByLabel(word, out OperatorDefinition? definition)
            && definition.SymbolicSpelling is { } symbol
            ? symbol
            : throw new ArgumentOutOfRangeException(nameof(word), word, "No symbolic spelling for this connective.");
    }

    /// <summary>
    /// Picks the delimiter pair for a group at <paramref name="depth"/>: always parentheses, or cycling <c>(</c> <c>[</c>
    /// <c>{</c> by depth so each nesting level looks different. Depth, not position or content, decides, so the output is
    /// deterministic and independent of what the operands mean.
    /// </summary>
    private static (char Open, char Close) Delimiters(int depth, GroupingStyle grouping)
    {
        if (grouping == GroupingStyle.Parentheses)
        {
            return ('(', ')');
        }

        return (depth % 3) switch
        {
            0 => ('(', ')'),
            1 => ('[', ']'),
            _ => ('{', '}'),
        };
    }

    private static string JoinOperands(
        Expression node,
        string separator,
        PrintContext operandContext,
        int depth,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants,
        NoteLookup? notes
    )
    {
        return string.Join(
            separator,
            ExpressionShape
                .Of(node)
                .Operands.Select(o => PrintNode(o, operandContext, depth, grouping, operatorStyle, constants, notes))
        );
    }
}
