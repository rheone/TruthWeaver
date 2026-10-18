namespace TruthWeaver.Tool.Core.Validation;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Reports;
using TruthWeaver.Yaml;

/// <summary>
/// Validates a loaded rule in levels, and reports each level separately with the statistics of the rule. Every level is
/// on by default and <see cref="ValidationOptions.Levels"/> switches each one off. See <see cref="ValidationLevels"/> for
/// what each level checks.
/// </summary>
/// <remarks>
/// <para>The validator compiles the rule once with the permissive compiler of the tool (see
/// <see cref="RuleConverter.CreateCompiler"/>) and divides the compile diagnostics between the levels: <c>TRE0009</c> and
/// <c>TRE0010</c> go to <see cref="ValidationLevels.Limits"/>; the analysis and lint codes go to
/// <see cref="ValidationLevels.Analysis"/>; every other code goes to <see cref="ValidationLevels.Compile"/>. When the rule
/// fails only a limit, the validator compiles it again without the depth and node limits, so the other levels and the
/// statistics still run. With the limits level off, the validator does not apply the depth and node limits at all, and the
/// deep-nesting lint (<c>TRE0028</c>) does not report.</para>
/// <para>A level that needs a compiled rule is <see cref="ValidationStatus.Skipped"/> when the rule does not compile. The
/// tree schema level is skipped for rule text, and the predicate schema level is skipped when no schema file is set.</para>
/// </remarks>
public static class RuleValidator
{
    // Large enough for any rule that a person writes, small enough that no limit arithmetic in the compiler overflows.
    private const int UnlimitedSize = 1 << 20;

    private static readonly HashSet<string> LimitCodes =
    [
        DiagnosticCodes.MaxDepthExceeded,
        DiagnosticCodes.MaxNodeCountExceeded,
    ];

    private static readonly HashSet<string> AnalysisCodes =
    [
        DiagnosticCodes.AnalysisSkippedTooManyTerms,
        DiagnosticCodes.StructuralTautology,
        DiagnosticCodes.StructuralContradiction,
        DiagnosticCodes.RedundantInspection,
        DiagnosticCodes.RedundantCoalesce,
        DiagnosticCodes.ConstantIfCondition,
        DiagnosticCodes.IdenticalIfBranches,
        DiagnosticCodes.VacuousCardinality,
        DiagnosticCodes.DuplicateOperands,
        DiagnosticCodes.DoubleNegation,
        DiagnosticCodes.DeepNesting,
        DiagnosticCodes.WideChain,
        DiagnosticCodes.NotCanonical,
    ];

    // The findings of a compile against the predicate schema file that the permissive compile cannot have.
    private static readonly HashSet<string> PredicateSchemaCodes =
    [
        DiagnosticCodes.UnknownPredicate,
        DiagnosticCodes.MissingArgument,
        DiagnosticCodes.ArgumentTypeMismatch,
        DiagnosticCodes.UnknownArgument,
    ];

    private static readonly RuleFormat[] ConversionFormats = [RuleFormat.Text, RuleFormat.Json, RuleFormat.Yaml];

    /// <summary>Validates a loaded rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ValidationOptions.Default"/>.</param>
    /// <returns>
    /// The report, always present, and every diagnostic of every level. <see cref="ToolResult{T}.Succeeded"/> is
    /// <see langword="false"/> when a level found an error. Use <see cref="ValidationReport.ExitCode"/> for the exit code.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static ToolResult<ValidationReport> Validate(LoadedRule rule, ValidationOptions? options = null)
    {
        return ValidateWith(rule, options, static (compiled, format) => compiled.PrintAs(format));
    }

