namespace TruthWeaver.Tool.Core.Assignment;

/// <summary>One term of a rule in an <see cref="AssignmentOutcome"/>, with the value the stub assignment gives it.</summary>
/// <param name="Display">The term as canonical rule text, such as <c>hasRole(role: "admin")</c>.</param>
/// <param name="Key">
/// The key of the term in the stub assignment, which is the canonical text of the term: the same as <paramref name="Display"/>
/// for a term with literal arguments. A term with a <c>from(...)</c> argument has the key of the term with the value that the
/// data gives, such as <c>inRegion(region: "EU")</c>; it is <see langword="null"/> while no data resolves the reference.
/// </param>
/// <param name="Value">The value of the term in the assignment: <see cref="TruthValue.Unknown"/> when the assignment has no entry.</param>
/// <param name="HasDataReference">Whether the term has a <c>from(...)</c> argument.</param>
public sealed record TermAssignment(string Display, string? Key, TruthValue Value, bool HasDataReference);
