namespace TruthWeaver.Tool.Core;

using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>
/// The result of one rewrite before it is accepted: the rule before and after, as rule text and as a plain text tree, the
/// size of each, the steps of <c>Simplify</c> and the diagnostics of the rewrite (such as <c>TRE0033</c>, the rewrite did not
/// converge).
/// </summary>
/// <param name="Kind">The rewrite.</param>
/// <param name="Before">The rule before the rewrite.</param>
/// <param name="After">The rule after the rewrite, or <see langword="null"/> when the rewrite gave no rule (such as <c>TRE0016</c>, the result is too large).</param>
/// <param name="Steps">The steps of <see cref="RuleRewriteKind.Simplify"/>, in the order they were applied; empty for the other rewrites.</param>
/// <param name="Diagnostics">The diagnostics of the rewrite.</param>
public sealed record RewritePreview(
    RuleRewriteKind Kind,
    CompiledRule<StubAssignment> Before,
    CompiledRule<StubAssignment>? After,
    IReadOnlyList<RewriteStep> Steps,
    EquatableArray<Diagnostic> Diagnostics
)
{
    /// <summary>Gets a value indicating whether the rewrite gave a rule whose tree differs from the rule before it.</summary>
    public bool Changed =>
        this.After is not null
        && !string.Equals(
            this.After.PrintJson(includeNotes: false),
            this.Before.PrintJson(includeNotes: false),
            StringComparison.Ordinal
        );

    /// <summary>Gets the rule text before the rewrite, without notes, since the rule after a rewrite has none.</summary>
    public string BeforeText => this.Before.CanonicalText;

    /// <summary>Gets the rule text after the rewrite, or <see langword="null"/> when it gave no rule.</summary>
    public string? AfterText => this.After?.CanonicalText;

    /// <summary>Gets the plain text tree before the rewrite.</summary>
    public string BeforeTree => this.Before.PrintPlainText();

    /// <summary>Gets the plain text tree after the rewrite, or <see langword="null"/> when it gave no rule.</summary>
    public string? AfterTree => this.After?.PrintPlainText();
}
