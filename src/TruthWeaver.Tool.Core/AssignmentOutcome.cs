namespace TruthWeaver.Tool.Core;

using TruthWeaver.Diagnostics;

/// <summary>
/// The result of evaluating a rule under an <see cref="AssignmentSession"/>: the terms and their values, the decision, the
/// trace with a reason for each node, the faults and the result of the <c>Collapse</c> and <c>Project</c> policies.
/// </summary>
/// <param name="HasRule">Whether a rule was evaluated. It is <see langword="false"/> while no text of the document compiled.</param>
/// <param name="IsRuleCurrent">Whether the evaluated rule is the rule of the current text; otherwise it is the last tree that compiled.</param>
/// <param name="Terms">The distinct terms of the rule, in the order of their first use.</param>
/// <param name="Decision">The decision, or <see langword="null"/> when no rule was evaluated.</param>
/// <param name="Trace">The trace tree, or <see langword="null"/> when no rule was evaluated.</param>
/// <param name="Reason">One plain sentence about the result of the rule as a whole.</param>
/// <param name="Faults">One line for each fault: the term and the cause.</param>
/// <param name="Policy">The policy that <paramref name="Collapsed"/> and <paramref name="Projected"/> used.</param>
/// <param name="Collapsed">The decision collapsed under <paramref name="Policy"/>, or <see langword="null"/> when no rule was evaluated.</param>
/// <param name="Projected">
/// The result with <c>Unknown</c> replaced by the value that <paramref name="Policy"/> names. It is <see langword="null"/>
/// when no rule was evaluated and for <see cref="CollapsePolicy.UnknownIsError"/>, which names no value.
/// </param>
/// <param name="VariableDiagnostics">The problems of the variables text. While it has an error, the last data that was well-formed applies.</param>
public sealed record AssignmentOutcome(
    bool HasRule,
    bool IsRuleCurrent,
    IReadOnlyList<TermAssignment> Terms,
    Decision? Decision,
    TraceLine? Trace,
    string Reason,
    IReadOnlyList<string> Faults,
    CollapsePolicy Policy,
    CollapseOutcome? Collapsed,
    TruthValue? Projected,
    IReadOnlyList<Diagnostic> VariableDiagnostics
)
{
    /// <summary>Gets a value indicating whether the decision is satisfied: only a definite <see cref="TruthValue.True"/> is (fail-closed).</summary>
    public bool IsSatisfied => this.Decision?.IsSatisfied ?? false;

    /// <summary>
    /// Gets the stub evaluation behind <see cref="Decision"/>: the rule compiled over the stand-in predicates, the decision
    /// and the options. A view that marks the decision on the rule (a tree or a diagram) prints it from this rule, so the
    /// nodes and the trace agree. It is <see langword="null"/> when no rule was evaluated.
    /// </summary>
    public StubEvaluation? Evaluation { get; init; }
}
