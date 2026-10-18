namespace TruthWeaver.Tool.Core.Tests.Validation;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Tests.TestSupport;
using TruthWeaver.Tool.Core.Validation;
using TruthWeaver.Yaml;

/// <summary>Each validation level passes a good rule, fails a bad one, and can be switched off.</summary>
public sealed class RuleValidatorTests
{
    private const string PredicateSchema = """
        predicates:
          isActive: {}
          hasRole:
            arguments:
              role: string
        """;

    /// <summary>A valid rule text passes every level that applies, and the levels that do not apply say why.</summary>
    [Fact]
    public void Validate_ValidRuleText_PassesEveryApplicableLevel_Test()
    {
        LoadedRule rule = Text("isActive AND hasRole(role: \"editor\")");

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule);

        Assert.True(result.Succeeded);
        ValidationReport report = result.Value;
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.Compile].Status);
        Assert.Equal(ValidationStatus.Skipped, report[ValidationLevels.TreeSchema].Status);
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.Limits].Status);
        Assert.Equal(ValidationStatus.Skipped, report[ValidationLevels.PredicateSchema].Status);
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.Analysis].Status);
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.ConversionCheck].Status);
        Assert.NotNull(report.Statistics);
        Assert.Equal(ToolExitCode.Success, report.ExitCode(strict: true));
    }

    /// <summary>The report has one entry for each level, in the order of the flags.</summary>
    [Fact]
    public void Validate_AnyRule_ReportsEachLevelOnceInFlagOrder_Test()
    {
        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive"));

        Assert.Equal(
            [
                ValidationLevels.Compile,
                ValidationLevels.TreeSchema,
                ValidationLevels.Limits,
                ValidationLevels.PredicateSchema,
                ValidationLevels.Analysis,
                ValidationLevels.ConversionCheck,
            ],
            result.Value!.Levels.Select(level => level.Level)
        );
    }

    /// <summary>A syntax error fails the compile level, and the levels that need a compiled rule are skipped.</summary>
    [Fact]
    public void Validate_SyntaxError_FailsCompileAndSkipsDependentLevels_Test()
    {
        ValidationOptions options = new() { PredicateSchema = Schema(PredicateSchema) };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive AND"), options);

        Assert.False(result.Succeeded);
        ValidationReport report = result.Value!;
        Assert.Equal(ValidationStatus.Failed, report[ValidationLevels.Compile].Status);
        Assert.Equal(DiagnosticCodes.SyntaxError, report[ValidationLevels.Compile].Diagnostics[0].Code);
        Assert.Equal(ValidationStatus.Skipped, report[ValidationLevels.PredicateSchema].Status);
        Assert.Equal(ValidationStatus.Skipped, report[ValidationLevels.Analysis].Status);
        Assert.Equal(ValidationStatus.Skipped, report[ValidationLevels.ConversionCheck].Status);
        Assert.Null(report.Statistics);
        Assert.Equal(ToolExitCode.Failed, report.ExitCode());
    }

    /// <summary>With the compile level off, its diagnostics are not reported.</summary>
    [Fact]
    public void Validate_CompileLevelOff_ReportsNoCompileDiagnostics_Test()
    {
        ValidationOptions options = new() { Levels = ValidationLevels.All & ~ValidationLevels.Compile };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive AND"), options);

        Assert.Equal(ValidationStatus.Off, result.Value![ValidationLevels.Compile].Status);
        Assert.Empty(result.Value[ValidationLevels.Compile].Diagnostics);
    }

    /// <summary>A JSON rule that matches the tree schema passes the tree schema level.</summary>
    [Fact]
    public void Validate_JsonFixture_PassesTreeSchema_Test()
    {
        LoadedRule rule = RuleLoader.LoadFile(Fixtures.Path("access.json")).Value!;

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule);

        Assert.Equal(ValidationStatus.Passed, result.Value![ValidationLevels.TreeSchema].Status);
    }

    /// <summary>A JSON rule with a member that its node does not allow fails the tree schema level at that member.</summary>
    [Fact]
    public void Validate_JsonWithUnknownMember_FailsTreeSchemaAtTheMember_Test()
    {
        const string Json = """{"op": "and", "operands": [{"predicate": "isActive", "extra": 1}, {"predicate": "b"}]}""";
        LoadedRule rule = Load(Json, RuleFormat.Json);

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule);

        ValidationLevelReport level = result.Value![ValidationLevels.TreeSchema];
        Assert.Equal(ValidationStatus.Failed, level.Status);
        Diagnostic finding = Assert.Single(level.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.TreeSchemaViolation, finding.Code);
        Assert.Equal("$.operands[0].extra", finding.Path);
        Assert.Equal("1", Json.Substring(finding.Span.Start, finding.Span.Length));
    }

    /// <summary>A YAML rule whose operator is not known fails the tree schema level at the operator.</summary>
    [Fact]
    public void Validate_YamlWithUnknownOperator_FailsTreeSchemaAtTheOperator_Test()
    {
        const string Yaml = "op: andd\noperands:\n  - predicate: a\n  - predicate: b\n";
        LoadedRule rule = Load(Yaml, RuleFormat.Yaml);

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule);

        Diagnostic finding = Assert.Single(result.Value![ValidationLevels.TreeSchema].Diagnostics);
        Assert.Equal("$.op", finding.Path);
        Assert.Equal("andd", Yaml.Substring(finding.Span.Start, finding.Span.Length));
    }

    /// <summary>With the tree schema level off, a schema violation is not reported by that level.</summary>
    [Fact]
    public void Validate_TreeSchemaLevelOff_ReportsNothingForIt_Test()
    {
        LoadedRule rule = Load("""{"predicate": "a", "extra": 1}""", RuleFormat.Json);
        ValidationOptions options = new() { Levels = ValidationLevels.All & ~ValidationLevels.TreeSchema };

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule, options);

        Assert.Equal(ValidationStatus.Off, result.Value![ValidationLevels.TreeSchema].Status);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == ToolDiagnosticCodes.TreeSchemaViolation);
    }

    /// <summary>
    /// A rule deeper than the depth limit fails the limits level only; the rule is compiled again without the limit, so the
    /// other levels and the statistics still run.
    /// </summary>
    [Fact]
    public void Validate_RuleOverDepthLimit_FailsLimitsAndRunsTheOtherLevels_Test()
    {
        ValidationOptions options = new() { Settings = ToolSettings.Default with { MaxDepth = 2 } };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("NOT (a AND (b OR c))"), options);

        ValidationReport report = result.Value!;
        Assert.Equal(ValidationStatus.Failed, report[ValidationLevels.Limits].Status);
        Assert.NotEmpty(report[ValidationLevels.Limits].Diagnostics);
        Assert.All(report[ValidationLevels.Limits].Diagnostics, d => Assert.Equal(DiagnosticCodes.MaxDepthExceeded, d.Code));
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.Compile].Status);
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.ConversionCheck].Status);
        Assert.Equal(4, report.Statistics!.Depth);
    }

    /// <summary>With the limits level off, a rule over the limits compiles and validates without a limit finding.</summary>
    [Fact]
    public void Validate_LimitsLevelOff_AcceptsRuleOverTheLimits_Test()
    {
        ValidationOptions options = new()
        {
            Levels = ValidationLevels.All & ~ValidationLevels.Limits,
            Settings = ToolSettings.Default with { MaxDepth = 2, MaxNodeCount = 2 },
        };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("NOT (a AND (b OR c))"), options);

        Assert.True(result.Succeeded);
        Assert.Equal(ValidationStatus.Off, result.Value[ValidationLevels.Limits].Status);
    }

    /// <summary>A rule that uses only the declared predicates and arguments passes the predicate schema level.</summary>
    [Fact]
    public void Validate_RuleMatchingPredicateSchema_PassesPredicateSchema_Test()
    {
        ValidationOptions options = new() { PredicateSchema = Schema(PredicateSchema) };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive AND hasRole(role: \"editor\")"), options);

        Assert.Equal(ValidationStatus.Passed, result.Value![ValidationLevels.PredicateSchema].Status);
    }

    /// <summary>
    /// An unknown predicate, a missing argument, an unknown argument and an argument of the wrong kind each fail the
    /// predicate schema level, and the permissive compile level still passes.
    /// </summary>
    [Fact]
    public void Validate_RuleBreakingPredicateSchema_FailsPredicateSchemaWithEachFinding_Test()
    {
        ValidationOptions options = new() { PredicateSchema = Schema(PredicateSchema) };

        ToolResult<ValidationReport> result = RuleValidator.Validate(
            Text("isLocked AND hasRole(level: 3) AND hasRole(role: 4) AND isActive"),
            options
        );

        ValidationReport report = result.Value!;
        Assert.Equal(ValidationStatus.Passed, report[ValidationLevels.Compile].Status);
        Assert.Equal(ValidationStatus.Failed, report[ValidationLevels.PredicateSchema].Status);
        Assert.Equal(
            [
                DiagnosticCodes.UnknownPredicate,
                DiagnosticCodes.MissingArgument,
                DiagnosticCodes.ArgumentTypeMismatch,
                DiagnosticCodes.UnknownArgument,
            ],
            report[ValidationLevels.PredicateSchema].Diagnostics.Select(d => d.Code).Distinct().Order(StringComparer.Ordinal)
        );
    }

    /// <summary>The predicate schema file that <c>truthweaver.json</c> names is read and applied.</summary>
    [Fact]
    public void Validate_SchemaFileFromConfiguration_AppliesTheFile_Test()
    {
        ToolConfiguration file = ToolConfigurationFile.Load(Fixtures.Path("config/truthweaver.json")).Value!;
        ValidationOptions options = new() { Settings = ToolSettings.Resolve(file, null) };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive AND isLocked"), options);

        Diagnostic finding = Assert.Single(result.Value![ValidationLevels.PredicateSchema].Diagnostics);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, finding.Code);
    }

    /// <summary>A predicate schema file that is not usable fails the level, and the exit code says the tool could not work.</summary>
    [Fact]
    public void Validate_UnusableSchemaFile_FailsPredicateSchemaAsUnusable_Test()
    {
        using TemporaryDirectory directory = new();
        string path = directory.File("predicates.yaml");
        File.WriteAllText(path, "predicates:\n  hasRole:\n    arguments:\n      role: text\n");
        ValidationOptions options = new() { Settings = ToolSettings.Default with { SchemaFile = path } };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive"), options);

        ValidationLevelReport level = result.Value![ValidationLevels.PredicateSchema];
        Assert.Equal(ValidationStatus.Failed, level.Status);
        Assert.Equal(ToolDiagnosticCodes.InvalidSchemaFile, Assert.Single(level.Diagnostics).Code);
        Assert.Equal(ToolExitCode.Unusable, result.Value.ExitCode());
    }

    /// <summary>With the predicate schema level off, the schema is not applied.</summary>
    [Fact]
    public void Validate_PredicateSchemaLevelOff_DoesNotApplyTheSchema_Test()
    {
        ValidationOptions options = new()
        {
            Levels = ValidationLevels.All & ~ValidationLevels.PredicateSchema,
            PredicateSchema = Schema(PredicateSchema),
        };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isLocked"), options);

        Assert.True(result.Succeeded);
        Assert.Equal(ValidationStatus.Off, result.Value[ValidationLevels.PredicateSchema].Status);
    }

    /// <summary>A tautology is a warning of the analysis level, which fails the command in strict mode only.</summary>
    [Fact]
    public void Validate_Tautology_AnalysisPassesWithWarnings_Test()
    {
        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("isActive OR True"));

        ValidationReport report = result.Value!;
        Assert.Equal(ValidationStatus.PassedWithWarnings, report[ValidationLevels.Analysis].Status);
        Assert.Contains(report[ValidationLevels.Analysis].Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
        Assert.Equal(ToolExitCode.Success, report.ExitCode());
        Assert.Equal(ToolExitCode.Failed, report.ExitCode(strict: true));
    }

    /// <summary>The analysis level runs the logic and structure lints by default.</summary>
    [Fact]
    public void Validate_DoubleNegation_ReportsTheLintInAnalysis_Test()
    {
        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("NOT NOT isActive"));

        Diagnostic finding = Assert.Single(result.Value![ValidationLevels.Analysis].Diagnostics);
        Assert.Equal(DiagnosticCodes.DoubleNegation, finding.Code);
        Assert.Equal(ValidationStatus.Passed, result.Value[ValidationLevels.Analysis].Status);
    }

    /// <summary>With the analysis level off, no analysis or lint finding is reported.</summary>
    [Fact]
    public void Validate_AnalysisLevelOff_ReportsNoAnalysisFinding_Test()
    {
        ValidationOptions options = new() { Levels = ValidationLevels.All & ~ValidationLevels.Analysis };

        ToolResult<ValidationReport> result = RuleValidator.Validate(Text("NOT NOT (isActive OR True)"), options);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(ValidationStatus.Off, result.Value![ValidationLevels.Analysis].Status);
    }

    /// <summary>A YAML rule with a note converts to each format and back to the same tree and the same note.</summary>
    [Fact]
    public void Validate_YamlFixtureWithNote_PassesConversionCheck_Test()
    {
        LoadedRule rule = RuleLoader.LoadFile(Fixtures.Path("access.yaml")).Value!;

        ToolResult<ValidationReport> result = RuleValidator.Validate(rule);

        Assert.Equal(ValidationStatus.Passed, result.Value![ValidationLevels.ConversionCheck].Status);
    }

    /// <summary>A conversion that gives a different tree fails the conversion check and names the format.</summary>
    [Fact]
    public void ValidateWith_PrinterThatChangesTheTree_FailsConversionCheck_Test()
    {
        ToolResult<ValidationReport> result = RuleValidator.ValidateWith(
            Text("isActive AND isLocked"),
            null,
            (compiled, format) => format == RuleFormat.Json ? """{"predicate": "isActive"}""" : compiled.PrintAs(format)
        );

        ValidationLevelReport level = result.Value![ValidationLevels.ConversionCheck];
        Assert.Equal(ValidationStatus.Failed, level.Status);
        Diagnostic finding = Assert.Single(level.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.ConversionMismatch, finding.Code);
        Assert.Contains("JSON", finding.Message, StringComparison.Ordinal);
    }

    /// <summary>A conversion that drops a note fails the conversion check.</summary>
    [Fact]
    public void ValidateWith_PrinterThatDropsNotes_FailsConversionCheck_Test()
    {
        ToolResult<ValidationReport> result = RuleValidator.ValidateWith(
            Text("// Active users.\nisActive"),
            null,
            (compiled, format) => compiled.PrintAs(format, includeNotes: false)
        );

        Assert.All(
            result.Value![ValidationLevels.ConversionCheck].Diagnostics,
            d => Assert.Contains("notes", d.Message, StringComparison.Ordinal)
        );
        Assert.Equal(ValidationStatus.Failed, result.Value[ValidationLevels.ConversionCheck].Status);
    }

    /// <summary>With the conversion check off, the printer is not used.</summary>
    [Fact]
    public void ValidateWith_ConversionCheckOff_DoesNotConvert_Test()
    {
        ValidationOptions options = new() { Levels = ValidationLevels.All & ~ValidationLevels.ConversionCheck };

        ToolResult<ValidationReport> result = RuleValidator.ValidateWith(
            Text("isActive"),
            options,
            (_, _) => throw new InvalidOperationException("The conversion check is off.")
        );

        Assert.True(result.Succeeded);
        Assert.Equal(ValidationStatus.Off, result.Value[ValidationLevels.ConversionCheck].Status);
    }

    private static LoadedRule Text(string text)
    {
        return Load(text, RuleFormat.Text);
    }

    private static LoadedRule Load(string text, RuleFormat format)
    {
        return RuleLoader.LoadString(text, "rule", new RuleLoadOptions { Format = format }).Value!;
    }

    private static PredicateSchemaFile Schema(string yaml)
    {
        return PredicateSchemaFile.Parse(yaml).Value!;
    }
}
