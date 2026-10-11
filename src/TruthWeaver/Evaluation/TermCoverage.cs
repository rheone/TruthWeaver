namespace TruthWeaver.Evaluation;

/// <summary>
/// The coverage of one distinct term of a rule over a set of decisions (<see cref="RuleCoverage"/>). A term that occurs
/// more than once in the rule has one entry: the evaluator runs it once per evaluation.
/// </summary>
/// <param name="Term">The canonical text of the term, for example <c>hasRole(role: "admin")</c>.</param>
/// <param name="EvaluatedCount">The number of decisions in which the term was evaluated (not skipped by short-circuiting or a fault budget).</param>
/// <param name="TrueCount">The number of decisions in which the term was <see cref="TruthValue.True"/>.</param>
/// <param name="FalseCount">The number of decisions in which the term was <see cref="TruthValue.False"/>.</param>
/// <param name="UnknownCount">The number of decisions in which the term was <see cref="TruthValue.Unknown"/>, a fault included.</param>
/// <param name="FaultCount">The number of decisions in which the predicate of the term faulted.</param>
/// <param name="DecidedCount">
/// The number of decisions the term decided: a different value of the term (at every place it occurs), with every
/// other evaluated node kept and every skipped node read as <see cref="TruthValue.Unknown"/>, changes the result of the rule.
/// </param>
public sealed record TermCoverage(
    string Term,
    int EvaluatedCount,
    int TrueCount,
    int FalseCount,
    int UnknownCount,
    int FaultCount,
    int DecidedCount
)
{
    /// <summary>Gets a value indicating whether the term decided no decision. Such a term is a candidate for a dead branch.</summary>
    public bool NeverDecided => this.DecidedCount == 0;

    /// <summary>Gets a value indicating whether the term was evaluated at least once and was <see cref="TruthValue.Unknown"/> every time.</summary>
    public bool AlwaysUnknown => this.EvaluatedCount > 0 && this.UnknownCount == this.EvaluatedCount;
}