    /// <summary>Validates a loaded rule, with the printer that the conversion self-check uses as a parameter, for tests.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="ValidationOptions.Default"/>.</param>
    /// <param name="print">Prints a compiled rule in a format.</param>
    /// <returns>The report and every diagnostic.</returns>
    internal static ToolResult<ValidationReport> ValidateWith(
        LoadedRule rule,
        ValidationOptions? options,
        Func<CompiledRule<StubAssignment>, RuleFormat, string> print
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        options ??= ValidationOptions.Default;

        CompilerOptions limited = options.Settings.ToCompilerOptions() with
        {
            Lints = options.Levels.HasFlag(ValidationLevels.Analysis) ? options.Lints : LintRules.None,
        };
        CompilerOptions unlimited = limited with { MaxDepth = UnlimitedSize, MaxNodeCount = UnlimitedSize };

        CompilationResult<StubAssignment> first = Compile(
            rule,
            options.Levels.HasFlag(ValidationLevels.Limits) ? limited : unlimited
        );
        CompiledRule<StubAssignment>? compiled = first.CompiledRule;
        List<Diagnostic> analysis = [.. first.Diagnostics.Where(d => AnalysisCodes.Contains(d.Code))];
        bool onlyLimitErrors =
            first.Diagnostics.Any(d => LimitCodes.Contains(d.Code))
            && first.Diagnostics.Where(IsError).All(d => LimitCodes.Contains(d.Code));
        if (compiled is null && onlyLimitErrors)
        {
            // The rule is valid but too large: compile it again so the other levels and the statistics still run.
            CompilationResult<StubAssignment> second = Compile(rule, unlimited);
            compiled = second.CompiledRule;
            analysis = [.. second.Diagnostics.Where(d => AnalysisCodes.Contains(d.Code))];
        }

        RuleStatistics? statistics = compiled is null
            ? null
            : RuleStatistics.Compute(compiled, options.Settings.MaxAnalysisTerms);

        ValidationLevelReport[] levels =
        [
            Level(
                options,
                ValidationLevels.Compile,
                () =>
                    ValidationLevelReport.Ran(
                        ValidationLevels.Compile,
                        first.Diagnostics.Where(d => !LimitCodes.Contains(d.Code) && !AnalysisCodes.Contains(d.Code))
                    )
            ),
            Level(options, ValidationLevels.TreeSchema, () => TreeSchemaLevel(rule)),
            Level(
                options,
                ValidationLevels.Limits,
                () =>
                    ValidationLevelReport.Ran(
                        ValidationLevels.Limits,
                        first.Diagnostics.Where(d => LimitCodes.Contains(d.Code))
                    )
            ),
            Level(
                options,
                ValidationLevels.PredicateSchema,
                () => PredicateSchemaLevel(rule, compiled, statistics, options, unlimited)
            ),
            Level(
                options,
                ValidationLevels.Analysis,
                () =>
                    compiled is null
                        ? NotCompiled(ValidationLevels.Analysis)
                        : ValidationLevelReport.Ran(ValidationLevels.Analysis, analysis)
            ),
            Level(
                options,
                ValidationLevels.ConversionCheck,
                () =>
                    compiled is null
                        ? NotCompiled(ValidationLevels.ConversionCheck)
                        : ValidationLevelReport.Ran(
                            ValidationLevels.ConversionCheck,
                            CheckConversions(compiled, Compiler(unlimited with { Lints = LintRules.None }), print)
                        )
            ),
        ];

        ValidationReport report = new(rule, new EquatableArray<ValidationLevelReport>(levels), statistics);
        return new ToolResult<ValidationReport>(report, new EquatableArray<Diagnostic>(report.Diagnostics));
    }

    private static ValidationLevelReport Level(
        ValidationOptions options,
        ValidationLevels level,
        Func<ValidationLevelReport> run
    )
    {
        return options.Levels.HasFlag(level) ? run() : ValidationLevelReport.SwitchedOff(level);
    }

    private static ValidationLevelReport TreeSchemaLevel(LoadedRule rule)
    {
        return rule.Format == RuleFormat.Text
            ? ValidationLevelReport.Skip(
                ValidationLevels.TreeSchema,
                "The rule is rule text. The tree schema applies to JSON and YAML rules."
            )
            : ValidationLevelReport.Ran(ValidationLevels.TreeSchema, TreeSchemaValidator.Validate(rule.Text, rule.Format));
    }

