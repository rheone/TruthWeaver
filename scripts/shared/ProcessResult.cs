namespace TruthWeaver.Scripts;

/// <summary>The captured output of a finished process.</summary>
/// <param name="StandardOutput">Everything the process wrote to standard output.</param>
/// <param name="StandardError">Everything the process wrote to standard error.</param>
public sealed record ProcessResult(string StandardOutput, string StandardError);
