namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;

/// <summary>
/// Measures <see cref="CompiledRule{TContext}.EvaluateAsync"/> for each operator family the Strong K3
/// language surface added (ADR-0005), so a slowdown in one family's evaluator path is visible on its
/// own. Every rule is a single operator (or a small group of them) over predicates that answer
/// <see cref="TruthValue.True"/>, <see cref="TruthValue.False"/> and <see cref="TruthValue.Unknown"/> in turn,
/// run in <see cref="EvaluationMode.Exhaustive"/> so no operand is skipped by short-circuiting.
/// </summary>
[MemoryDiagnoser]
public class OperatorBenchmarks
{
    private const int TermCount = 8;

    private static readonly EvaluationOptions ExhaustiveOptions = new(Mode: EvaluationMode.Exhaustive);

    private BenchmarkContext context = null!;
    private CompiledRule<BenchmarkContext> rule = null!;

    /// <summary>Gets or sets which operator family this case evaluates.</summary>
    [Params("Connectives", "Cardinality", "Threshold", "External", "If", "Parity")]
    public string Family { get; set; } = string.Empty;

    /// <summary>Compiles the rule for <see cref="Family"/>, outside the measured operation.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        PredicateRegistryBuilder<BenchmarkContext> registryBuilder = PredicateRegistry<BenchmarkContext>.CreateBuilder();
        for (int i = 0; i < TermCount; i++)
        {
            TruthValue value = (i % 3) switch
            {
                0 => TruthValue.True,
                1 => TruthValue.False,
                _ => TruthValue.Unknown,
            };
            (
                PredicateSchema schema,
                Func<BenchmarkContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
            ) = FakePredicates.Returning<BenchmarkContext>($"t{i}", value);
            registryBuilder.Add(schema, evaluate);
        }

        RuleBuilder[] t = [.. Enumerable.Range(0, TermCount).Select(i => RuleBuilder.Predicate($"t{i}"))];
        RuleBuilder builder = this.Family switch
        {
            "Connectives" => RuleBuilder.And(
                RuleBuilder.Xor(t[0], t[1]),
                RuleBuilder.Equivalent(t[2], t[3]),
                RuleBuilder.Implies(t[4], t[5]),
                RuleBuilder.Nand(t[6], t[7]),
                RuleBuilder.Nor(t[0], t[2])
            ),
            "Cardinality" => RuleBuilder.And(
                RuleBuilder.Any(t),
                RuleBuilder.All(t),
                RuleBuilder.None(t),
                RuleBuilder.ExactlyOne(t)
            ),
            "Threshold" => RuleBuilder.And(
                RuleBuilder.AtLeast(3, t),
                RuleBuilder.AtMost(5, t),
                RuleBuilder.Exactly(3, t),
                RuleBuilder.Between(2, 6, t)
            ),
            "External" => RuleBuilder.And(
                RuleBuilder.Coalesce(t[2], t[1], t[0]),
                RuleBuilder.IsTrue(t[0]),
                RuleBuilder.IsFalse(t[1]),
                RuleBuilder.IsUnknown(t[2]),
                RuleBuilder.IsKnown(t[3])
            ),
            "If" => RuleBuilder.If(t[0], t[1], t[2]),
            "Parity" => RuleBuilder.Parity(t),
            _ => throw new NotSupportedException($"Unhandled operator family '{this.Family}'."),
        };

        RuleCompiler<BenchmarkContext> compiler = new(registryBuilder.Build());
        this.rule =
            builder.Compile(compiler).CompiledRule
            ?? throw new InvalidOperationException($"The '{this.Family}' benchmark rule failed to compile.");
        this.context = new BenchmarkContext();
    }

    /// <summary>Evaluates the family's rule once.</summary>
    /// <returns>The evaluation's decision, so the evaluator cannot be dead-code-eliminated.</returns>
    [Benchmark]
    public Task<Decision> EvaluateAsync()
    {
        return this.rule.EvaluateAsync(this.context, NullServiceProvider.Instance, ExhaustiveOptions);
    }
}
