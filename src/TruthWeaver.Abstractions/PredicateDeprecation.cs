namespace TruthWeaver.Abstractions;

/// <summary>
/// Marks a <see cref="PredicateSchema"/> as deprecated, so the compiler warns (<c>TRE0027</c>) on every use of the
/// predicate in a rule.
/// </summary>
/// <param name="ReplacedBy">
/// The registered name of the predicate to use instead, or <see langword="null"/> when there is none. The warning names it
/// and offers it as a suggestion.
/// </param>
/// <param name="Message">
/// Extra advice for the rule author, such as when the predicate will be removed, or <see langword="null"/> for none.
/// The warning message includes it.
/// </param>
public sealed record PredicateDeprecation(string? ReplacedBy, string? Message);
