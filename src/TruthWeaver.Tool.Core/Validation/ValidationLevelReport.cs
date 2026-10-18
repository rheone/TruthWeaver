namespace TruthWeaver.Tool.Core.Validation;

using TruthWeaver.Diagnostics;

/// <summary>The result of one level of a validation.</summary>
/// <param name="Level">The level: one flag of <see cref="ValidationLevels"/>.</param>
/// <param name="Status">The outcome.</param>
/// <param name="Diagnostics">The diagnostics of this level only.</param>
/// <param name="Reason">Why the level was skipped, when <paramref name="Status"/> is <see cref="ValidationStatus.Skipped"/>; otherwise <see langword="null"/>.</param>
public sealed record ValidationLevelReport(
    ValidationLevels Level,
    ValidationStatus Status,
    EquatableArray<Diagnostic> Diagnostics,
    string? Reason = null
)
{
    /// <summary>Creates the report of a level that ran, with the status from the most severe diagnostic.</summary>
    /// <param name="level">The level.</param>
    /// <param name="diagnostics">The diagnostics of the level.</param>
    /// <returns>The report.</returns>
    internal static ValidationLevelReport Ran(ValidationLevels level, IEnumerable<Diagnostic> diagnostics)
    {
        EquatableArray<Diagnostic> all = new(diagnostics);
        ValidationStatus status = ValidationStatus.Passed;
        if (all.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            status = ValidationStatus.Failed;
        }
        else if (all.Any(d => d.Severity == DiagnosticSeverity.Warning))
        {
            status = ValidationStatus.PassedWithWarnings;
        }

        return new ValidationLevelReport(level, status, all);
    }

    /// <summary>Creates the report of a level that did not run.</summary>
    /// <param name="level">The level.</param>
    /// <param name="reason">Why it did not run.</param>
    /// <returns>The report.</returns>
    internal static ValidationLevelReport Skip(ValidationLevels level, string reason)
    {
        return new ValidationLevelReport(level, ValidationStatus.Skipped, new EquatableArray<Diagnostic>([]), reason);
    }

    /// <summary>Creates the report of a level that the options switched off.</summary>
    /// <param name="level">The level.</param>
    /// <returns>The report.</returns>
    internal static ValidationLevelReport SwitchedOff(ValidationLevels level)
    {
        return new ValidationLevelReport(level, ValidationStatus.Off, new EquatableArray<Diagnostic>([]));
    }
}
