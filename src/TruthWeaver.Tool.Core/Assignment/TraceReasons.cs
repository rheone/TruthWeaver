namespace TruthWeaver.Tool.Core.Assignment;

/// <summary>Writes the plain reason line of a trace node, with the K3 rule that gave the value.</summary>
internal static class TraceReasons
{
    /// <summary>The reason for a node that short-circuiting skipped.</summary>
    public const string Skipped = "Not evaluated: an earlier operand already settled the result.";

    /// <summary>Gives the reason for an operator node from the values of its operands.</summary>
    /// <param name="name">The operator name from the trace, such as <c>AND</c>.</param>
    /// <param name="value">The value of the node.</param>
    /// <param name="operands">The values of the operands; <see langword="null"/> for an operand that short-circuiting skipped.</param>
    /// <returns>The reason.</returns>
    public static string ForOperator(string name, TruthValue value, IReadOnlyList<TruthValue?> operands)
    {
        int unknown = operands.Count(v => v == TruthValue.Unknown);
        if (name.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            return value switch
            {
                TruthValue.False => unknown > 0
                    ? "AND is False: one operand is False, and False wins over an Unknown operand."
                    : "AND is False: one operand is False.",
                TruthValue.Unknown =>
                    $"AND is Unknown: no operand is False, but {Count(unknown)} Unknown, so the answer is not settled.",
                _ => "AND is True: every operand is True.",
            };
        }

        if (name.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            return value switch
            {
                TruthValue.True => unknown > 0
                    ? "OR is True: one operand is True, and True wins over an Unknown operand."
                    : "OR is True: one operand is True.",
                TruthValue.Unknown =>
                    $"OR is Unknown: no operand is True, but {Count(unknown)} Unknown, so the answer is not settled.",
                _ => "OR is False: every operand is False.",
            };
        }

        if (name.Equals("NOT", StringComparison.OrdinalIgnoreCase))
        {
            return value switch
            {
                TruthValue.Unknown => "NOT is Unknown: NOT of Unknown stays Unknown.",
                TruthValue.True => "NOT is True: the operand is False.",
                _ => "NOT is False: the operand is True.",
            };
        }

        string values = string.Join(", ", operands.Select(v => v?.ToString() ?? "skipped"));
        return $"{name} is {value}, from the operand values ({values}).";
    }

    /// <summary>Gives the reason for a term or constant node.</summary>
    /// <param name="value">The value of the node.</param>
    /// <param name="fault">The message of the fault of the term, or <see langword="null"/> when it had none.</param>
    /// <returns>The reason.</returns>
    public static string ForLeaf(TruthValue value, string? fault)
    {
        if (fault is not null)
        {
            return $"Unknown: the term could not be evaluated ({fault})";
        }

        return value == TruthValue.Unknown
            ? "Unknown: the term has no value. Press Space on it to give it True or False."
            : $"{value}: the value that the assignment gives the term.";
    }

    private static string Count(int unknown)
    {
        return unknown == 1 ? "one operand is" : $"{unknown} operands are";
    }
}
