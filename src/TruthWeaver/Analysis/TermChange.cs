namespace TruthWeaver.Analysis;

/// <summary>One term change of a <see cref="ChangeSet"/>: the term, the value the facts give it, and the value to set.</summary>
/// <param name="Term">The printed term, such as <c>hasRole(role: "admin")</c>, as <see cref="TermIdentity.ToString"/> writes it.</param>
/// <param name="From">
/// The value the facts give the term, or <see langword="null"/> when the facts do not give one: the term is not in the
/// facts dictionary, or the decision did not evaluate it.
/// </param>
/// <param name="To">The value to set. It can be <see cref="TruthValue.Unknown"/>.</param>
public sealed record TermChange(string Term, TruthValue? From, TruthValue To);
