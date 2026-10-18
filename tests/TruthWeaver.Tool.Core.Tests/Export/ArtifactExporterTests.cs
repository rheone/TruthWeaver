namespace TruthWeaver.Tool.Core.Tests.Export;

using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Reports;
using TruthWeaver.Tool.Core.Tests.TestSupport;
using TruthWeaver.Tool.Core.Validation;
using TruthWeaver.Yaml;

/// <summary>Every artifact of the catalog is the library printer's output, and an export of every artifact is deterministic.</summary>
public sealed class ArtifactExporterTests
{
    // Nine distinct terms: over the truth-table cap of eight. (f, t and u are the constants, so they are not used.)
    private const string NineTerms = "a AND b AND c AND d AND e AND g AND h AND i AND j";

    /// <summary>Gets the id of every artifact in the catalog.</summary>
    public static TheoryData<string> ArtifactIds => [.. ArtifactCatalog.All.Select(artifact => artifact.Id)];

    /// <summary>
    /// The text of each artifact equals what the library printer, the statistics writer, the diagnostics writer or the
    /// report gives for the same rule and options. The expected text is made here from the id, independently of the catalog.
    /// </summary>
    /// <param name="id">The artifact id.</param>
    [Theory]
    [MemberData(nameof(ArtifactIds))]
    public void Render_EachCatalogArtifact_EqualsTheLibraryPrinterOutput_Test(string id)
    {
        LoadedRule loaded = Load();

        ToolResult<string> rendered = ArtifactExporter.Render(loaded, id);

        Assert.True(rendered.Succeeded, string.Join("; ", rendered.Diagnostics.Select(d => d.Message)));
        Assert.Equal(Expected(id, loaded), rendered.Value);
    }

