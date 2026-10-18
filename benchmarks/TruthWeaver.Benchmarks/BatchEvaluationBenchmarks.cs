namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fakes;

/// <summary>
/// Compares <see cref="CompiledRule{TContext}.EvaluateManyAsync(IEnumerable{TContext}, IServiceProvider?, DataSources?, EvaluationOptions?, int, CancellationToken)"/>
/// with a plain loop of <see cref="CompiledRule{TContext}.EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> calls over the same contexts. The loop is the
/// baseline; the batch runs once with one evaluation at a time (the cost of the ordered window) and once with one
/// evaluation per processor (the gain from parallelism). The rule mixes the binary connectives over predicates that
/// answer synchronously, so the measurement is the engine and not a predicate's I/O.
/// </summary>
[MemoryDiagnoser]
public class BatchEvaluationBenchmarks
{
    private const int TermCount = 8;

    private BenchmarkContext[] contexts = [];
    private CompiledRule<BenchmarkContext> rule = null!;

    /// <summary>Gets or sets how many contexts one batch evaluates.</summary>
    [Params(100, 1000)]
    public int ContextCount { get; set; }

    /// <summary>Compiles the rule and makes the contexts, outside the measured operation.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        PredicateRegistryBuilder<BenchmarkContext> registryBuilder = PredicateRegistry<BenchmarkContext>.CreateBuilder();
        for (int i = 0; i < TermCount; i++)
        {
            (
                PredicateSchema schema,
                Func<BenchmarkContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
            ) = FakePredicates.Returning<BenchmarkContext>($"t{i}", i % 2 == 0 ? TruthValue.True : TruthValue.False);
            registryBuilder.Add(schema, evaluate);
        }

        RuleBuilder[] t = [.. Enumerable.Range(0, TermCount).Select(i => RuleBuilder.Predicate($"t{i}"))];
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.Xor(t[0], t[1]),
            RuleBuilder.Equivalent(t[2], t[3]),
            RuleBuilder.Implies(t[4], t[5]),
            RuleBuilder.Nand(t[6], t[7])
        );
        CompilationResult<BenchmarkContext> result = builder.Compile(
            new RuleCompiler<BenchmarkContext>(registryBuilder.Build())
        );
        this.rule = result.CompiledRule ?? throw new InvalidOperationException("The batch benchmark rule failed to compile.");
        this.contexts = [.. Enumerable.Range(0, this.ContextCount).Select(_ => new BenchmarkContext())];
    }

    /// <summary>Evaluates every context in a <see langword="foreach"/> loop, one call after the other.</summary>
    /// <returns>The number of satisfied decisions, so the work cannot be removed.</returns>
    [Benchmark(Baseline = true)]
    public async Task<int> LoopAsync()
    {
        int satisfied = 0;
        foreach (BenchmarkContext context in this.contexts)
        {
            Decision decision = await this.rule.EvaluateAsync(context, NullServiceProvider.Instance).ConfigureAwait(false);
            satisfied += decision.IsSatisfied ? 1 : 0;
        }

        return satisfied;
    }

    /// <summary>Evaluates every context with <c>EvaluateManyAsync</c> and one evaluation at a time.</summary>
    /// <returns>The number of satisfied decisions, so the work cannot be removed.</returns>
    [Benchmark]
    public Task<int> EvaluateManySequentialAsync()
    {
        return this.CountSatisfiedAsync(1);
    }

    /// <summary>Evaluates every context with <c>EvaluateManyAsync</c> and one evaluation per processor.</summary>
    /// <returns>The number of satisfied decisions, so the work cannot be removed.</returns>
    [Benchmark]
    public Task<int> EvaluateManyParallelAsync()
    {
        return this.CountSatisfiedAsync(Environment.ProcessorCount);
    }

    private async Task<int> CountSatisfiedAsync(int maxDegreeOfParallelism)
    {
        int satisfied = 0;
        await foreach (
            Decision decision in this
                .rule.EvaluateManyAsync(
                    this.contexts,
                    NullServiceProvider.Instance,
                    maxDegreeOfParallelism: maxDegreeOfParallelism
                )
                .ConfigureAwait(false)
        )
        {
            satisfied += decision.IsSatisfied ? 1 : 0;
        }

        return satisfied;
    }
}
