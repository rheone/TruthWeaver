namespace TruthWeaver.Tool.Tui;

using TruthWeaver.Tool.Core;

/// <summary>The text of the assignment pane: the decision lines, a line for each term and a line for each trace node. It has no view.</summary>
internal static class AssignmentText
{
    /// <summary>The text of the decision lines while the rule has not compiled yet.</summary>
    public const string NoRule = "No rule yet: the rule text has errors.";

    /// <summary>The first line of the pane: the result, the fail-closed answer and the number of faults.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The line.</returns>
    public static string ResultLine(AssignmentOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (outcome.Decision is not { } decision)
        {
            return NoRule;
        }

        string stale = outcome.IsRuleCurrent ? string.Empty : " (last valid tree)";
        return $"Result: {decision.Result} | IsSatisfied: {(outcome.IsSatisfied ? "True" : "False")} | Faults: {decision.Faults.Count}{stale}";
    }

    /// <summary>The second line of the pane: the policy and what <c>Collapse</c> and <c>Project</c> give under it.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The line.</returns>
    public static string PolicyLine(AssignmentOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        string projected = outcome.Projected is { } value ? value.ToString() : "n/a";
        string collapsed = outcome.Collapsed is { } result ? result.ToString() : "n/a";
        return $"Policy (Ctrl+F3): {Workbench.PolicyText(outcome.Policy)} | Collapse: {collapsed} | Project: {projected}";
    }

    /// <summary>The line of a term in the list: a letter for the value, then the term.</summary>
    /// <param name="term">The term.</param>
    /// <returns>The line.</returns>
    public static string TermLine(TermAssignment term)
    {
        ArgumentNullException.ThrowIfNull(term);

        if (term.Key is null)
        {
            return $"[?] {term.Display}  (no data yet)";
        }

        return $"[{Letter(term.Value)}] {term.Display}";
    }

    /// <summary>The line of a trace node: the value, then the node.</summary>
    /// <param name="line">The node.</param>
    /// <returns>The line.</returns>
    public static string TraceLineText(TraceLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return $"{(line.Value is { } value ? value.ToString() : "skipped")}  {line.Text}";
    }

    /// <summary>The lines of the Variables pane under the editor: the problems of the text, or a hint.</summary>
    /// <param name="session">The session.</param>
    /// <returns>The text.</returns>
    public static string VariablesNote(AssignmentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return session.VariableDiagnostics.Count == 0
            ? "A JSON or YAML object: each source name maps to its data, such as {\"user\": {\"region\": \"EU\"}}. Ctrl+F4 loads a file."
            : string.Join(" | ", session.VariableDiagnostics.Select(d => d.Message)) + " (the last good data applies)";
    }

    private static char Letter(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => 'T',
            TruthValue.False => 'F',
            _ => 'U',
        };
    }
}
