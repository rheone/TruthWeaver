namespace TruthWeaver.Tool.Core.Validation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Reports;

/// <summary>The result of <see cref="RuleValidator.Validate"/>: one report for each level, and the statistics of the rule.</summary>
/// <param name="Rule">The rule that was validated.</param>
/// <param name="Levels">One report for each level of <see cref="ValidationLevels"/>, in the order of the flags.</param>
/// <param name="Statistics">The statistics, or <see langword="null"/> when the rule does not compile.</param>
public sealed record ValidationReport(LoadedRule Rule, EquatableArray<ValidationLevelReport> Levels, RuleStatistics? Statistics)
{
    /// <summary>Gets every diagnostic of every level, in the order of the levels.</summary>
    public IEnumerable<Diagnostic> Diagnostics => this.Levels.SelectMany(level => level.Diagnostics);

    /// <summary>Gets the report of one level.</summary>
    /// <param name="level">One flag of <see cref="ValidationLevels"/>.</param>
    /// <returns>The report of the level.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is not one level.</exception>
    public ValidationLevelReport this[ValidationLevels level] =>
        this.Levels.FirstOrDefault(report => report.Level == level)
        ?? throw new ArgumentOutOfRangeException(nameof(level), level, "The value is not one validation level.");

    /// <summary>Gets the exit code of the validation (see <see cref="SeverityPolicy.ExitCodeFor"/>).</summary>
    /// <param name="strict"><see langword="true"/> for strict mode, in which a warning fails too.</param>
    /// <returns>The exit code.</returns>
    public ToolExitCode ExitCode(bool strict = false)
    {
        return SeverityPolicy.ExitCodeFor(this.Diagnostics, strict);
    }
}
