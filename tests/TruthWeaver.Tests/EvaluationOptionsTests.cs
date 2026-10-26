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

    /// <summary>By default the flat trace is not requested, so <see cref="Decision.Trace"/> is null while the trace tree is still filled.</summary>
    [Fact]
    public async Task EvaluateAsync_DefaultOptions_LeavesTheFlatTraceNullAndFillsTheTraceTree_Test()
    {
        Decision decision = await EvaluateWithTraceAsync(EvaluationOptions.Default);

        Assert.Null(decision.Trace);
        Assert.NotNull(decision.TraceTree);
    }

    /// <summary>
    /// With <see cref="EvaluationOptions.IncludeTrace"/> the flat trace holds one entry for each node visited,
    /// operators included, and an operator's entry follows the entries of its operands.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_IncludeTrace_FillsTheFlatTrace_Test()
    {
        Decision decision = await EvaluateWithTraceAsync(new EvaluationOptions(IncludeTrace: true));

        // The flat trace shape changed deliberately (ultra-review ticket 52): it held only the leaves ["a", "b"]
        // before; it now also holds the AND node's own entry, after its operands, carrying the operator's result.
        Assert.NotNull(decision.Trace);
        Assert.Equal(
            [
                new TraceEntry("a", TruthValue.True, false),
                new TraceEntry("b", TruthValue.True, false),
                new TraceEntry("AND", TruthValue.True, false),
            ],
            decision.Trace.Entries
        );
    }

    /// <summary>
    /// Nested operators each add their own flat trace entry after their operands' entries, and an operand skipped by
    /// short-circuiting keeps its not-evaluated entry ahead of the operator that skipped it.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_IncludeTraceWithNestedOperators_RecordsEveryOperatorAfterItsOperands_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("NOT (a OR b)").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(
            [
                new TraceEntry("a", TruthValue.True, false),
                new TraceEntry("b", null, true),
                new TraceEntry("OR", TruthValue.True, false),
                new TraceEntry("NOT", TruthValue.False, false),
            ],
            decision.Trace!.Entries
        );
    }

    /// <summary>
    /// Every operator kind adds its own flat trace entry last, after its operands, and that entry shows the same text
    /// and result as the root node of the trace tree.
    /// </summary>
    /// <param name="ruleText">A rule whose root is the operator under test.</param>
    [Theory]
    [InlineData("a XOR b")]
    [InlineData("a IMPLIES b")]
    [InlineData("If(a, b, c)")]
    [InlineData("COALESCE(a, b)")]
    [InlineData("IsTrue(a)")]
    [InlineData("AtLeast(1, a, b, c)")]
    [InlineData("ExactlyOne(a, b, c)")]
    public async Task EvaluateAsync_IncludeTraceWithOperatorRoot_RecordsTheOperatorEntryLast_Test(string ruleText)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", false)
                .AddConstant("c", true)
                .Build()
        );
        CompilationResult<RuleTestContext> compiled = compiler.Compile(ruleText);
        Assert.True(compiled.Succeeded, string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));

        Decision decision = await compiled.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(new TraceEntry(decision.TraceTree!.Text, decision.TraceTree.Result, false), decision.Trace!.Entries[^1]);
        Assert.Equal(decision.TraceTree.Children.Count + 1, decision.Trace.Entries.Count);
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
            options: new EvaluationOptions(IncludeTrace: true),
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

    /// <summary>
    /// An <c>AND</c> operand skipped by a fault-budget abort counts as <c>Unknown</c>: the inspection makes the first
    /// operand a definite <c>True</c>, but the skipped <c>isOff</c> could still be <c>False</c>, so the rule is
    /// <c>Unknown</c> and not satisfied.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetAbortSkipsAnAndOperand_FoldsTheSkippedOperandAsUnknown_Test()
    {
        Decision decision = await EvaluateAbortingRuleAsync("IsUnknown(boom) AND isOff");

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Single(decision.Faults);
    }

    /// <summary>
    /// An <c>OR</c> operand skipped by a fault-budget abort counts as <c>Unknown</c>: the inspection makes the first
    /// operand a definite <c>False</c>, but the skipped <c>isOn</c> could still be <c>True</c>, so the rule is
    /// <c>Unknown</c>.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetAbortSkipsAnOrOperand_FoldsTheSkippedOperandAsUnknown_Test()
    {
        Decision decision = await EvaluateAbortingRuleAsync("IsTrue(boom) OR isOn");

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>
    /// A <c>COALESCE</c> operand skipped by a fault-budget abort counts as <c>Unknown</c>: the first operand is
    /// <c>Unknown</c> (its own skipped <c>isOff</c> is <c>Unknown</c>), and the skipped <c>isOn</c> adds no definite
    /// value, so the rule is <c>Unknown</c> and not the <c>True</c> of <c>isOn</c>.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetAbortSkipsACoalesceOperand_FoldsTheSkippedOperandAsUnknown_Test()
    {
        Decision decision = await EvaluateAbortingRuleAsync("COALESCE(IsUnknown(boom) AND isOff, isOn)");

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>
    /// A fault-budget abort does not change the result of an <c>AND</c> that already holds a <c>False</c> operand: a
    /// skipped operand counts as <c>Unknown</c>, and <c>False AND Unknown</c> is <c>False</c>.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetAbortAfterADecidingAndOperand_KeepsTheDefiniteResult_Test()
    {
        Decision decision = await EvaluateAbortingRuleAsync("IsKnown(boom) AND isOn");

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>The skipped operand of an aborted chain stays marked as not evaluated in the trace.</summary>
    [Fact]
    public async Task EvaluateAsync_FaultBudgetAbortSkipsAnAndOperand_MarksTheSkippedOperandNotEvaluated_Test()
    {
        Decision decision = await EvaluateAbortingRuleAsync("IsUnknown(boom) AND isOff");

        Assert.Contains(new TraceEntry("isOff", null, true), decision.Trace!.Entries);
        Assert.Equal(new TraceEntry("AND", TruthValue.Unknown, false), decision.Trace.Entries[^1]);
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
            options: new EvaluationOptions(FaultBudget: budget, IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    // Evaluates rule text under a fault budget of 1, where "boom" throws (so the first fault aborts the evaluation),
    // "isOn" is True and "isOff" is False.
    private static Task<Decision> EvaluateAbortingRuleAsync(string text)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddThrowing("boom")
                .AddConstant("isOn", true)
                .AddConstant("isOff", false)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile(text).GetRuleOrThrow();

        return rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(FaultBudget: 1, IncludeTrace: true),
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private static Task<Decision> EvaluateWithTraceAsync(EvaluationOptions options)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND b").CompiledRule!;

        return rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
