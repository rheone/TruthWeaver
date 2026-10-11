namespace TruthWeaver.Printing;

using System.Text;

/// <summary>
/// Escapes literal text (predicate names and argument values) for the LaTeX equation dialect. The escaping is a unit of
/// its own, as <c>MermaidTreePrinter.Escape</c> is for Mermaid, because a wrong escape breaks the typeset equation.
/// </summary>
internal static class LatexText
{
    /// <summary>Writes literal text as LaTeX math that shows the text unchanged.</summary>
    /// <param name="value">The literal text.</param>
    /// <param name="mathJaxSafe">
    /// <see langword="true"/> to use only MathJax commands. Each special character leaves the <c>\text</c> group and
    /// becomes a math command, because MathJax does not support text-mode commands such as <c>\textbackslash</c>.
    /// </param>
    /// <returns>The math text: one <c>\text{...}</c> group, or a run of groups and commands.</returns>
    public static string Text(string value, bool mathJaxSafe)
    {
        StringBuilder result = new();
        StringBuilder run = new();

        foreach (char c in value)
        {
            string? escaped = mathJaxSafe ? MathCommand(c) : TextCommand(c);
            if (escaped is null)
            {
                run.Append(c);
            }
            else if (mathJaxSafe)
            {
                FlushRun(result, run);
                result.Append(escaped);
            }
            else
            {
                run.Append(escaped);
            }
        }

        // Raw mode is always one group. MathJax-safe mode drops an empty group unless the whole text is empty.
        if (!mathJaxSafe || run.Length > 0 || result.Length == 0)
        {
            result.Append(@"\text{").Append(run).Append('}');
        }

        return result.ToString();
    }

    private static void FlushRun(StringBuilder result, StringBuilder run)
    {
        if (run.Length > 0)
        {
            result.Append(@"\text{").Append(run).Append('}');
            run.Clear();
        }
    }

    /// <summary>The escape for a special character inside a text-mode group, in full LaTeX.</summary>
    private static string? TextCommand(char c)
    {
        return c switch
        {
            '_' => @"\_",
            '%' => @"\%",
            '&' => @"\&",
            '#' => @"\#",
            '{' => @"\{",
            '}' => @"\}",
            '$' => @"\$",
            '\\' => @"\textbackslash{}",
            '^' => @"\textasciicircum{}",
            '~' => @"\textasciitilde{}",
            _ => null,
        };
    }

    /// <summary>The math-mode command for a special character, from the MathJax subset that GitHub renders.</summary>
    private static string? MathCommand(char c)
    {
        return c switch
        {
            '_' or '%' or '&' or '#' or '{' or '}' or '$' => $@"\{c}",
            '\\' => @"\backslash",
            '^' => @"\hat{\ }",
            '~' => @"\sim",
            '`' => @"\unicode{x60}",
            _ => null,
        };
    }
}
