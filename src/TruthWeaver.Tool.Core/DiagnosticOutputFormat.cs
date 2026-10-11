namespace TruthWeaver.Tool.Core;

/// <summary>The output formats of <see cref="DiagnosticWriter"/>.</summary>
public enum DiagnosticOutputFormat
{
    /// <summary>Plain text: a header line for each diagnostic, the source line with a caret underline, and the expected, found and suggestion lines.</summary>
    Text,

    /// <summary>A JSON document with one object for each diagnostic.</summary>
    Json,

    /// <summary>A SARIF 2.1.0 log, for code scanning in CI.</summary>
    Sarif,
}
