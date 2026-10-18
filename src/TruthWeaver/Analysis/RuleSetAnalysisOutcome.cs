namespace TruthWeaver.Analysis;

/// <summary>The verdict of <see cref="RuleSetAnalysis.Analyze{TContext}(IReadOnlyList{TruthWeaver.Evaluation.CompiledRule{TContext}}, int?, int?)"/>.</summary>
public enum RuleSetAnalysisOutcome
{
    /// <summary>
    /// Every answer was computed: <see cref="RuleSetAnalysisResult.Pairs"/>, <see cref="RuleSetAnalysisResult.Rules"/>,
    /// <see cref="RuleSetAnalysisResult.RemovableTogether"/> and <see cref="RuleSetAnalysisResult.Shadowing"/>.
    /// </summary>
    Decided,

    /// <summary>
    /// The set-level answers (redundancy, the removable set and shadowing) were not computed, because the rules have
    /// more distinct terms between them than the term cap, or because the shared BDD of the set would exceed its node
    /// budget. <see cref="RuleSetAnalysisResult.Rules"/>, <see cref="RuleSetAnalysisResult.RemovableTogether"/> and
    /// <see cref="RuleSetAnalysisResult.Shadowing"/> are empty.
    /// The pairwise answers were still computed pair by pair, and each pair has its own outcome.
    /// </summary>
    Undecided,

    /// <summary>
    /// Nothing was computed, because the set has more rules than the rule cap. Every list of the result is empty.
    /// </summary>
    TooManyRules,
}
