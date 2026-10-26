namespace TruthWeaver.Evaluation;

// Ticket 34: the term memo key (element scope, registry, term identity) and the collection-materialization key
// (element scope, registry, collection name) are aliased here, file-locally, so the generic Dictionary property
// types below stay on one line under CSharpier's print width, as a plain get-only auto-property. A multi-line
// generic type on such a property is the documented CSharpier/dotnet format disagreement (CLAUDE.md
// "Formatting"); the alias avoids it rather than wrapping. Evaluator{TContext} and
// CollectionBinding{TContext, TItem} build the same-shaped key as a plain tuple (a using alias is file-scoped, and
// their own declarations never needed one to stay under the print width); tuple element names are not part of the
// type, so both convert to/from this alias without a cast.
using CollectionMemoKey = (string? Scope, object Registry, string Binding);
using MemoKey = (string? Scope, object Registry, TermIdentity Identity);

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
    /// quantifier), the registry the term was resolved against, plus the term's identity (ADR-0010 decision 5:
    /// "the memo key inside a body is (scope path, term identity)", widened by ticket 34 with the registry
    /// component below). The registry component is the <c>PredicateRegistry&lt;TContext&gt;</c> instance that
    /// resolved the term, upcast to <see langword="object"/> (a reference-type conversion, so the key shape does
    /// not depend on <c>TContext</c>) and compared by reference. <c>TruthWeaver.Policies.PolicySet&lt;TContext&gt;</c>
    /// shares one <see cref="EvaluationState"/> across rules that may be compiled against different registries (a
    /// different compile mode, or a different implementation bound to the same predicate name), and without this
    /// component two such rules could produce an equal <see cref="TermIdentity"/> that is not actually the same
    /// predicate, letting one rule's answer silently stand in for the other's.
    /// </summary>
    public Dictionary<MemoKey, TruthValue> Memo { get; } = [];

    /// <summary>Gets the trace text of a term whose variables resolved, with the values shown, keyed the same way as <see cref="Memo"/>.</summary>
    public Dictionary<MemoKey, string> ValueDescriptions { get; } = [];

    /// <summary>
    /// Gets the outcome note of each variable reference of a term (<c>from("user", "$.age"): resolved</c>), keyed the same
    /// way as <see cref="Memo"/>, in the order the term's references were resolved.
    /// </summary>
    public Dictionary<MemoKey, List<string>> VariableOutcomes { get; } = [];

    /// <summary>Gets the zero-match array references of a term, keyed the same way as <see cref="Memo"/>.</summary>
    public Dictionary<MemoKey, List<VariableReference>> ZeroMatchReferences { get; } = [];

    /// <summary>
    /// Gets the per-evaluation data-source query memo, keyed by the memoization scope path the named source was bound
    /// at (a quantifier element's own source at that element's path, every other source at <see langword="null"/>)
    /// plus the reference, so each <c>(source, query)</c> pair is queried at most once (ADR-0006 decision 11).
    /// </summary>
    public Dictionary<(string? Scope, VariableReference Reference), SourceAnswer> SourceAnswers { get; } = [];

    /// <summary>
    /// Gets the per-evaluation collection-materialization cache, keyed by the enclosing memoization scope path
    /// (<see langword="null"/> outside any quantifier), the registry the quantifier itself was evaluated against,
    /// plus the collection's registered name (ADR-0010 decision 5: "the selector is called at most once per
    /// (binding, outer element) within one evaluation", widened by ticket 34 with the registry component). The
    /// registry component is compared by reference, exactly as for <see cref="Memo"/>, so two quantifiers over a
    /// like-named collection registered on different registries never share a materialization. The value is an
    /// opaque <see cref="CollectionMaterialization"/>; the owning <c>CollectionBinding&lt;TContext, TItem&gt;</c>
    /// is the only reader, and it casts <see cref="CollectionMaterialization.Elements"/> back to
    /// <c>IReadOnlyList&lt;TItem&gt;</c>, the type it stored.
    /// </summary>
    public Dictionary<CollectionMemoKey, CollectionMaterialization> CollectionMaterializations { get; } = [];

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
