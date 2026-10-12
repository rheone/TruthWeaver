namespace TruthWeaver.Tool;

/// <summary>How a command writes its outcome.</summary>
internal enum OutputMode
{
    /// <summary>The result as text on standard output, and the diagnostics as text on standard error.</summary>
    Text,

    /// <summary>One JSON envelope on standard output: the command, the exit code, the diagnostics and the result.</summary>
    Json,

    /// <summary>A SARIF 2.1.0 log of the diagnostics on standard output (<c>validate --sarif</c>).</summary>
    Sarif,
}
