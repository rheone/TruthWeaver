namespace TruthWeaver.Tool.Core.Assignment;

using TruthWeaver.Abstractions;

/// <summary>
/// The outcome of one term of a rule: its value, its resolved key and the faults an evaluation recorded for it, found by
/// <see cref="TermOutcomeLookup"/>.
/// </summary>
/// <param name="Display">The term as canonical rule text, such as <c>hasRole(role: "admin")</c>. Matches <see cref="TermAssignment.Display"/>.</param>
/// <param name="Key">The resolved key of the term, or <see langword="null"/> while unresolved. See <see cref="TermAssignment.Key"/>.</param>
/// <param name="Value">The value of the term in the assignment.</param>
/// <param name="Faults">
/// The faults the evaluation recorded for this term, in the order they occurred; empty when none were recorded. Each
/// fault is attributed by matching its own <see cref="Fault.Term"/> identity against this term, not by any text search
/// of a fault's message, so a fault whose message happens to mention another term's display is never misattributed.
/// </param>
public sealed record TermOutcome(string Display, string? Key, TruthValue Value, IReadOnlyList<Fault> Faults);
