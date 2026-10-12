namespace TruthWeaver.Analysis;

using TruthWeaver.Compilation;

/// <summary>The settings of a <see cref="RuleCounterfactual"/> search.</summary>
/// <param name="Target">The value the rule must take. The default is <see cref="TruthValue.True"/>.</param>
/// <param name="FixedTerms">
/// The printed terms that the search must not change, such as <c>isEmployee</c> for a fact that the caller cannot
/// change. Each must be a term of the rule. <see langword="null"/> fixes no term.
/// </param>
/// <param name="MaxChangeSets">
/// The most change sets to return, at least 1. The default is <see cref="RuleCounterfactual.DefaultMaxChangeSets"/>.
/// </param>
/// <param name="MaxAnalysisTerms">
/// The most distinct terms the search decides. <see langword="null"/> uses the <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
/// </param>
public sealed record CounterfactualOptions(
    TruthValue Target = TruthValue.True,
    IReadOnlyCollection<string>? FixedTerms = null,
    int MaxChangeSets = RuleCounterfactual.DefaultMaxChangeSets,
    int? MaxAnalysisTerms = null
);
