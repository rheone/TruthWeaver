namespace TruthWeaver.Compilation;

/// <summary>
/// Compile-time resource bounds and mode (ADR-0002/ticket 09), so a rule authored somewhere not
/// code-reviewed (e.g. an admin UI backed by a database) cannot pathologically hang a request thread.
/// </summary>
/// <remarks>
/// Each limit, with its default and what happens when a rule exceeds it:
/// <list type="table">
/// <listheader><term>Limit</term><description>Outcome when exceeded</description></listheader>
/// <item><term><c>MaxDepth</c> (32)</term><description><c>Compile</c> returns a <c>TRE0009</c> error and no rule.</description></item>
/// <item><term><c>MaxNodeCount</c> (512)</term><description><c>Compile</c> returns a <c>TRE0010</c> error and no rule.</description></item>
/// <item><term><c>MaxAnalysisTerms</c> (20)</term><description>The analysis is skipped with a <c>TRE0011</c> info diagnostic and the rule compiles. Equivalence checks are undecided.</description></item>
/// <item><term><c>MaxRewriteNodeCount</c> (100,000)</term><description>An expanding rewrite or normal form returns a <c>TRE0016</c> error and no rule. A result under this cap can still be over <c>MaxNodeCount</c>, so its text compiles back only when <c>MaxNodeCount</c> is raised.</description></item>
/// <item><term><c>EvaluationOptions.FaultBudget</c></term><description>Evaluation stops when the faults reach the budget. The terms not yet run are <c>Unknown</c>.</description></item>
/// <item><term><c>EvaluationOptions.Timeout</c></term><description><c>EvaluateAsync</c> throws <see cref="OperationCanceledException"/>. It records no fault.</description></item>
/// </list>
/// </remarks>
/// <param name="MaxDepth">The maximum expression tree depth. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxNodeCount">The maximum total node count. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxAnalysisTerms">
/// The maximum number of distinct term identities the BDD-based analyzer will consider; beyond this,
/// analysis is skipped and reported as an <c>Info</c> diagnostic, never silently treated as
/// "not constant".
/// </param>
/// <param name="Mode">How an unregistered predicate name is treated.</param>
/// <param name="MaxRewriteNodeCount">
/// The maximum node count, counted as a printed tree, that <c>ExpandToPrimitives</c>, <c>ExpandToNand</c>,
/// <c>ExpandToNor</c>, <c>ToNnf</c>, <c>ToCnf</c> and <c>ToDnf</c> may produce. A rewrite that would exceed it is refused
/// with a <c>TRE0016</c> error diagnostic instead of being built. Pass a larger value to those methods to allow bigger results.
/// </param>
/// <param name="Lints">
/// The opt-in lint rules to run after analysis. Defaults to <see cref="LintRules.None"/>, so enabling none of them
/// leaves a rule's diagnostics exactly as they were. Findings are <c>Info</c> diagnostics and never block compilation.
/// </param>
/// <param name="DataSources">
/// The data source names a rule may use in <c>from("name", "query")</c> (ADR-0006). <see langword="null"/> (the default)
/// declares none, so any variable reference is a <c>TRE0024</c> error.
/// </param>
/// <param name="DeepNestingFraction">
/// The share of <paramref name="MaxDepth"/> at which the <see cref="LintRules.DeepNesting"/> lint reports a rule. The
/// default is 0.75, so with the default <paramref name="MaxDepth"/> of 32 a rule 24 levels deep or more is reported.
/// </param>
/// <param name="WideChainOperandLimit">
/// The operand count above which the <see cref="LintRules.WideChain"/> lint reports an <c>AND</c> or <c>OR</c> chain. The
/// default is 16, so a chain of 17 operands is reported.
/// </param>
public sealed record CompilerOptions(
    int MaxDepth = 32,
    int MaxNodeCount = 512,
    int MaxAnalysisTerms = 20,
    CompilationMode Mode = CompilationMode.Strict,
    int MaxRewriteNodeCount = 100_000,
    LintRules Lints = LintRules.None,
    DataSourceDeclarations? DataSources = null,
    double DeepNestingFraction = 0.75,
    int WideChainOperandLimit = 16
)
{
    /// <summary>Gets the default options: 32 / 512 / 20 / <see cref="CompilationMode.Strict"/> / 100000.</summary>
    public static CompilerOptions Default { get; } = new();
}
