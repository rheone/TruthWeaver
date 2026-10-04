namespace TruthWeaver.Abstractions;

/// <summary>
/// One syntax problem an <see cref="IQueryValidator"/> found in a query (ADR-0006 decision 4). The compiler turns it into a
/// diagnostic that points at the query string in the rule.
/// </summary>
/// <param name="Message">What is wrong, in the source's own dialect terms. It must not contain data values.</param>
/// <param name="Position">The 0-based character offset within the query where the problem was detected, or <see langword="null"/> when the validator cannot say.</param>
public sealed record QueryProblem(string Message, int? Position = null);
