namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// Breaks <see cref="CompileBenchmarks.Compile"/>'s end-to-end cost down by pipeline stage
/// (k3-hardening ticket 18): Parse, Validate+Build (fused in a single <c>RuleNodeCompiler</c> pass -
/// see its remarks), Analyze (the dual-rail BDD tautology/contradiction analyzer) and the opt-in Lint
/// pass, plus a JSON-vs-YAML parse comparison to attribute the shared <c>TreeFormatReader</c>'s cost.
/// Reaches internal types (<c>RuleNodeCompiler</c>, <c>Analysis.Analyzer</c>, <c>Analysis.Linter</c>)
/// only because <c>TruthWeaver</c>'s <c>AssemblyInfo</c> grants this project
/// <c>InternalsVisibleTo</c> for exactly this purpose.
/// </summary>
[MemoryDiagnoser]
public class CompileStageBenchmarks
{
    private PredicateRegistry<BenchmarkContext> registry = null!;
    private CompilerOptions options = null!;
    private CompilerOptions lintOptions = null!;
    private string ruleJson = null!;
    private string ruleYaml = null!;
    private RuleNode parsedRoot = null!;
    private Expression builtTree = null!;

    /// <summary>Gets or sets the rule size this benchmark case compiles.</summary>
    [Params(RuleSize.Small, RuleSize.Large)]
    public RuleSize Size { get; set; }

    /// <summary>
    /// Builds the registry, options, pre-rendered rule text (JSON and the equivalent YAML) and the
    /// intermediate Parse/Build outputs each later stage needs, all outside the measured operations.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        (int termCount, int groupSize) = this.Size switch
        {
            RuleSize.Small => (10, 2),
            RuleSize.Large => (200, 4),
            _ => throw new NotSupportedException($"Unhandled rule size '{this.Size}'."),
        };

        this.registry = RuleFixtures.BuildRegistry(termCount);
        this.options = new CompilerOptions(MaxAnalysisTerms: Math.Max(20, termCount));
        this.lintOptions = this.options with { Lints = LintRules.All };

        RuleBuilder rule = RuleFixtures.BuildGroupedRule(termCount, groupSize);
        this.ruleJson = rule.ToJson();

        RuleCompiler<BenchmarkContext> compiler = new(this.registry, this.options);
        CompilationResult<BenchmarkContext> compiled = compiler.CompileJson(this.ruleJson);
        this.ruleYaml = compiled.CompiledRule!.PrintYaml();

        (RuleNode? parsed, IReadOnlyList<Diagnostic> _) = JsonTreeParser.Parse(this.ruleJson);
        this.parsedRoot = parsed!;
        (Expression? tree, IReadOnlyList<Diagnostic> _) = RuleNodeCompiler<BenchmarkContext>.Compile(
            this.parsedRoot,
            this.registry,
            this.options
        );
        this.builtTree = tree!;
    }

    /// <summary>
    /// Parses the rule's JSON tree text only (the Parse stage of the JSON front end). Returns
    /// <see cref="object"/> because the parsed <c>RuleNode</c> tree is internal; the reference is
    /// still returned (not boxed) so the parse can't be dead-code-eliminated.
    /// </summary>
    [Benchmark]
    public object? ParseJson()
    {
        return JsonTreeParser.Parse(this.ruleJson).Root;
    }

    /// <summary>
    /// Parses the same rule's YAML tree text. Both front ends share the <c>TreeFormatReader</c> walk;
    /// comparing this against <see cref="ParseJson"/> attributes how much of the Parse stage is the
    /// shared reader versus the JSON- or YAML-specific cursor.
    /// </summary>
    [Benchmark]
    public object? ParseYaml()
    {
        return YamlTreeParser.Parse(this.ruleYaml).Root;
    }

    /// <summary>
    /// Validates and builds the expression tree from the already-parsed raw tree (the fused
    /// Validate + Build stage - <c>RuleNodeCompiler</c> does not separate them).
    /// </summary>
    [Benchmark]
    public Expression? ValidateAndBuild()
    {
        return RuleNodeCompiler<BenchmarkContext>.Compile(this.parsedRoot, this.registry, this.options).Tree;
    }

    /// <summary>Runs the dual-rail BDD tautology/contradiction analyzer alone on the already-built tree.</summary>
    [Benchmark]
    public IReadOnlyList<Diagnostic> Analyze()
    {
        return Analyzer.Analyze(this.builtTree, this.options);
    }

    /// <summary>
    /// Runs every lint rule alone on the already-built tree. <see cref="CompileBenchmarks"/> does not
    /// exercise this stage at all: <see cref="CompilerOptions.Lints"/> defaults to
    /// <see cref="LintRules.None"/>, so a default-options compile skips it entirely.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<Diagnostic> Lint()
    {
        return Linter.Lint(this.builtTree, this.lintOptions);
    }
}
