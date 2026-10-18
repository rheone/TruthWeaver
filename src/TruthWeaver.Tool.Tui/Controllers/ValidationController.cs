namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Validation;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The logic behind the validation panel: it runs every level of the tool core validation on the editor text and lays the
/// result out as lines, one for each level, one for each diagnostic under its level, and the statistics. A diagnostic line
/// carries the span of the diagnostic, so Enter on it can move the editor cursor there. It has no view.
/// </summary>
internal sealed class ValidationController
{
    /// <summary>The line before a report exists.</summary>
    public const string NotRun = "Not run yet. Press r in this panel, or F7, to validate every level.";

    private static readonly ConditionalWeakTable<Workbench, ValidationController> Controllers = [];

    private readonly Workbench workbench;

    /// <summary>Initializes a new instance of the <see cref="ValidationController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public ValidationController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
    }

    /// <summary>Occurs after a run or a change of the strict mode.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets a value indicating whether strict mode is on. In strict mode a warning fails the validation too.</summary>
    public bool Strict { get; private set; }

    /// <summary>Gets the report of the last run, or <see langword="null"/> before the first run.</summary>
    public ValidationReport? Report { get; private set; }

    /// <summary>Gets the lines of the panel.</summary>
    public IReadOnlyList<ValidationLine> Lines =>
        this.Report is null ? [new ValidationLine(NotRun, null)] : this.BuildLines(this.Report);

    /// <summary>Gets the controller of a workbench. The keys and the panel use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static ValidationController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new ValidationController(key));
    }

    /// <summary>Validates the editor text on every level.</summary>
    /// <param name="announce"><see langword="true"/> to report the outcome on the status line; <see langword="false"/> for a run that follows the typing.</param>
    /// <returns>The report.</returns>
    public ValidationReport Run(bool announce = true)
    {
        RuleDocument document = this.workbench.Document;
        LoadedRule rule = RuleLoader
            .LoadString(document.Text, "<editor>", new RuleLoadOptions { Format = document.Format })
            .Value!;
        ValidationReport report = RuleValidator
            .Validate(rule, new ValidationOptions { Settings = this.workbench.Settings })
            .Value!;
        this.Report = report;
        if (announce)
        {
            this.workbench.SetStatus(Summary(report, this.Strict));
        }

        this.Changed?.Invoke(this, EventArgs.Empty);
        return report;
    }

    /// <summary>Turns strict mode on or off and shows the outcome again.</summary>
    public void ToggleStrict()
    {
        this.Strict = !this.Strict;
        if (this.Report is { } report)
        {
            this.workbench.SetStatus(Summary(report, this.Strict));
        }
        else
        {
            this.workbench.SetStatus($"Strict mode {(this.Strict ? "on" : "off")}.");
        }

        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Moves the editor cursor to the span of a diagnostic line.</summary>
    /// <param name="line">The line.</param>
    /// <returns><see langword="true"/> when the line has a span.</returns>
    public bool Select(ValidationLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.Span is not { } span)
        {
            return false;
        }

        this.workbench.RequestSpan(span);
        return true;
    }

    private static string Summary(ValidationReport report, bool strict)
    {
        int errors = report.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        int warnings = report.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        string outcome = report.ExitCode(strict) == ToolExitCode.Success ? "passed" : "failed";
        return $"Validation {outcome}{(strict ? " (strict)" : string.Empty)}: {errors} error{(errors == 1 ? string.Empty : "s")}, {warnings} warning{(warnings == 1 ? string.Empty : "s")}.";
    }

    private static string StatusWord(ValidationStatus status)
    {
        return status switch
        {
            ValidationStatus.Passed => "passed",
            ValidationStatus.PassedWithWarnings => "passed with warnings",
            ValidationStatus.Failed => "FAILED",
            ValidationStatus.Skipped => "skipped",
            _ => "off",
        };
    }

    private static string Describe(string text, Diagnostic diagnostic)
    {
        string severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };
        string location =
            diagnostic.Span == SourceSpan.None
                ? string.Empty
                : $" {diagnostic.Span.GetLocation(text).Line}:{diagnostic.Span.GetLocation(text).Column}";
        return $"    {severity} {diagnostic.Code}{location}: {diagnostic.Message}";
    }

    private List<ValidationLine> BuildLines(ValidationReport report)
    {
        string text = report.Rule.Text;
        List<ValidationLine> lines =
        [
            new(
                $"Validation ({(this.Strict ? "strict: warnings fail" : "warnings pass")}): {Summary(report, this.Strict)}",
                null
            ),
        ];
        foreach (ValidationLevelReport level in report.Levels)
        {
            string reason = level.Reason is null ? string.Empty : $" - {level.Reason}";
            lines.Add(new ValidationLine($"[{StatusWord(level.Status)}] {level.Level}{reason}", null));
            foreach (Diagnostic diagnostic in level.Diagnostics)
            {
                lines.Add(
                    new ValidationLine(Describe(text, diagnostic), diagnostic.Span == SourceSpan.None ? null : diagnostic.Span)
                );
            }
        }

        lines.Add(new ValidationLine("Statistics:", null));
        if (report.Statistics is { } statistics)
        {
            lines.AddRange(
                ValidationReportWriter
                    .FormatStatistics(statistics, DiagnosticOutputFormat.Text)
                    .Split('\n')
                    .Select(line => new ValidationLine("  " + line, null))
            );
        }
        else
        {
            lines.Add(new ValidationLine("  None: the rule does not compile.", null));
        }

        return lines;
    }
}
