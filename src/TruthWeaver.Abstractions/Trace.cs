namespace TruthWeaver.Abstractions;

/// <summary>
/// A literal, ordered record of every node an evaluation visited or explicitly skipped.
/// </summary>
/// <param name="Entries">
/// The trace entries, in evaluation (left-to-right, depth-first) order. Each constant, term, operator and quantifier
/// that was visited or skipped has an entry, and the entry of an operator or a quantifier follows the entries of its
/// operands, because its result is known only after them.
/// </param>
public sealed record Trace(EquatableArray<TraceEntry> Entries);
