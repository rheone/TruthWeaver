namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 11: <see cref="EvaluationOptions"/> - FaultBudget, Exhaustive mode, and timeout.</summary>
public sealed class EvaluationOptionsTests
{
    /// <summary>A budget of 1 aborts as soon as the first fault is recorded, so the later faulting term never runs.</summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetOfOne_AbortsOnTheFirstFault_Test()
    {
        Decision decision = await EvaluateWithFaultBudgetAsync(1);

        Assert.Single(decision.Faults);
    }

    /// <summary>A budget of 2 tolerates the first fault and aborts when the second is recorded.</summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetOfTwo_AbortsOnTheSecondFault_Test()
    {
        Decision decision = await EvaluateWithFaultBudgetAsync(2);

        Assert.Equal(2, decision.Faults.Count);
        Assert.Contains(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    /// <summary>A budget below 1 is rejected, because no evaluation could stay within it.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public Task EvaluateAsync_FaultBudgetBelowOne_ThrowsArgumentOutOfRange_Test(int budget)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a").CompiledRule!;

        return Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                options: new EvaluationOptions(FaultBudget: budget),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>The unlimited default fault budget completes evaluation with multiple faults.</summary>
    [Fact]
    public async Task FaultBudget_UnlimitedDefault_CompletesWithMultipleFaults_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("ExactlyOne(a, b, c)").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, decision.Faults.Count);
        Assert.DoesNotContain(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    /// <summary>Exhaustive mode evaluates the remaining AND operands after a false first operand.</summary>
    [Fact]
    public async Task Exhaustive_FalseFirstOperand_EvaluatesRemainingOperands_Test()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCountingConstant("a", false, invocationLog)
                .AddCountingConstant("b", true, invocationLog)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND b").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Equal(["a", "b"], invocationLog);
    }

    /// <summary>The result is identical between the default and exhaustive modes.</summary>
    [Theory]
    [InlineData(EvaluationMode.ShortCircuit)]
    [InlineData(EvaluationMode.Exhaustive)]
    public async Task Mode_DefaultAndExhaustive_ProduceIdenticalResult_Test(EvaluationMode mode)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", false).AddThrowing("b").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND b").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(Mode: mode),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>A timeout cancels an in flight evaluation.</summary>
    [Fact]
    public Task Timeout_InFlightEvaluation_IsCancelled_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDelayed("slow", TimeSpan.FromSeconds(5), true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("slow").CompiledRule!;

        return Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                options: new EvaluationOptions(Timeout: TimeSpan.FromMilliseconds(50)),
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>Evaluation is unaffected when no timeout is configured.</summary>
    [Fact]
    public async Task Timeout_NotConfigured_LeavesEvaluationUnaffected_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDelayed("fast", TimeSpan.FromMilliseconds(10), true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("fast").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A fault budget abort leaves the trace with unevaluated entries.</summary>
    [Fact]
#pragma warning disable CA1822 // kept as an instance method for reading-order symmetry with its
    // sibling fault-budget test above (SA1204 would otherwise force
    // every static member in this class ahead of every instance one).
    public async Task FaultBudget_Abort_LeavesTraceWithUnevaluatedEntries_Test()
#pragma warning restore CA1822
    {
        Decision decision = await EvaluateWithFaultBudgetAsync(1);

        Assert.Contains(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    /// <summary>Genuine cancellation propagates rather than being recorded as a fault.</summary>
    [Fact]
    public async Task Cancellation_Genuine_PropagatesRatherThanRecordedAsFault_Test()
    {
        using CancellationTokenSource cts = new();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddCancelingPredicate("cancels", cts).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("cancels").CompiledRule!;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            rule.EvaluateAsync(new RuleTestContext(), EmptyServiceProvider.Instance, cancellationToken: cts.Token)
        );
    }

    /// <summary>A predicate's own <see cref="OperationCanceledException"/>, with the caller's token untouched, is a fault, not a propagating cancellation.</summary>
    [Fact]
    public async Task EvaluateAsync_PredicateSelfCancelsWithoutTheCallersTokenBeingCancelled_IsRecordedAsAFault_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddSelfCancelingPredicate("selfCancels").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("selfCancels").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Single(decision.Faults);
        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>A predicate's own <see cref="TimeoutException"/> (its own internal deadline, not the evaluation's token) is a fault, mirroring a data source's own timeout.</summary>
    [Fact]
    public async Task EvaluateAsync_PredicateTimesOutOnItsOwn_IsRecordedAsAFault_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddTimingOutPredicate("timesOut").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("timesOut").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Single(decision.Faults);
        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    private static Task<Decision> EvaluateWithFaultBudgetAsync(int budget)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("ExactlyOne(a, b, c)").CompiledRule!;

        return rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(FaultBudget: budget),
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
