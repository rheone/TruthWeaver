namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// The single diagnostic every front end (DSL, JSON, YAML, builder) raises when rule text still declares a
/// <c>Collapse</c>. Collapse is not a rule-language feature: the rule always yields the raw three-valued result, and the
/// application picks a policy at the call site with <c>Decision.Collapse(CollapsePolicy)</c> (ADR-0005 decision 14). Sharing
/// one message keeps the migration advice identical wherever a persisted rule is read.
/// </summary>
internal static class CollapseRejection
{
    private const string Message =
        "Collapse is not part of the rule language: a rule always yields the three-valued result. Evaluate the rule, then call Decision.Collapse(policy) on the Decision to choose how Unknown is resolved.";

    private const string Hint =
        "Remove Collapse from the rule and call Decision.Collapse(CollapsePolicy.UnknownAsFalse), UnknownAsTrue or UnknownIsError on the result instead.";

    /// <summary>Creates the diagnostic for a declared <c>Collapse</c>.</summary>
    /// <param name="code">The diagnostic code to report, <see cref="DiagnosticCodes.UnknownPredicate"/> on every surface.</param>
    /// <param name="span">The source span of the collapse expression, or <see cref="SourceSpan.None"/> for a tree format.</param>
    /// <param name="path">The JSON/YAML path of the offending node, or <see langword="null"/> for DSL text.</param>
    /// <returns>An error diagnostic whose suggestion points at <c>Decision.Collapse</c>.</returns>
    public static Diagnostic Create(string code, SourceSpan span, string? path = null)
    {
        return Diagnostic.Error(
            code,
            Message,
            span,
            expected: "a rule without Collapse",
            found: "Collapse",
            suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, Hint),
            path: path
        );
    }
}
