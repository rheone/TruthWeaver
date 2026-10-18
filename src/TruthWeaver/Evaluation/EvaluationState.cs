namespace TruthWeaver.Evaluation;

/// <summary>
/// The evaluation-scoped mutable state shared by every nested <c>Evaluator&lt;TContext&gt;</c> created while
/// evaluating one rule, including the private evaluators a quantifier's element iteration creates over
/// <c>Element&lt;TOuter, TItem&gt;</c> (ADR-0010 decision 5: "the evaluator state that is per evaluation ...
/// moves to one shared state object that the nested evaluators share"). Sharing one instance is what makes the
/// fault budget, <see cref="TruthWeaver.Evaluation.EvaluationOptions.MaxElementEvaluations"/> and the flat trace
/// log whole-evaluation concepts rather than per-quantifier ones: a fault raised three levels into nested
/// quantifiers counts against the same budget as one raised at the rule's root.
/// </summary>
internal sealed class EvaluationState(bool collectTrace = true)
{
    /// <summary>Gets every fault recorded anywhere in this evaluation, in the order they were raised.</summary>
    public List<Fault> Faults { get; } = [];

    /// <summary>Gets the flat trace log, in evaluation order. Stays empty when the caller did not request a trace.</summary>
    public List<TraceEntry> Trace { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the evaluation has aborted (the fault budget or
    /// <c>MaxElementEvaluations</c> was reached).
    /// </summary>
    public bool Aborted { get; set; }

    /// <summary>Gets or sets the total number of quantifier element-body evaluations performed so far, across every quantifier.</summary>
    public int ElementEvaluations { get; set; }

    /// <summary>
    /// Gets the per-evaluation term memo, keyed by the element scope path (<see langword="null"/> outside any
    /// quantifier) plus the term's identity (ADR-0010 decision 5: "the memo key inside a body is (scope path,
    /// term identity)").
    /// </summary>
    public Dictionary<(string? Scope, TermIdentity Identity), TruthValue> Memo { get; } = [];

    /// <summary>Gets the trace text of a term whose variables resolved, with the values shown, keyed the same way as <see cref="Memo"/>.</summary>
    public Dictionary<(string? Scope, TermIdentity Identity), string> ValueDescriptions { get; } = [];

    /// <summary>
    /// Gets the outcome note of each variable reference of a term (<c>from("user", "$.age"): resolved</c>), keyed the same
    /// way as <see cref="Memo"/>, in the order the term's references were resolved.
    /// </summary>
    public Dictionary<(string? Scope, TermIdentity Identity), List<string>> VariableOutcomes { get; } = [];

    /// <summary>Gets the zero-match array references of a term, keyed the same way as <see cref="Memo"/>.</summary>
    public Dictionary<(string? Scope, TermIdentity Identity), List<VariableReference>> ZeroMatchReferences { get; } = [];

    /// <summary>Gets the per-evaluation data-source query memo, keyed by the element scope path plus the reference.</summary>
    public Dictionary<(string? Scope, VariableReference Reference), SourceAnswer> SourceAnswers { get; } = [];

    /// <summary>
    /// Gets the per-evaluation collection-materialization cache, keyed by the enclosing memoization scope path
    /// (<see langword="null"/> outside any quantifier) plus the collection's registered name (ADR-0010 decision 5:
    /// "the selector is called at most once per (binding, outer element) within one evaluation"). The value is an
    /// opaque <see cref="CollectionMaterialization"/>; the owning <c>CollectionBinding&lt;TContext, TItem&gt;</c>
    /// is the only reader, and it casts <see cref="CollectionMaterialization.Elements"/> back to
    /// <c>IReadOnlyList&lt;TItem&gt;</c>, the type it stored.
    /// </summary>
    public Dictionary<(string? Scope, string Binding), CollectionMaterialization> CollectionMaterializations { get; } = [];

    /// <summary>Appends <paramref name="entry"/> to the flat trace log, unless the caller did not request a trace (<see cref="EvaluationOptions.IncludeTrace"/>).</summary>
    /// <param name="entry">The entry to record.</param>
    public void AddTrace(TraceEntry entry)
    {
        if (collectTrace)
        {
            this.Trace.Add(entry);
        }
    }
}
