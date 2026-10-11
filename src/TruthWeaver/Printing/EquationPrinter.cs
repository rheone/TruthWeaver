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
    /// <summary>
    /// The letters of simple-variable mode, in order. After the last letter they repeat with a numeric subscript:
    /// <c>p</c> to <c>z</c>, then <c>p₁</c> to <c>z₁</c>, then <c>p₂</c>. The letters stay clear of <c>a</c> to <c>d</c>,
    /// which rule authors often use as short predicate names.
    /// </summary>
    private const string VariableLetters = "pqrstuvwxyz";

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
        if (options.SimpleVariables)
        {
            return PrintWithLegend(root, options).Equation;
        }

        EquationTokens tokens = EquationTokens.For(options);
        return tokens.Wrap(
            PrintNode(
                root,
                PrintContext.Top,
                tokens,
                identity => tokens.FormatTerm(identity, options.ShowArgumentValues),
                options.Constants
            )
        );
    }

    /// <summary>
    /// Prints an expression tree with a letter for each term, and builds the legend. The letters follow the first
    /// occurrence of each term in a depth-first, left-to-right walk, so the same tree always gets the same letters.
    /// </summary>
    /// <param name="root">The tree to print.</param>
    /// <param name="options">The dialect options. The equation uses letters whatever <see cref="EquationOptions.SimpleVariables"/> says.</param>
    /// <returns>The equation and its legend.</returns>
    public static EquationWithLegend PrintWithLegend(Expression root, EquationOptions options)
    {
        EquationTokens tokens = EquationTokens.For(options);

        // The walk order of the operands equals the print order, so a term is lettered where it first prints.
        List<TermIdentity> terms = [];
        CollectTerms(root, terms);

        Dictionary<TermIdentity, string> variables = [];
        List<EquationLegendEntry> legend = [];
        for (int index = 0; index < terms.Count; index++)
        {
            string variable = tokens.FormatVariable(
                VariableLetters[index % VariableLetters.Length],
                index / VariableLetters.Length
            );
            variables[terms[index]] = variable;
            legend.Add(new EquationLegendEntry(variable, terms[index], tokens.FormatTerm(terms[index], true)));
        }

        string equation = tokens.Wrap(
            PrintNode(root, PrintContext.Top, tokens, identity => variables[identity], options.Constants)
        );
        string legendText = string.Join("\n", legend.Select(e => tokens.Wrap($"{e.Variable} = {e.TermText}")));
        return new EquationWithLegend(equation, legend, legendText);
    }

    /// <summary>Adds each distinct term to the list in first-occurrence order, depth first and left to right.</summary>
    private static void CollectTerms(Expression node, List<TermIdentity> terms)
    {
        // Terms and constants are leaves: ExpressionShape has no operands for them.
        if (node is TermExpression term)
        {
            if (!terms.Contains(term.Identity))
            {
                terms.Add(term.Identity);
            }

            return;
        }

        if (node is ConstantExpression)
        {
            return;
        }

        foreach (Expression operand in ExpressionShape.Of(node).Operands)
        {
            CollectTerms(operand, terms);
        }
    }

    private static string PrintNode(
        Expression node,
        PrintContext context,
        EquationTokens tokens,
        Func<TermIdentity, string> formatTerm,
        ConstantFormat constants
    )
    {
        string inner = node switch
        {
            ConstantExpression c => tokens.FormatConstant(c.Value, constants),
            TermExpression t => formatTerm(t.Identity),
            NotExpression n => tokens.Not + PrintNode(n.Operand, PrintContext.Operand, tokens, formatTerm, constants),
            AndExpression => JoinInfix(node, tokens.And, tokens, formatTerm, constants),
            OrExpression => JoinInfix(node, tokens.Or, tokens, formatTerm, constants),
            XorExpression => JoinInfix(node, tokens.Xor, tokens, formatTerm, constants),
            EquivalentExpression => JoinInfix(node, tokens.Equivalent, tokens, formatTerm, constants),
            ImpliesExpression => JoinInfix(node, tokens.Implies, tokens, formatTerm, constants),
            NandExpression => JoinInfix(node, tokens.Nand, tokens, formatTerm, constants),
            NorExpression => JoinInfix(node, tokens.Nor, tokens, formatTerm, constants),
            ParityExpression => FunctionCall("PARITY", [], node, tokens, formatTerm, constants),
            AnyExpression => FunctionCall("ANY", [], node, tokens, formatTerm, constants),
            AllExpression => FunctionCall("ALL", [], node, tokens, formatTerm, constants),
            NoneExpression => FunctionCall("NONE", [], node, tokens, formatTerm, constants),
            ExactlyOneExpression => FunctionCall("ExactlyOne", [], node, tokens, formatTerm, constants),
            InspectionExpression ins => FunctionCall(ins.Kind.ToString(), [], node, tokens, formatTerm, constants),
            IfExpression iff when tokens.FormatIf is { } formatIf => formatIf(
                PrintNode(iff.Condition, PrintContext.Operand, tokens, formatTerm, constants),
                PrintNode(iff.WhenTrue, PrintContext.Operand, tokens, formatTerm, constants),
                PrintNode(iff.WhenFalse, PrintContext.Operand, tokens, formatTerm, constants)
            ),
            IfExpression => FunctionCall("If", [], node, tokens, formatTerm, constants),
            CoalesceExpression => FunctionCall("COALESCE", [], node, tokens, formatTerm, constants),
            BetweenExpression bt => FunctionCall("BETWEEN", [bt.Min, bt.Max], node, tokens, formatTerm, constants),
            ThresholdExpression th => FunctionCall(th.Comparison.ToString(), [th.K], node, tokens, formatTerm, constants),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        return NeedsParentheses(node, context, tokens) ? $"({inner})" : inner;
    }

    /// <summary>
    /// An infix connective needs parentheses only as an operand of another connective. <c>NOT</c> binds tightest
    /// and never needs them. A function call delimits its own arguments, so it never needs them either. <c>If</c>
    /// needs them too, but only when <see cref="EquationTokens.FormatIf"/> turns it into a ternary — the function-call
    /// form delimits itself like any other call.
    /// </summary>
    private static bool NeedsParentheses(Expression node, PrintContext context, EquationTokens tokens)
    {
        if (context != PrintContext.Operand)
        {
            return false;
        }

        return node
                is AndExpression
                    or OrExpression
                    or XorExpression
                    or EquivalentExpression
                    or ImpliesExpression
                    or NandExpression
                    or NorExpression
            || (node is IfExpression && tokens.FormatIf is not null);
    }

    private static string JoinInfix(
        Expression node,
        string token,
        EquationTokens tokens,
        Func<TermIdentity, string> formatTerm,
        ConstantFormat constants
    )
    {
        return string.Join(
            $" {token} ",
            ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, PrintContext.Operand, tokens, formatTerm, constants))
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
        Func<TermIdentity, string> formatTerm,
        ConstantFormat constants
    )
    {
        IEnumerable<string> arguments = counts
            .Select(count => count.ToString(CultureInfo.InvariantCulture))
            .Concat(
                ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, PrintContext.Top, tokens, formatTerm, constants))
            );
        return $"{tokens.FormatFunctionName(name)}({string.Join(", ", arguments)})";
    }
}
