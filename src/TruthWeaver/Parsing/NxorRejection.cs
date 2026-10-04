namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// The single diagnostic every front end (DSL, JSON, YAML) raises when rule text still uses the retired <c>NXOR</c>
/// spelling. The operator is now <c>PARITY</c> (ADR-0005 decision 4): <c>NXOR</c> conventionally means negated XOR
/// (<c>XNOR</c>, which is <c>EQUIVALENT</c>), the opposite of what n-ary parity does, so the old word is rejected rather than
/// kept as an alias. The diagnostic carries a "did you mean" replacement because the two words are too far apart for the
/// edit-distance suggester to find on its own.
/// </summary>
internal static class NxorRejection
{
    private const string Message =
        "NXOR has been renamed PARITY: NXOR conventionally means negated XOR (XNOR, i.e. EQUIVALENT), the opposite of n-ary parity, so the NXOR spelling is no longer accepted.";

    /// <summary>Creates the diagnostic for a declared <c>NXOR</c>.</summary>
    /// <param name="code">The diagnostic code of the front end's error class (<see cref="DiagnosticCodes.SyntaxError"/> for DSL text, <see cref="DiagnosticCodes.MalformedTree"/> for JSON/YAML).</param>
    /// <param name="span">The source span of the call, or <see cref="SourceSpan.None"/> for a tree format.</param>
    /// <param name="replacement">The spelling to suggest in the front end's own style: <c>PARITY</c> for DSL text, <c>parity</c> for a tree <c>op</c>.</param>
    /// <param name="path">The JSON/YAML path of the offending node, or <see langword="null"/> for DSL text.</param>
    /// <returns>An error diagnostic whose suggestion is the <paramref name="replacement"/> spelling.</returns>
    public static Diagnostic Create(string code, SourceSpan span, string replacement, string? path = null)
    {
        return Diagnostic.Error(
            code,
            Message,
            span,
            expected: "PARITY",
            found: "NXOR",
            suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, replacement),
            path: path
        );
    }
}
