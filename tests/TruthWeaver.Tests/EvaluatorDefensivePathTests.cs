namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The evaluator's defensive paths: a tree that names a predicate or a collection its registry does not hold. A
/// compiled rule keeps the registry it was compiled with, so these trees are built by hand against an empty registry.
/// </summary>
public sealed class EvaluatorDefensivePathTests
{
    /// <summary>A term whose predicate is not in the registry is <c>Unknown</c> plus a Fault that names the predicate.</summary>
    [Fact]
    public async Task EvaluateAsync_UnregisteredPredicate_IsUnknownWithFault_Test()
    {
        TermIdentity identity = new("notRegistered", []);
        Evaluator<RuleTestContext> evaluator = CreateEvaluator();

        Decision decision = await evaluator.EvaluateAsync(new TermExpression(identity));

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal(identity, fault.Term);
        KeyNotFoundException exception = Assert.IsType<KeyNotFoundException>(fault.Exception);
        Assert.Contains("'notRegistered'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A quantifier whose collection is not in the registry is <c>Unknown</c> plus a Fault that names the collection.</summary>
    [Fact]
    public async Task EvaluateAsync_UnregisteredCollection_IsUnknownWithFault_Test()
    {
        Evaluator<RuleTestContext> evaluator = CreateEvaluator();

        Decision decision = await evaluator.EvaluateAsync(
            new QuantifierExpression(QuantifierKind.ForAll, "x", "notRegistered", new ConstantExpression(TruthValue.True))
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal(new TermIdentity("notRegistered", []), fault.Term);
        KeyNotFoundException exception = Assert.IsType<KeyNotFoundException>(fault.Exception);
        Assert.Contains("'notRegistered'", exception.Message, StringComparison.Ordinal);
    }

    private static Evaluator<RuleTestContext> CreateEvaluator()
    {
        return new Evaluator<RuleTestContext>(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            EvaluationOptions.Default,
            TestContext.Current.CancellationToken
        );
    }
}
