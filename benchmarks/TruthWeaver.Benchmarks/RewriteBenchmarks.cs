namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Measures each <see cref="CompiledRule{TContext}"/> rewrite (expansions to primitives, NAND and NOR;
/// compression to derived operators; canonicalisation; simplification) against one rule that uses
/// every operator family the rewrites have to handle, so each rewrite's cost is comparable.
/// </summary>
[MemoryDiagnoser]
public class RewriteBenchmarks
{
    private CompiledRule<BenchmarkContext> rule = null!;

    /// <summary>Compiles the shared rewrite input once, outside the measured operations.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        PredicateRegistry<BenchmarkContext> registry = RuleFixtures.BuildRegistry(8);
        RuleBuilder[] t = [.. Enumerable.Range(0, 8).Select(i => RuleBuilder.Predicate($"term{i}"))];
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.Xor(t[0], t[1]),
            RuleBuilder.Implies(t[2], t[3]),
            RuleBuilder.Parity(t[0], t[1], t[2], t[3]),
            RuleBuilder.AtLeast(2, t[4], t[5], t[6], t[7]),
            RuleBuilder.ExactlyOne(t[0], t[4], t[7]),
            RuleBuilder.If(t[1], t[5], RuleBuilder.Not(RuleBuilder.Not(t[6]))),
            RuleBuilder.Or(t[0], t[0], RuleBuilder.And(t[1], t[1]))
        );
        this.rule =
            builder.Compile(new RuleCompiler<BenchmarkContext>(registry)).CompiledRule
            ?? throw new InvalidOperationException("The rewrite benchmark rule failed to compile.");
    }

    /// <summary>Expands every derived operator to <c>AND</c>/<c>OR</c>/<c>NOT</c>.</summary>
    /// <returns>The rewrite result, so the work cannot be dead-code-eliminated.</returns>
    [Benchmark]
    public CompilationResult<BenchmarkContext> ExpandToPrimitives()
    {
        return this.rule.ExpandToPrimitives();
    }

    /// <summary>Expands to NAND-only form.</summary>
    /// <returns>The rewrite result.</returns>
    [Benchmark]
    public CompilationResult<BenchmarkContext> ExpandToNand()
    {
        return this.rule.ExpandToNand();
    }

    /// <summary>Expands to NOR-only form.</summary>
    /// <returns>The rewrite result.</returns>
    [Benchmark]
    public CompilationResult<BenchmarkContext> ExpandToNor()
    {
        return this.rule.ExpandToNor();
    }

    /// <summary>Recognises primitive patterns and compresses them to derived operators.</summary>
    /// <returns>The rewritten rule.</returns>
    [Benchmark]
    public CompiledRule<BenchmarkContext> CompressToDerived()
    {
        return this.rule.CompressToDerived();
    }

    /// <summary>Rewrites the tree to its canonical form.</summary>
    /// <returns>The rewritten rule.</returns>
    [Benchmark]
    public CompiledRule<BenchmarkContext> Canonicalize()
    {
        return this.rule.Canonicalize();
    }

    /// <summary>Applies the semantics-preserving simplification rules.</summary>
    /// <returns>The rewritten rule.</returns>
    [Benchmark]
    public CompiledRule<BenchmarkContext> Simplify()
    {
        return this.rule.Simplify();
    }
}
