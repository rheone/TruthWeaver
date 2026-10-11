namespace TruthWeaver.Tool.Core;

using TruthWeaver.Diagnostics;

/// <summary>
/// The severity model of the tools: which diagnostics fail a command, and the exit code. An error always fails. A warning
/// fails only in strict mode (<c>--strict</c>). An information diagnostic never fails.
/// </summary>
public static class SeverityPolicy
{
    /// <summary>The tool core errors that mean the tool could not do its work, not that the rule has a problem.</summary>
    private static readonly HashSet<string> UnusableCodes =
    [
        ToolDiagnosticCodes.InputUnreadable,
        ToolDiagnosticCodes.InvalidEncoding,
        ToolDiagnosticCodes.MalformedDocument,
        ToolDiagnosticCodes.InvalidPath,
        ToolDiagnosticCodes.PathNotFound,
        ToolDiagnosticCodes.NotARule,
        ToolDiagnosticCodes.InvalidConfiguration,
        ToolDiagnosticCodes.InvalidStubValue,
        ToolDiagnosticCodes.OutputFailed,
        ToolDiagnosticCodes.DuplicateDataSource,
        ToolDiagnosticCodes.InvalidSchemaFile,
        ToolDiagnosticCodes.RendererNotFound,
        ToolDiagnosticCodes.RenderFailed,
        ToolDiagnosticCodes.UnknownArtifact,
    ];

    /// <summary>Tells whether a diagnostic fails a command.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    /// <param name="strict"><see langword="true"/> for strict mode, in which a warning fails too.</param>
    /// <returns><see langword="true"/> for an error, and for a warning in strict mode.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is <see langword="null"/>.</exception>
    public static bool Fails(Diagnostic diagnostic, bool strict)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return diagnostic.Severity == DiagnosticSeverity.Error || (strict && diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Gets the exit code for the diagnostics of a command: <see cref="ToolExitCode.Unusable"/> when a tool core error says
    /// that the tool could not read its input, configuration or schema file, write its output, find an artifact, or render an
    /// image;
    /// <see cref="ToolExitCode.Failed"/> when any other diagnostic fails (see <see cref="Fails"/>); otherwise
    /// <see cref="ToolExitCode.Success"/>.
    /// </summary>
    /// <param name="diagnostics">Every diagnostic of the command.</param>
    /// <param name="strict"><see langword="true"/> for strict mode, in which a warning fails too.</param>
    /// <returns>The exit code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is <see langword="null"/>.</exception>
    public static ToolExitCode ExitCodeFor(IEnumerable<Diagnostic> diagnostics, bool strict)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        ToolExitCode code = ToolExitCode.Success;
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error && UnusableCodes.Contains(diagnostic.Code))
            {
                return ToolExitCode.Unusable;
            }

            if (Fails(diagnostic, strict))
            {
                code = ToolExitCode.Failed;
            }
        }

        return code;
    }
}
