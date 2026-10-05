namespace TruthWeaver.Abstractions;

/// <summary>
/// One problem a <see cref="PredicateSchema.ArgumentValidator"/> found in the literal arguments of a predicate call, for
/// example reversed bounds on a range predicate. The compiler turns it into an error diagnostic that points at the call,
/// so the rule does not compile.
/// </summary>
/// <param name="Message">What is wrong, naming the arguments involved.</param>
/// <param name="Expected">What the arguments must satisfy, for the diagnostic's expected text (for example "'lower' less than or equal to 'upper'").</param>
/// <param name="Found">What the call supplied, for the diagnostic's found text (for example "'lower' is 20 and 'upper' is 10").</param>
/// <param name="Suggestion">A fix to offer the author, or <see langword="null"/> when there is none. The compiler never applies it.</param>
public sealed record PredicateArgumentProblem(string Message, string Expected, string Found, string? Suggestion = null);
