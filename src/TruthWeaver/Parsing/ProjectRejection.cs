namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// The single diagnostic every front end (DSL, JSON, YAML) raises when rule text still declares a <c>Project</c>.
/// Project is not a rule-language feature: the rule always yields the raw three-valued result, and the application picks
/// how <c>Unknown</c> becomes a definite value either inside the rule with <c>COALESCE(x, True)</c> / <c>COALESCE(x, False)</c> or
/// on the result with <c>Decision.Project(bool)</c> (ADR-0005 decision 12). Sharing one message keeps the migration advice
/// identical wherever a persisted rule is read.
/// </summary>
internal static class ProjectRejection
{
    private const string Message =
        "Project is not part of the rule language. To replace Unknown inside a rule write COALESCE(x, True) or COALESCE(x, False); to make the final result definite call Decision.Project(unknownAs) on the Decision.";

    private const string Hint =
        "Replace Project(x, True) with COALESCE(x, True) (or Project(x, False) with COALESCE(x, False)), or call Decision.Project(unknownAs) on the result.";

    /// <summary>Creates the diagnostic for a declared <c>Project</c>.</summary>
    /// <param name="code">The diagnostic code to report, <see cref="DiagnosticCodes.UnknownPredicate"/> on every surface.</param>
    /// <param name="span">The source span of the project expression, or <see cref="SourceSpan.None"/> for a tree format.</param>
    /// <param name="path">The JSON/YAML path of the offending node, or <see langword="null"/> for DSL text.</param>
    /// <returns>An error diagnostic whose suggestion points at <c>COALESCE</c> and <c>Decision.Project</c>.</returns>
    public static Diagnostic Create(string code, SourceSpan span, string? path = null)
    {
        return Diagnostic.Error(
            code,
            Message,
            span,
            expected: "a rule without Project",
            found: "Project",
            suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, Hint),
            path: path
        );
    }
}
