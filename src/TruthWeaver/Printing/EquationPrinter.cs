namespace TruthWeaver.Printing;

using System.Globalization;
using TruthWeaver.Ast;

/// <summary>
/// Renders a compiled expression tree as a flat, single-line infix equation. The walk has the same shape as
/// <see cref="CanonicalPrinter"/>: an infix join with context-aware parentheses. An <see cref="EquationTokens"/> table
/// supplies every token, so the walk does not depend on the dialect.
/// </summary>
/// <remarks>
/// The equation uses fewer parentheses than the canonical DSL text. The canonical text always groups the binary
/// connectives, even at the root. An equation follows the mathematical habit of no outer pair at the root. A
/// connective nested in another connective is still grouped, so the reader never needs a precedence table.
/// </remarks>
internal static class EquationPrinter
{
    /// <summary>The position of a node relative to its parent, which decides whether the node needs parentheses.</summary>
    private enum PrintContext
    {
        /// <summary>The root of the equation, or an argument of a function call, which the call already delimits.</summary>
        Top,

        /// <summary>An operand of an infix or prefix connective.</summary>
        Operand,
    }

    /// <summary>Prints an expression tree as an equation.</summary>
    /// <param name="root">The tree to print.</param>
    /// <param name="options">The dialect and term options.</param>
    /// <returns>The equation text.</returns>
    public static string Print(Expression root, EquationOptions options)
    {
        EquationTokens tokens = EquationTokens.For(options.Dialect);
        return tokens.Wrap(PrintNode(root, PrintContext.Top, tokens, options.ShowArgumentValues));
    }

    private static string PrintNode(Expression node, PrintContext context, EquationTokens tokens, bool showArgumentValues)
    {
        string inner = node switch
        {
            ConstantExpression c => tokens.FormatConstant(c.Value),
            TermExpression t => tokens.FormatTerm(t.Identity, showArgumentValues),
            NotExpression n => tokens.Not + PrintNode(n.Operand, PrintContext.Operand, tokens, showArgumentValues),
            AndExpression => JoinInfix(node, tokens.And, tokens, showArgumentValues),
            OrExpression => JoinInfix(node, tokens.Or, tokens, showArgumentValues),
            XorExpression => JoinInfix(node, tokens.Xor, tokens, showArgumentValues),
            EquivalentExpression => JoinInfix(node, tokens.Equivalent, tokens, showArgumentValues),
            ImpliesExpression => JoinInfix(node, tokens.Implies, tokens, showArgumentValues),
            NandExpression => JoinInfix(node, tokens.Nand, tokens, showArgumentValues),
            NorExpression => JoinInfix(node, tokens.Nor, tokens, showArgumentValues),
            ParityExpression => FunctionCall("PARITY", [], node, tokens, showArgumentValues),
            AnyExpression => FunctionCall("ANY", [], node, tokens, showArgumentValues),
            AllExpression => FunctionCall("ALL", [], node, tokens, showArgumentValues),
            NoneExpression => FunctionCall("NONE", [], node, tokens, showArgumentValues),
            ExactlyOneExpression => FunctionCall("ExactlyOne", [], node, tokens, showArgumentValues),
            InspectionExpression ins => FunctionCall(ins.Kind.ToString(), [], node, tokens, showArgumentValues),
            IfExpression => FunctionCall("If", [], node, tokens, showArgumentValues),
            CoalesceExpression => FunctionCall("COALESCE", [], node, tokens, showArgumentValues),
            BetweenExpression bt => FunctionCall("BETWEEN", [bt.Min, bt.Max], node, tokens, showArgumentValues),
            ThresholdExpression th => FunctionCall(th.Comparison.ToString(), [th.K], node, tokens, showArgumentValues),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        return NeedsParentheses(node, context) ? $"({inner})" : inner;
    }

    /// <summary>
    /// An infix connective needs parentheses only as an operand of another connective. <c>NOT</c> binds tightest
    /// and never needs them. A function call delimits its own arguments, so it never needs them either.
    /// </summary>
    private static bool NeedsParentheses(Expression node, PrintContext context)
    {
        return context == PrintContext.Operand
            && node
                is AndExpression
                    or OrExpression
                    or XorExpression
                    or EquivalentExpression
                    or ImpliesExpression
                    or NandExpression
                    or NorExpression;
    }

    private static string JoinInfix(Expression node, string token, EquationTokens tokens, bool showArgumentValues)
    {
        return string.Join(
            $" {token} ",
            ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, PrintContext.Operand, tokens, showArgumentValues))
        );
    }

    /// <summary>
    /// Prints an operator with no infix symbol as <c>Name(counts..., operands...)</c>, with the same spelling and
    /// argument order as the DSL, for example <c>AtLeast(2, a, b, c)</c>.
    /// </summary>
    private static string FunctionCall(
        string name,
        IReadOnlyList<int> counts,
        Expression node,
        EquationTokens tokens,
        bool showArgumentValues
    )
    {
        IEnumerable<string> arguments = counts
            .Select(count => count.ToString(CultureInfo.InvariantCulture))
            .Concat(ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, PrintContext.Top, tokens, showArgumentValues)));
        return $"{tokens.FormatFunctionName(name)}({string.Join(", ", arguments)})";
    }
}