    private static ValidationLevelReport PredicateSchemaLevel(
        LoadedRule rule,
        CompiledRule<StubAssignment>? compiled,
        RuleStatistics? statistics,
        ValidationOptions options,
        CompilerOptions unlimited
    )
    {
        PredicateSchemaFile? schema = options.PredicateSchema;
        if (schema is null && options.Settings.SchemaFile is { } path)
        {
            ToolResult<PredicateSchemaFile> loaded = PredicateSchemaFile.Load(path);
            if (!loaded.TryGetValue(out schema, out EquatableArray<Diagnostic> diagnostics))
            {
                return ValidationLevelReport.Ran(ValidationLevels.PredicateSchema, diagnostics);
            }
        }

        if (schema is null)
        {
            return ValidationLevelReport.Skip(ValidationLevels.PredicateSchema, "No predicate schema file is set.");
        }

        if (compiled is null || statistics is null)
        {
            return NotCompiled(ValidationLevels.PredicateSchema);
        }

        // Declare every data source the rule names: the permissive compile already accepted them, and this level checks
        // only the predicates and their arguments.
        DataSourceDeclarations sources = [];
        foreach (string source in statistics.DataSources)
        {
            sources.Add(source);
        }

        RuleCompiler<StubAssignment> strict = new(
            schema.Registry,
            unlimited with
            {
                Mode = CompilationMode.Strict,
                Lints = LintRules.None,
                DataSources = sources,
            }
        );
        CompilationResult<StubAssignment> result = strict.CompileAs(rule.Text, rule.Format);
        return ValidationLevelReport.Ran(
            ValidationLevels.PredicateSchema,
            result.Diagnostics.Where(d => PredicateSchemaCodes.Contains(d.Code))
        );
    }

    /// <summary>
    /// Prints the rule as rule text, JSON and YAML and compiles each one back. Each one must compile to the same tree,
    /// compared by the canonical text, with the same notes.
    /// </summary>
    private static List<Diagnostic> CheckConversions(
        CompiledRule<StubAssignment> rule,
        RuleCompiler<StubAssignment> compiler,
        Func<CompiledRule<StubAssignment>, RuleFormat, string> print
    )
    {
        List<Diagnostic> diagnostics = [];
        foreach (RuleFormat format in ConversionFormats)
        {
            CompilationResult<StubAssignment> back = compiler.CompileAs(print(rule, format), format);
            string? problem = null;
            if (!back.TryGetRule(out CompiledRule<StubAssignment>? converted, out EquatableArray<Diagnostic> errors))
            {
                problem = "does not compile: " + errors.First(IsError).Message;
            }
            else if (!string.Equals(converted.CanonicalText, rule.CanonicalText, StringComparison.Ordinal))
            {
                problem = $"compiles to a different tree: {converted.CanonicalText}";
            }
            else if (!NotesEqual(converted.Notes, rule.Notes))
            {
                problem = "does not keep the notes";
            }

            if (problem is not null)
            {
                diagnostics.Add(
                    ToolResults.Error(
                        ToolDiagnosticCodes.ConversionMismatch,
                        $"The rule converted to {FormatName(format)} {problem}."
                    )
                );
            }
        }

        return diagnostics;
    }

    private static bool NotesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
    {
        return left.Count == right.Count
            && left.All(note =>
                right.TryGetValue(note.Key, out string? other) && string.Equals(note.Value, other, StringComparison.Ordinal)
            );
    }

    private static CompilationResult<StubAssignment> Compile(LoadedRule rule, CompilerOptions options)
    {
        return Compiler(options).CompileAs(rule.Text, rule.Format);
    }

    private static RuleCompiler<StubAssignment> Compiler(CompilerOptions options)
    {
        return new RuleCompiler<StubAssignment>(PredicateRegistry<StubAssignment>.CreateBuilder().Build(), options);
    }

    private static ValidationLevelReport NotCompiled(ValidationLevels level)
    {
        return ValidationLevelReport.Skip(level, "The rule does not compile.");
    }

    private static bool IsError(Diagnostic diagnostic)
    {
        return diagnostic.Severity == DiagnosticSeverity.Error;
    }

    private static string FormatName(RuleFormat format)
    {
        return format switch
        {
            RuleFormat.Json => "JSON",
            RuleFormat.Yaml => "YAML",
            _ => "rule text",
        };
    }
}
