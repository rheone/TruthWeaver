namespace TruthWeaver.Tool.Core.Export;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Reports;
using TruthWeaver.Tool.Core.Validation;

/// <summary>
/// A loaded rule, compiled once, with the export options: the input of every artifact. An export of every artifact
/// shares one source, so the rule is compiled and validated only once.
/// </summary>
internal sealed class ArtifactSource
{
    private readonly Lazy<ValidationReport> validation;
    private readonly Lazy<RuleStatistics?> statistics;

    /// <summary>Initializes a new instance of the <see cref="ArtifactSource"/> class and compiles the rule.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="options">The export options.</param>
    public ArtifactSource(LoadedRule rule, ExportOptions options)
    {
        this.Rule = rule;
        this.Options = options;
        CompilationResult<StubAssignment> result = RuleConverter.Compile(rule, options.Settings);
        this.Compiled = result.CompiledRule;
        this.CompileDiagnostics = [.. result.Diagnostics];
        this.validation = new Lazy<ValidationReport>(() =>
            RuleValidator.Validate(rule, new ValidationOptions { Settings = options.Settings }).Value!
        );
        this.statistics = new Lazy<RuleStatistics?>(() =>
            this.Compiled is null ? null : RuleStatistics.Compute(this.Compiled, options.Settings.MaxAnalysisTerms)
        );
    }

    /// <summary>Gets the loaded rule.</summary>
    public LoadedRule Rule { get; }

    /// <summary>Gets the export options.</summary>
    public ExportOptions Options { get; }

    /// <summary>Gets the compiled rule, or <see langword="null"/> when the rule does not compile.</summary>
    public CompiledRule<StubAssignment>? Compiled { get; }

    /// <summary>Gets the diagnostics of the compile.</summary>
    public IReadOnlyList<Diagnostic> CompileDiagnostics { get; }

    /// <summary>Gets the validation report with every level on. The validator always gives a report.</summary>
    public ValidationReport Validation => this.validation.Value;

    /// <summary>Gets the statistics, or <see langword="null"/> when the rule does not compile.</summary>
    public RuleStatistics? Statistics => this.statistics.Value;

    /// <summary>
    /// Makes text from the compiled rule, or gives the compile diagnostics when the rule does not compile. The compile
    /// warnings and information diagnostics are not repeated in a success: the diagnostics artifacts hold them.
    /// </summary>
    /// <param name="print">Prints the compiled rule.</param>
    /// <returns>The text, or the compile diagnostics.</returns>
    public ToolResult<string> Print(Func<CompiledRule<StubAssignment>, string> print)
    {
        return this.Compiled is null
            ? ToolResults.Failure<string>(this.CompileDiagnostics)
            : ToolResults.Success(print(this.Compiled));
    }

    /// <summary>Makes text from the compiled rule with a printer that can refuse, or gives the compile diagnostics.</summary>
    /// <param name="print">Prints the compiled rule, and gives a result of its own.</param>
    /// <returns>The result of <paramref name="print"/>, or the compile diagnostics.</returns>
    public ToolResult<string> PrintOrRefuse(Func<CompiledRule<StubAssignment>, ToolResult<string>> print)
    {
        return this.Compiled is null ? ToolResults.Failure<string>(this.CompileDiagnostics) : print(this.Compiled);
    }
}
