namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 11: <see cref="EvaluationOptions"/> — FaultBudget, Exhaustive mode, and timeout.</summary>
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

    [Fact]
    public async Task Unlimited_default_fault_budget_continues_to_completion_with_multiple_faults()
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

    [Fact]
    public async Task Exhaustive_mode_evaluates_remaining_and_operands_after_a_false_first_operand()
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

    [Theory]
    [InlineData(EvaluationMode.ShortCircuit)]
    [InlineData(EvaluationMode.Exhaustive)]
    public async Task Result_is_identical_between_default_and_exhaustive_modes(EvaluationMode mode)
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

    [Fact]
    public Task Timeout_cancels_an_in_flight_evaluation()
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

    [Fact]
    public async Task No_timeout_configured_leaves_evaluation_unaffected()
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

    [Fact]
#pragma warning disable CA1822 // kept as an instance method for reading-order symmetry with its
    // sibling fault-budget test above (SA1204 would otherwise force
    // every static member in this class ahead of every instance one).
    public async Task Fault_budget_abort_leaves_the_trace_with_unevaluated_entries()
#pragma warning restore CA1822
    {
        Decision decision = await EvaluateWithFaultBudgetAsync(1);

        Assert.Contains(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    [Fact]
    public async Task Genuine_cancellation_propagates_rather_than_being_recorded_as_a_fault()
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
