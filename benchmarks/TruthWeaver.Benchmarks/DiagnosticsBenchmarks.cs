namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Compilation;

/// <summary>
/// Measures <see cref="CompilationResult{TContext}.FormatDiagnostics"/> on a result carrying several
/// errors, with and without the source text (which adds line, column and caret rendering).
/// </summary>
[MemoryDiagnoser]
public class DiagnosticsBenchmarks
{
    private const string Source = "term0 AND unknownA OR (term1 XOR unknownB) AND ATLEAST(9, term0, term1) AND unknownC";

    private CompilationResult<BenchmarkContext> result = null!;

    /// <summary>Compiles a deliberately faulty rule once, outside the measured operations.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        RuleCompiler<BenchmarkContext> compiler = new(RuleFixtures.BuildRegistry(2));
        this.result = compiler.Compile(Source);
        if (this.result.Diagnostics.Count == 0)
        {
            throw new InvalidOperationException("The diagnostics benchmark rule unexpectedly compiled cleanly.");
        }
    }

    /// <summary>Formats the diagnostics with offsets only.</summary>
    /// <returns>The rendered text.</returns>
    [Benchmark]
    public string FormatWithoutSource()
    {
        return this.result.FormatDiagnostics();
    }

    /// <summary>Formats the diagnostics with the source text, adding line, column and the offending line.</summary>
    /// <returns>The rendered text.</returns>
    [Benchmark]
    public string FormatWithSource()
    {
        return this.result.FormatDiagnostics(Source);
    }
}
