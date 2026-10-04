namespace TruthWeaver.Abstractions;

using System.Diagnostics;

/// <summary>
/// The result of evaluating a rule against a context: a <see cref="TruthValue"/> plus any faults
/// recorded along the way, and optionally a trace (ADR-0001).
/// </summary>
/// <param name="Result">
/// The rule's three-valued result, always the raw value the expression produced. A rule cannot declare a collapse
/// (ADR-0005 decision 14); to resolve <see cref="TruthValue.Unknown"/> choose a policy at the call site with
/// <see cref="Collapse(CollapsePolicy)"/>.
/// </param>
/// <param name="Faults">Every fault absorbed during this evaluation, in the order they occurred.</param>
/// <param name="Trace">
/// The evaluation trace, present only when requested via <c>EvaluationOptions</c>.
/// </param>
/// <param name="TraceTree">
/// A structural mirror of the compiled expression tree from this evaluation, with every node
/// (leaf or interior) annotated by what happened to it — its resolved result, or that it was skipped
/// by short-circuiting. Unlike <paramref name="Trace"/>'s flat log, this preserves the tree shape, so
/// it can drive a full-tree rendering (e.g. <c>MermaidTreePrinter</c>/<c>PlainTextTreePrinter</c>)
/// that shows the whole rule, the path actually taken, and the parts left out.
/// </param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record Decision(TruthValue Result, IReadOnlyList<Fault> Faults, Trace? Trace = null, TraceNode? TraceTree = null)
{
    /// <summary>
    /// Gets a value indicating whether this decision is satisfied. <see langword="true"/> only when
    /// <see cref="Result"/> is <see cref="TruthValue.True"/> — <see cref="TruthValue.Unknown"/> fails
    /// closed, the correct default for an authorization consumer (ADR-0001).
    /// </summary>
    public bool IsSatisfied => this.Result == TruthValue.True;

    // Shows the answer and the fault count; the default record ToString expands the whole trace and every fault.
    private string DebuggerDisplay => $"{this.Result} ({this.Faults.Count} faults)";

    /// <summary>
    /// Collapses <see cref="Result"/> to a final answer under <paramref name="policy"/>. <see cref="TruthValue.True"/> and
    /// <see cref="TruthValue.False"/> always map to the matching <see cref="CollapseOutcome"/>; only
    /// <see cref="TruthValue.Unknown"/> depends on the policy, but an undefined policy is rejected for every result. Pure: it does not change this decision, its
    /// <see cref="Faults"/> or <see cref="IsSatisfied"/> (which stays fail-closed regardless of any policy applied here).
    /// </summary>
    /// <param name="policy">How an <see cref="TruthValue.Unknown"/> result is resolved.</param>
    /// <returns>The collapsed outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy"/> is not a defined <see cref="CollapsePolicy"/>.</exception>
    public CollapseOutcome Collapse(CollapsePolicy policy)
    {
        // Checked up front so a bad policy is rejected for every result, not only the Unknown one that consults it.
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unhandled collapse policy.");
        }

        return this.Result switch
        {
            TruthValue.True => CollapseOutcome.True,
            TruthValue.False => CollapseOutcome.False,
            _ => policy switch
            {
                CollapsePolicy.UnknownAsFalse => CollapseOutcome.False,
                CollapsePolicy.UnknownAsTrue => CollapseOutcome.True,
                _ => CollapseOutcome.RejectedUnresolved,
            },
        };
    }

    /// <summary>
    /// Projects <see cref="Result"/> to a definite value: <see cref="TruthValue.True"/> and <see cref="TruthValue.False"/>
    /// pass through unchanged and <see cref="TruthValue.Unknown"/> becomes <paramref name="unknownAs"/> (ADR-0005 decision 12).
    /// It is the call-site counterpart of <c>COALESCE(rule, True)</c> / <c>COALESCE(rule, False)</c>, which does the same inside a
    /// rule. Pure: it does not change this decision, its <see cref="Faults"/> or <see cref="Result"/>, and
    /// <see cref="IsSatisfied"/> stays fail-closed regardless of the value chosen here.
    /// </summary>
    /// <param name="unknownAs">The definite value an <see cref="TruthValue.Unknown"/> result becomes: <see langword="true"/> for <see cref="TruthValue.True"/>, <see langword="false"/> for <see cref="TruthValue.False"/>. A <see cref="bool"/> so that an <see cref="TruthValue.Unknown"/> replacement cannot be requested.</param>
    /// <returns><see cref="TruthValue.True"/> or <see cref="TruthValue.False"/>, never <see cref="TruthValue.Unknown"/>.</returns>
    public TruthValue Project(bool unknownAs)
    {
        return this.Result switch
        {
            TruthValue.Unknown => unknownAs ? TruthValue.True : TruthValue.False,
            _ => this.Result,
        };
    }
}
