namespace TruthWeaver.Evaluation;

/// <summary>
/// The outcome of materializing one quantifier's collection selector, cached in
/// <see cref="EvaluationState.CollectionMaterializations"/> so a second quantifier over the same binding and outer
/// element reuses it instead of calling the selector again (ADR-0010 decision 5).
/// </summary>
/// <param name="Faulted"><see langword="true"/> when the selector threw; <paramref name="Fault"/> holds the exception and <paramref name="Elements"/> is unused.</param>
/// <param name="Fault">The exception the selector threw, or <see langword="null"/> when it did not.</param>
/// <param name="Elements">The materialized elements as an <c>IReadOnlyList&lt;TItem&gt;</c> boxed to <see langword="object"/>, or <see langword="null"/> for a <see langword="null"/> collection (when <paramref name="Faulted"/> is <see langword="false"/>).</param>
internal readonly record struct CollectionMaterialization(bool Faulted, Exception? Fault, object? Elements);
