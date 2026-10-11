namespace TruthWeaver.Tool.Core;

/// <summary>The outcome of one level of a validation.</summary>
public enum ValidationStatus
{
    /// <summary>The level ran and found no error and no warning. It can have information diagnostics.</summary>
    Passed,

    /// <summary>The level ran and found warnings but no error.</summary>
    PassedWithWarnings,

    /// <summary>The level ran and found at least one error.</summary>
    Failed,

    /// <summary>The level is on but did not run: it does not apply to the rule, or an earlier level gave it nothing to check.</summary>
    Skipped,

    /// <summary>The options switched the level off.</summary>
    Off,
}
