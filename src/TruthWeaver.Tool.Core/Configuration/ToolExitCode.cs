namespace TruthWeaver.Tool.Core.Configuration;

using TruthWeaver.Tool.Core.Diagnostics;

/// <summary>
/// The process exit code of a tool command, from its diagnostics. <see cref="SeverityPolicy.ExitCodeFor"/> maps the
/// diagnostics to a code. The values are stable: scripts and CI jobs test them.
/// </summary>
public enum ToolExitCode
{
    /// <summary>The command did its work and found no failure: no error, and no warning in strict mode.</summary>
    Success = 0,

    /// <summary>The command found a failure in the rule: an error, or a warning in strict mode.</summary>
    Failed = 1,

    /// <summary>
    /// The command could not do its work: a tool core (<c>TWT</c>) error about the input, the configuration, a schema file,
    /// an output file, an artifact id or an image renderer. This code wins over <see cref="Failed"/>.
    /// </summary>
    Unusable = 2,
}
