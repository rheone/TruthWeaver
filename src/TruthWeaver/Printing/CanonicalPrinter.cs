namespace TruthWeaver.Printing;

using TruthWeaver.Ast;

/// <summary>
/// Renders a compiled expression tree back to canonical DSL text (ADR-0003) — deterministic, and
/// parenthesized for clarity wherever an operator is mixed with a different one, even where
/// precedence alone would make the meaning unambiguous (e.g. <c>a AND b OR c</c> prints as
/// <c>(a AND b) OR c</c>) — the point is to make a large nested rule easy for a human to read at a
/// glance, not merely to avoid a parser error. <c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c> are always parenthesized
/// regardless of context. This is the exact form <c>parse</c> reproduces a structurally equal tree
/// from (ticket 06).
/// </summary>
/// <remarks>
/// <see cref="OperatorStyle.Symbolic"/> (ticket 06) swaps <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c>
/// for the symbols the DSL lexer itself reads for them (<c>DslParser.SymbolAlias</c>), so
/// the printed text still re-parses to an equal tree. <c>COALESCE</c> and <c>If</c> are unaffected: the DSL has no call
/// syntax for <c>??</c>, so they always print in word/function-call form. <see cref="OperatorStyle.CStyle"/> is not
/// supported here — the caller (<c>CompiledRule.PrintRuleText</c>) rejects it before this printer ever sees it, since
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
    /// <returns>The DSL text.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</exception>
    public static string Print(
        Expression root,
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null
    )
    {
        return PrintNode(root, PrintContext.Top, 0, grouping, operatorStyle, constants ?? ConstantFormat.Default);
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

    private static string PrintNode(
        Expression node,
        PrintContext context,
        int depth,
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat constants
    )
    {
        // A node that gets wrapped opens a group, so its operands sit one group deeper than the node itself.
        bool wrap = NeedsWrap(node, context);
        int innerDepth = wrap ? depth + 1 : depth;
        string inner = node switch
        {
            ConstantExpression c => constants.Text(c.Value),
            TermExpression t => t.Identity.ToString(),
            NotExpression n => NotToken(operatorStyle)
                + PrintNode(n.Operand, PrintContext.NotOperand, innerDepth, grouping, operatorStyle, constants),
            AndExpression => JoinOperands(
                node,
                InfixToken("AND", operatorStyle),
                PrintContext.AndOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants
            ),
            OrExpression => JoinOperands(
                node,
                InfixToken("OR", operatorStyle),
                PrintContext.OrOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants
            ),
            XorExpression x => PrintNode(x.Left, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants)
                + InfixToken("XOR", operatorStyle)
                + PrintNode(x.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants),
            EquivalentExpression xn => PrintNode(
                xn.Left,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants
            )
                + InfixToken("EQUIVALENT", operatorStyle)
                + PrintNode(xn.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants),
            ImpliesExpression im => PrintNode(
                im.Antecedent,
                PrintContext.InfixOperand,
                innerDepth,
                grouping,
                operatorStyle,
                constants
            )
                + InfixToken("IMPLIES", operatorStyle)
                + PrintNode(im.Consequent, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants),
            NandExpression nd => PrintNode(nd.Left, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants)
                + InfixToken("NAND", operatorStyle)
                + PrintNode(nd.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants),
            NorExpression nr => PrintNode(nr.Left, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants)
                + InfixToken("NOR", operatorStyle)
                + PrintNode(nr.Right, PrintContext.InfixOperand, innerDepth, grouping, operatorStyle, constants),
            ParityExpression =>
                $"PARITY({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            AnyExpression =>
                $"ANY({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            AllExpression =>
                $"ALL({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            NoneExpression =>
                $"NONE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            ExactlyOneExpression =>
                $"ExactlyOne({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            InspectionExpression ins =>
                $"{ins.Kind}({PrintNode(ins.Operand, PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            IfExpression => $"If({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            CoalesceExpression =>
                $"COALESCE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            BetweenExpression bt =>
                $"BETWEEN({bt.Min}, {bt.Max}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            ThresholdExpression th =>
                $"{th.Comparison}({th.K}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping, operatorStyle, constants)})",
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        if (!wrap)
        {
            return inner;
        }

        (char open, char close) = Delimiters(depth, grouping);
        return $"{open}{inner}{close}";
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
        return word switch
        {
            "AND" => "∧",
            "OR" => "∨",
            "XOR" => "⊕",
            "EQUIVALENT" => "↔",
            "IMPLIES" => "→",
            "NAND" => "↑",
            "NOR" => "↓",
            _ => throw new ArgumentOutOfRangeException(nameof(word), word, "No symbolic spelling for this connective."),
        };
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
        ConstantFormat constants
    )
    {
        return string.Join(
            separator,
            ExpressionShape
                .Of(node)
                .Operands.Select(o => PrintNode(o, operandContext, depth, grouping, operatorStyle, constants))
        );
    }
}
