namespace TruthWeaver.Tool.Core.Configuration;

using TruthWeaver.Diagnostics;

/// <summary>
/// Describes the first error of a list of diagnostics in one line, for a status line. <see cref="ToolResult{T}.FailureSummary"/>
/// uses it for a result; use it directly when only the diagnostics are at hand.
/// </summary>
public static class DiagnosticSummary
{
    /// <summary>The fallback text of <see cref="FirstError"/> and <see cref="ToolResult{T}.FailureSummary"/>.</summary>
    public const string UnknownProblem = "unknown problem.";

    /// <summary>Describes the first error diagnostic as <c>code: message</c>. Warnings and information are not described.</summary>
    /// <param name="diagnostics">The diagnostics.</param>
    /// <param name="fallback">The text for a list with no error diagnostic.</param>
    /// <returns>The description of the first error, or <paramref name="fallback"/> when there is none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> or <paramref name="fallback"/> is <see langword="null"/>.</exception>
    public static string FirstError(IEnumerable<Diagnostic> diagnostics, string fallback = UnknownProblem)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(fallback);

        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? fallback : $"{first.Code}: {first.Message}";
    }
}
