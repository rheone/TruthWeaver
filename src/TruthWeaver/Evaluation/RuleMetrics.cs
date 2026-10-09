namespace TruthWeaver.Evaluation;

using TruthWeaver.Analysis;
using TruthWeaver.Ast;
using TruthWeaver.Rewriting;

/// <summary>
/// The size and analysis cost of a compiled rule, read from <see cref="CompiledRule{TContext}.Metrics"/>. Source-text
/// length misleads: an XOR-heavy rule is short as text but can have a very large analysis diagram.
/// </summary>
/// <param name="NodeCount">
/// The number of nodes in the expression tree, counted as a tree: a sub-expression that appears twice in the printed
/// rule counts twice.
/// </param>
/// <param name="MaxDepth">
/// The number of nodes on the longest root-to-leaf path. A rule that is a single term or constant has depth 1. This is
/// the measure <see cref="Compilation.CompilerOptions.MaxDepth"/> limits.
/// </param>
/// <param name="DistinctTermCount">
/// The number of distinct terms, where a term is a predicate name with its argument values. A term used twice counts once.
/// </param>
/// <param name="BddNodeCount">
/// The number of decision nodes in the shared binary decision diagram (BDD) manager after both rails of the Strong K3
/// analysis are built, not counting the two terminals. Sub-graphs shared between the rails or between sub-expressions
/// count once. It measures the cost of K3 analysis, not the size of a two-valued BDD. It is <see langword="null"/> when
/// the rule has more than <see cref="Compilation.CompilerOptions.MaxAnalysisTerms"/> distinct terms, because the analysis
/// is skipped there.
/// </param>
public sealed record RuleMetrics(long NodeCount, int MaxDepth, int DistinctTermCount, int? BddNodeCount)
{
    /// <summary>Measures a tree.</summary>
    /// <param name="root">The tree.</param>
    /// <param name="maxAnalysisTerms">The distinct-term cap above which the BDD node count is not computed.</param>
    /// <returns>The measures.</returns>
    internal static RuleMetrics Measure(Expression root, int maxAnalysisTerms)
    {
        return new RuleMetrics(
            ExpressionTools.Size(root),
            ExpressionTools.Depth(root),
            Analyzer.DistinctTerms(root).Count,
            Analyzer.BddNodeCount(root, maxAnalysisTerms)
        );
    }
}