    /// <summary>The catalog has the equation in every dialect and constant style, the LaTeX envelopes, and every truth-table format.</summary>
    [Fact]
    public void All_Catalog_HasEveryEquationAndTruthTableVariant_Test()
    {
        IReadOnlyList<ArtifactDefinition> all = ArtifactCatalog.All;

        // Seven dialects plus the two LaTeX envelopes, each in three constant styles.
        Assert.Equal(27, all.Count(artifact => artifact.Id.StartsWith("equation-", StringComparison.Ordinal)));
        Assert.Equal(5, all.Count(artifact => artifact.Id.StartsWith("truth-table-", StringComparison.Ordinal)));
        Assert.Equal(all.Count, all.Select(artifact => artifact.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(all.Count, all.Select(artifact => artifact.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>The lookup helpers map printer options to the catalog ids.</summary>
    [Fact]
    public void Equation_DialectConstantsAndWrap_FindsTheCatalogArtifact_Test()
    {
        ArtifactDefinition mathJax = ArtifactCatalog.Equation(
            EquationDialect.LaTeX,
            ConstantStyle.Word,
            LatexWrapMode.MathJaxSafe
        );
        ArtifactDefinition csv = ArtifactCatalog.TruthTable(TruthTableFormat.Csv);

        Assert.Equal("equation-latex-mathjax-word", mathJax.Id);
        Assert.Equal("equation-latex-mathjax-word.md", mathJax.FileName);
        Assert.Equal("truth-table-csv", csv.Id);
        Assert.True(ArtifactCatalog.TryFind("truth-table-csv", out ArtifactDefinition? found));
        Assert.Same(csv, found);
    }

    /// <summary>An id that is not in the catalog is a <c>TWT0018</c> error that lists the ids.</summary>
    [Fact]
    public void Render_UnknownId_ReportsUnknownArtifact_Test()
    {
        ToolResult<string> rendered = ArtifactExporter.Render(Load(), "equation-klingon");

        Assert.False(rendered.Succeeded);
        Diagnostic error = Assert.Single(rendered.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.UnknownArtifact, error.Code);
        Assert.Contains("equation-latex-letters", error.Message, StringComparison.Ordinal);
        Assert.Equal(ToolExitCode.Unusable, SeverityPolicy.ExitCodeFor(rendered.Diagnostics, strict: false));
    }

    /// <summary>A rule that does not compile gives no printed artifact, but its diagnostics artifact shows why.</summary>
    [Fact]
    public void Render_RuleThatDoesNotCompile_GivesCompileDiagnosticsAndStillExportsTheDiagnostics_Test()
    {
        LoadedRule broken = RuleLoader.LoadString("isActive AND (", "broken.rule").Value!;

        ToolResult<string> json = ArtifactExporter.Render(broken, "json");
        ToolResult<string> diagnostics = ArtifactExporter.Render(broken, "diagnostics");

        Assert.False(json.Succeeded);
        Assert.Contains(json.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.True(diagnostics.Succeeded);
        Assert.Contains(DiagnosticCodes.SyntaxError, diagnostics.Value, StringComparison.Ordinal);
    }

    /// <summary>A truth table over the cap is refused with <c>TRE0035</c>, and printed with a <c>TRE0034</c> warning when the export allows it.</summary>
    [Fact]
    public void Render_TruthTableOverTheCap_IsRefusedUnlessTheExportAllowsIt_Test()
    {
        LoadedRule large = RuleLoader.LoadString(NineTerms).Value!;

        ToolResult<string> refused = ArtifactExporter.Render(large, "truth-table-csv");
        ToolResult<string> allowed = ArtifactExporter.Render(
            large,
            "truth-table-csv",
            new ExportOptions { AllowLargerTruthTable = true }
        );

        Assert.False(refused.Succeeded);
        Assert.Contains(refused.Diagnostics, d => d.Code == DiagnosticCodes.TruthTableTooLarge);
        Assert.True(allowed.Succeeded);
        Assert.Contains(allowed.Diagnostics, d => d.Code == DiagnosticCodes.TruthTableCapRaised);
    }

    /// <summary>An export to a writer writes the artifact with line feeds and one final newline.</summary>
    [Fact]
    public void Export_ToWriter_WritesTheNormalizedArtifact_Test()
    {
        LoadedRule loaded = Load();
        using StringWriter writer = new();

        ToolResult<string> result = ArtifactExporter.Export(
            loaded,
            ArtifactCatalog.TruthTable(TruthTableFormat.Markdown),
            writer
        );

        Assert.True(result.Succeeded);
        Assert.Equal(OutputWriter.Normalize(Expected("truth-table-markdown", loaded)), writer.ToString());
    }

    /// <summary>An export to a file writes the bytes of <see cref="OutputWriter.Encode"/> and creates the directory.</summary>
    [Fact]
    public void Export_ToFile_WritesTheEncodedArtifact_Test()
    {
        using TemporaryDirectory directory = new();
        LoadedRule loaded = Load();
        string path = directory.File("nested/rule.dot");

        ToolResult<string> result = ArtifactExporter.Export(loaded, ArtifactCatalog.All.Single(a => a.Id == "dot"), path);

        Assert.True(result.Succeeded);
        Assert.Equal(OutputWriter.Encode(Expected("dot", loaded)), File.ReadAllBytes(path));
    }

    /// <summary>An export of every artifact writes one file for each artifact and a manifest that lists them with the source hash.</summary>
    [Fact]
    public void ExportAll_Rule_WritesEveryArtifactAndTheManifest_Test()
    {
        using TemporaryDirectory directory = new();
        LoadedRule loaded = Load();

        ToolResult<ExportManifest> result = ArtifactExporter.ExportAll(loaded, directory.FullPath);

        Assert.True(result.Succeeded);
        ExportManifest manifest = result.Value;
        Assert.Equal(ArtifactCatalog.All.Select(a => a.Id), manifest.Artifacts.Select(a => a.Id));
        Assert.Empty(manifest.Skipped);
        Assert.Equal(ArtifactExporter.HashOf(loaded), manifest.SourceHash);
        Assert.StartsWith("sha256:", manifest.SourceHash, StringComparison.Ordinal);
        Assert.Equal(71, manifest.SourceHash.Length);
        foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
        {
            Assert.Equal(
                OutputWriter.Encode(Expected(artifact.Id, loaded)),
                File.ReadAllBytes(directory.File(artifact.FileName))
            );
        }

        Assert.Equal(OutputWriter.Encode(manifest.ToJson()), File.ReadAllBytes(directory.File(ExportManifest.FileName)));
    }

    /// <summary>Two exports of the same rule give byte-identical folders, so the output can be committed and checked.</summary>
    [Fact]
    public void ExportAll_RunTwice_GivesIdenticalFolders_Test()
    {
        using TemporaryDirectory first = new();
        using TemporaryDirectory second = new();

        Assert.True(ArtifactExporter.ExportAll(Load(), first.FullPath).Succeeded);
        Assert.True(ArtifactExporter.ExportAll(Load(), second.FullPath).Succeeded);

        string[] names = [.. Directory.GetFiles(first.FullPath).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];
        Assert.Equal(names, Directory.GetFiles(second.FullPath).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Equal(ArtifactCatalog.All.Count + 1, names.Length);
        foreach (string name in names)
        {
            Assert.Equal(File.ReadAllBytes(first.File(name)), File.ReadAllBytes(second.File(name)));
        }
    }

    /// <summary>After an export, every file of <see cref="ArtifactExporter.RenderAll"/> checks as identical, as <c>--check</c> needs.</summary>
    [Fact]
    public void RenderAll_AfterExportAll_EveryFileChecksIdentical_Test()
    {
        using TemporaryDirectory directory = new();
        LoadedRule loaded = Load();
        Assert.True(ArtifactExporter.ExportAll(loaded, directory.FullPath).Succeeded);

        ExportBundle bundle = ArtifactExporter.RenderAll(loaded).Value!;

        Assert.Equal(ExportManifest.FileName, bundle.Files[^1].FileName);
        Assert.All(
            bundle.Files,
            file =>
                Assert.Equal(
                    OutputCheckStatus.Identical,
                    OutputWriter.Check(directory.File(file.FileName), file.Text).Value!.Status
                )
        );
    }

    /// <summary>The manifest names the source, every file with its format and options, and the skipped artifacts, and no absolute path.</summary>
    [Fact]
    public void ToJson_Manifest_ListsSourceFilesOptionsAndSkipped_Test()
    {
        LoadedRule large = RuleLoader.LoadString(NineTerms).Value!;

        ExportManifest manifest = ArtifactExporter.RenderAll(large).Value!.Manifest;
        string json = manifest.ToJson();

        Assert.Contains("\"format\": \"text\"", json, StringComparison.Ordinal);
        Assert.Contains($"\"hash\": \"{ArtifactExporter.HashOf(large)}\"", json, StringComparison.Ordinal);
        Assert.Contains(
            """
                {
                  "id": "equation-latex-mathjax-letters",
                  "file": "equation-latex-mathjax-letters.md",
                  "format": "markdown",
                  "options": {
                    "constants": "letters",
                    "dialect": "latex",
                    "latexWrap": "mathjax"
                  }
                }
            """.ReplaceLineEndings("\n"),
            json,
            StringComparison.Ordinal
        );
        Assert.Contains("\"id\": \"truth-table-markdown\",\n      \"reason\": ", json, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetTempPath(), json, StringComparison.Ordinal);
    }

    /// <summary>
    /// An export of a rule over the truth-table cap leaves the truth tables out with a <c>TWT0015</c> warning each and
    /// lists them as skipped; the export still succeeds.
    /// </summary>
    [Fact]
    public void ExportAll_RuleOverTheCap_SkipsTheTruthTablesWithWarnings_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<ExportManifest> result = ArtifactExporter.ExportAll(
            RuleLoader.LoadString(NineTerms).Value!,
            directory.FullPath
        );

        Assert.True(result.Succeeded);
        Assert.Equal(
            ArtifactCatalog.All.Where(a => a.Id.StartsWith("truth-table-", StringComparison.Ordinal)).Select(a => a.Id),
            result.Value.Skipped.Select(s => s.Id)
        );
        Assert.Equal(5, result.Diagnostics.Count(d => d.Code == ToolDiagnosticCodes.ArtifactSkipped));
        Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
        Assert.False(File.Exists(directory.File("truth-table-csv.csv")));
    }

    /// <summary>An export of a rule that does not compile writes nothing and gives the compile diagnostics.</summary>
    [Fact]
    public void ExportAll_RuleThatDoesNotCompile_WritesNothing_Test()
    {
        using TemporaryDirectory directory = new();

        ToolResult<ExportManifest> result = ArtifactExporter.ExportAll(
            RuleLoader.LoadString("isActive AND (").Value!,
            directory.FullPath
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Empty(Directory.GetFiles(directory.FullPath));
    }

    /// <summary>The source hash ignores the line endings, so a CRLF checkout gives the same manifest.</summary>
    [Fact]
    public void HashOf_CrlfAndLfText_IsTheSame_Test()
    {
        LoadedRule lf = RuleLoader.LoadString("isActive\nAND isLocked").Value!;
        LoadedRule crlf = RuleLoader.LoadString("isActive\r\nAND isLocked").Value!;
        LoadedRule other = RuleLoader.LoadString("isActive\nOR isLocked").Value!;

        Assert.Equal(ArtifactExporter.HashOf(lf), ArtifactExporter.HashOf(crlf));
        Assert.NotEqual(ArtifactExporter.HashOf(lf), ArtifactExporter.HashOf(other));
    }

    private static LoadedRule Load()
    {
        return RuleLoader.LoadFile(Fixtures.Path("export.rule")).Value!;
    }

    // The expected text of an artifact, made from its id with the public printers.
    private static string Expected(string id, LoadedRule loaded)
    {
        CompiledRule<StubAssignment> rule = RuleConverter.Compile(loaded).CompiledRule!;
        string[] parts = id.Split('-');
        return id switch
        {
            "rule-text" => rule.PrintRuleText(),
            "rule-text-symbolic" => rule.PrintRuleText(GroupingStyle.Parentheses, OperatorStyle.Symbolic),
            "rule-text-pretty" => rule.PrintRuleText(new RuleTextOptions { Width = 80 }),
            "json" => rule.PrintJson(),
            "yaml" => rule.PrintYaml(),
            "tree" => rule.PrintPlainText(),
            "mermaid" => rule.PrintMermaid(),
            "dot" => rule.PrintDot(),
            "html" => rule.PrintHtml(),
            "statistics" => ValidationReportWriter.FormatStatistics(RuleStatistics.Compute(rule), DiagnosticOutputFormat.Json),
            "statistics-text" => ValidationReportWriter.FormatStatistics(
                RuleStatistics.Compute(rule),
                DiagnosticOutputFormat.Text
            ),
            "diagnostics" => Diagnostics(loaded, DiagnosticOutputFormat.Text),
            "diagnostics-json" => Diagnostics(loaded, DiagnosticOutputFormat.Json),
            "diagnostics-sarif" => Diagnostics(loaded, DiagnosticOutputFormat.Sarif),
            "report" => RuleReport.Format(loaded).Value!,
            _ when id.StartsWith("truth-table-", StringComparison.Ordinal) => rule.PrintTruthTable(
                new TruthTableOptions { Format = Enum.Parse<TruthTableFormat>(parts[^1], ignoreCase: true) }
            ),
            _ when id.StartsWith("equation-", StringComparison.Ordinal) => rule.PrintEquation(EquationOptionsOf(parts)),
            _ => throw new InvalidOperationException($"The test has no expected text for the artifact '{id}'."),
        };
    }

    // equation-<dialect>[-doubledollar|-mathjax]-<constants>
    private static EquationOptions EquationOptionsOf(string[] parts)
    {
        EquationDialect dialect = parts[1] switch
        {
            "unicode" => EquationDialect.Unicode,
            "latex" => EquationDialect.LaTeX,
            "asciimath" => EquationDialect.AsciiMath,
            "cstyle" => EquationDialect.CStyle,
            "mathml" => EquationDialect.MathML,
            "typst" => EquationDialect.Typst,
            "ascii" => EquationDialect.Ascii,
            _ => throw new InvalidOperationException("Unknown dialect " + parts[1]),
        };
        LatexWrapMode wrap =
            parts.Length == 4
                ? parts[2] switch
                {
                    "doubledollar" => LatexWrapMode.DoubleDollar,
                    "mathjax" => LatexWrapMode.MathJaxSafe,
                    _ => throw new InvalidOperationException("Unknown envelope " + parts[2]),
                }
                : LatexWrapMode.None;
        ConstantStyle constants = parts[^1] switch
        {
            "letters" => ConstantStyle.Letters,
            "word" => ConstantStyle.Word,
            "logicsymbols" => ConstantStyle.LogicSymbols,
            _ => throw new InvalidOperationException("Unknown constants " + parts[^1]),
        };
        return new EquationOptions
        {
            Dialect = dialect,
            LatexWrap = wrap,
            Constants = new ConstantFormat { Style = constants },
        };
    }

    private static string Diagnostics(LoadedRule loaded, DiagnosticOutputFormat format)
    {
        ValidationReport report = RuleValidator.Validate(loaded).Value!;
        return DiagnosticWriter.Format([DiagnosticDocument.For(loaded, report.Diagnostics)], format);
    }
}
