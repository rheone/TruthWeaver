namespace TruthWeaver.Compilation;

/// <summary>
/// Compile-time resource bounds and mode (ADR-0002/ticket 09), so a rule authored somewhere not
/// code-reviewed (e.g. an admin UI backed by a database) cannot pathologically hang a request thread.
/// </summary>
/// <param name="MaxDepth">The maximum expression tree depth. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxNodeCount">The maximum total node count. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxAnalysisTerms">
/// The maximum number of distinct term identities the BDD-based analyzer will consider; beyond this,
/// analysis is skipped and reported as an <c>Info</c> diagnostic, never silently treated as
/// "not constant".
/// </param>
/// <param name="Mode">How an unregistered predicate name is treated.</param>
/// <param name="MaxRewriteNodeCount">
/// The maximum node count, counted as a printed tree, that <c>ExpandToPrimitives</c>, <c>ExpandToNand</c> and
/// <c>ExpandToNor</c> may produce. A rewrite that would exceed it is refused with a <c>TRE0016</c> error diagnostic
/// instead of being built. Pass a larger value to those methods to allow bigger results.
/// </param>
/// <param name="Lints">
/// The opt-in lint rules to run after analysis. Defaults to <see cref="LintRules.None"/>, so enabling none of them
/// leaves a rule's diagnostics exactly as they were. Findings are <c>Info</c> diagnostics and never block compilation.
/// </param>
/// <param name="DataSources">
/// The data source names a rule may use in <c>from("name", "query")</c> (ADR-0006). <see langword="null"/> (the default)
/// declares none, so any variable reference is a <c>TRE0024</c> error.
/// </param>
public sealed record CompilerOptions(
    int MaxDepth = 32,
    int MaxNodeCount = 512,
    int MaxAnalysisTerms = 20,
    CompilationMode Mode = CompilationMode.Strict,
    int MaxRewriteNodeCount = 100_000,
    LintRules Lints = LintRules.None,
    DataSourceDeclarations? DataSources = null
)
{
    /// <summary>Gets the default options: 32 / 512 / 20 / <see cref="CompilationMode.Strict"/> / 100000.</summary>
    public static CompilerOptions Default { get; } = new();
}
