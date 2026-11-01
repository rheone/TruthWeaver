namespace TruthWeaver.Evaluation;

/// <summary>
/// What kind of thing faulted, so the evaluation log names it correctly. All three go through
/// <see cref="FaultRecorder.Record"/> and all three are logged under the same event id; the name of the value appears in the
/// rendered message (<c>"{Origin} fault for term {Term}: ..."</c>) and as the <c>Origin</c> structured field.
/// </summary>
internal enum FaultOrigin
{
    /// <summary>A predicate, or a variable's data source, threw, timed out or was otherwise absorbed as <c>Unknown</c>.</summary>
    Predicate,

    /// <summary>A collection's selector threw while it materialized the elements of a <c>FORALL</c> or <c>EXISTS</c>.</summary>
    Collection,

    /// <summary>A quantifier reached <see cref="EvaluationOptions.MaxElementEvaluations"/>.</summary>
    ElementLimit,
}
