namespace TruthWeaver.Benchmarks;

using System.Threading;
using BenchmarkDotNet.Attributes;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Measures whether operand order changes <see cref="CompiledRule{TContext}.EvaluateAsync"/>'s running
/// time on a realistic mixed-cost rule, for ticket 03's gating benchmark
/// (<c>.scratch/rewrite-optimization/issues/03-evaluation-order-optimization.md</c>): "A benchmark shows
/// that operand order changes evaluation time on a realistic rule with mixed-cost predicates; if it
/// does not, close this ticket." The rule mimics a gate check - "account active AND not locked AND has
/// permission AND passes an expensive fraud check" - where one cheap predicate (<c>notLocked</c>)
/// decides <see langword="false"/> and the evaluator's left-to-right short-circuit (<see cref="EvaluationMode.ShortCircuit"/>,
/// the default) never has to run the expensive predicate once it is skipped. <see cref="CheapFirstAsync"/> puts
/// the deciding cheap predicate ahead of the expensive one in <c>AND</c>'s operand list; <see cref="ExpensiveFirstAsync"/>
/// puts the expensive predicate first, so it always runs before the short-circuit can apply. Both compile the
/// same four predicates into the same operator tree shape and differ only in operand order.
/// </summary>
[MemoryDiagnoser]
public class EvaluationOrderBenchmarks
{
    /// <summary>
    /// How many busy-wait iterations the expensive predicate spins for. Chosen to give a measurable,
    /// consistent per-call cost (tens of microseconds) without making the whole suite slow.
    /// </summary>
    private const int ExpensiveSpinIterations = 50_000;

    private static readonly EvaluationOptions ShortCircuitOptions = new(Mode: EvaluationMode.ShortCircuit);

    private BenchmarkContext context = null!;
    private CompiledRule<BenchmarkContext> cheapFirstRule = null!;
    private CompiledRule<BenchmarkContext> expensiveFirstRule = null!;

    /// <summary>Compiles both operand orderings of the mixed-cost gate rule, outside the measured operation.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        PredicateRegistryBuilder<BenchmarkContext> registryBuilder = PredicateRegistry<BenchmarkContext>.CreateBuilder();
        registryBuilder.Add(
            PredicateSchema.NoArguments("accountActive", "accountActive", "A cheap check that always answers true."),
            (_, _, _) => ValueTask.FromResult(TruthValue.True)
        );
        registryBuilder.Add(
            PredicateSchema.NoArguments("notLocked", "notLocked", "A cheap check that decides the AND to false."),
            (_, _, _) => ValueTask.FromResult(TruthValue.False)
        );
        registryBuilder.Add(
            PredicateSchema.NoArguments("hasPermission", "hasPermission", "A cheap check that always answers true."),
            (_, _, _) => ValueTask.FromResult(TruthValue.True)
        );
        registryBuilder.Add(
            PredicateSchema.NoArguments("fraudCheck", "fraudCheck", "An expensive check that spins before answering true."),
            (_, _, _) =>
            {
                Thread.SpinWait(ExpensiveSpinIterations);
                return ValueTask.FromResult(TruthValue.True);
            }
        );
        PredicateRegistry<BenchmarkContext> registry = registryBuilder.Build();

        RuleBuilder accountActive = RuleBuilder.Predicate("accountActive");
        RuleBuilder notLocked = RuleBuilder.Predicate("notLocked");
        RuleBuilder hasPermission = RuleBuilder.Predicate("hasPermission");
        RuleBuilder fraudCheck = RuleBuilder.Predicate("fraudCheck");

        RuleCompiler<BenchmarkContext> compiler = new(registry);
        RuleBuilder cheapFirstBuilder = RuleBuilder.And(accountActive, notLocked, hasPermission, fraudCheck);
        this.cheapFirstRule =
            cheapFirstBuilder.Compile(compiler).CompiledRule
            ?? throw new InvalidOperationException("The cheap-first evaluation-order benchmark rule failed to compile.");

        RuleBuilder expensiveFirstBuilder = RuleBuilder.And(fraudCheck, accountActive, notLocked, hasPermission);
        this.expensiveFirstRule =
            expensiveFirstBuilder.Compile(compiler).CompiledRule
            ?? throw new InvalidOperationException("The expensive-first evaluation-order benchmark rule failed to compile.");

        this.context = new BenchmarkContext();
    }

    /// <summary>Evaluates the gate rule with the deciding cheap predicate ahead of the expensive one.</summary>
    /// <returns>The evaluation's decision, so the evaluator cannot be dead-code-eliminated.</returns>
    [Benchmark(Baseline = true)]
    public Task<Decision> CheapFirstAsync()
    {
        return this.cheapFirstRule.EvaluateAsync(this.context, NullServiceProvider.Instance, options: ShortCircuitOptions);
    }

    /// <summary>Evaluates the gate rule with the expensive predicate ahead of the deciding cheap one.</summary>
    /// <returns>The evaluation's decision, so the evaluator cannot be dead-code-eliminated.</returns>
    [Benchmark]
    public Task<Decision> ExpensiveFirstAsync()
    {
        return this.expensiveFirstRule.EvaluateAsync(this.context, NullServiceProvider.Instance, options: ShortCircuitOptions);
    }
}
