namespace TruthWeaver.Tests;

using Microsoft.Extensions.Logging;
using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 13: structured logging via <see cref="ILogger{TCategoryName}"/>.</summary>
public sealed class LoggingTests
{
    /// <summary>A faulting predicate logs a structured warning with the term and the exception.</summary>
    [Fact]
    public async Task Evaluate_FaultingPredicate_LogsStructuredWarning_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("flaky").Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("flaky").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
        Assert.Equal("flaky", call.Field("Term"));
        Assert.NotNull(call.Exception);
        Assert.IsType<InvalidOperationException>(call.Exception);
    }

    /// <summary>
    /// A throwing collection selector logs a structured warning for the collection, the same as a faulting
    /// predicate (ultra-review ticket 51): a collection-level fault shares the evaluator's central
    /// fault-recording path rather than bypassing it.
    /// </summary>
    [Fact]
    public async Task Evaluate_ThrowingCollectionSelector_LogsStructuredWarning_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCollection<TruthValue>(
                    "items",
                    _ => throw new InvalidOperationException("selector fault"),
                    elements =>
                        elements.Add(
                            PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                            (e, _, _) => ValueTask.FromResult(e.Item)
                        )
                )
                .Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("FORALL(x IN items, elementTrue)").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
        Assert.Equal("items", call.Field("Term"));
        Assert.NotNull(call.Exception);
        Assert.IsType<InvalidOperationException>(call.Exception);
    }

    /// <summary>
    /// Exceeding <see cref="EvaluationOptions.MaxElementEvaluations"/> logs a structured warning for the collection,
    /// the same as a faulting predicate (ultra-review ticket 51).
    /// </summary>
    [Fact]
    public async Task Evaluate_MaxElementEvaluationsExceeded_LogsStructuredWarning_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCollection<TruthValue>(
                    "items",
                    _ => Enumerable.Repeat(TruthValue.True, 10),
                    elements =>
                        elements.Add(
                            PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                            (e, _, _) => ValueTask.FromResult(e.Item)
                        )
                )
                .Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("FORALL(x IN items, elementTrue)").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: new EvaluationOptions(MaxElementEvaluations: 3),
            cancellationToken: TestContext.Current.CancellationToken
        );

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
        Assert.Equal("items", call.Field("Term"));
        Assert.NotNull(call.Exception);
        Assert.IsType<ElementEvaluationLimitExceededException>(call.Exception);
    }

    /// <summary>A faulting predicate's rendered log message still reads as a predicate fault.</summary>
    [Fact]
    public async Task Evaluate_FaultingPredicate_RenderedMessageSaysPredicate_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("flaky").Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("flaky").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
        Assert.StartsWith("Predicate fault for term flaky:", call.Message, StringComparison.Ordinal);
    }

    /// <summary>A throwing collection selector's rendered log message names a collection fault, not a predicate, and keeps its event id.</summary>
    [Fact]
    public async Task Evaluate_ThrowingCollectionSelector_RenderedMessageDoesNotSayPredicate_Test()
    {
        LoggerTestExtensions.LoggedCall call = await EvaluateQuantifierAndGetWarningAsync(
            _ => throw new InvalidOperationException("selector fault"),
            new EvaluationOptions()
        );

        Assert.DoesNotContain("Predicate", call.Message, StringComparison.Ordinal);
        Assert.StartsWith("Collection fault for term items:", call.Message, StringComparison.Ordinal);
        Assert.Equal(2, call.EventId.Id);
    }

    /// <summary>An exceeded element-evaluation limit's rendered log message does not say predicate, and keeps its event id.</summary>
    [Fact]
    public async Task Evaluate_MaxElementEvaluationsExceeded_RenderedMessageDoesNotSayPredicate_Test()
    {
        LoggerTestExtensions.LoggedCall call = await EvaluateQuantifierAndGetWarningAsync(
            _ => Enumerable.Repeat(TruthValue.True, 10),
            new EvaluationOptions(MaxElementEvaluations: 3)
        );

        Assert.DoesNotContain("Predicate", call.Message, StringComparison.Ordinal);
        Assert.StartsWith("ElementLimit fault for term items:", call.Message, StringComparison.Ordinal);
        Assert.Equal(2, call.EventId.Id);
    }

    /// <summary>Compiling a rule with a diagnostic logs one structured event per diagnostic.</summary>
    [Fact]
    public void Compile_DiagnosticRule_LogsOneEventPerDiagnostic_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.Compile("noSuchPredicate");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Error, call.Level);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Error, call.Field("Severity"));
    }

    /// <summary>Compiling a rule with a warning diagnostic logs at warning level.</summary>
    [Fact]
    public void Compile_WarningDiagnostic_LogsAtWarningLevel_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build(),
            logger: logger
        );

        compiler.Compile("a AND FALSE");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Warning, call.Level);
        Assert.Equal(DiagnosticCodes.StructuralContradiction, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Warning, call.Field("Severity"));
    }

    /// <summary>Compiling a rule with an information diagnostic logs at information level.</summary>
    [Fact]
    public void Compile_InfoDiagnostic_LogsAtInformationLevel_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxAnalysisTerms: 1),
            logger: logger
        );

        compiler.Compile("a AND NOT a AND b");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Info, call.Field("Severity"));
    }

    /// <summary>A rule swap notification logs a distinct structured event.</summary>
    [Fact]
    public void RuleSwap_Notification_LogsDistinctStructuredEvent_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.NotifyRuleSwapped("my-rule");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal("my-rule", call.Field("RuleIdentifier"));

        // Distinct event id from both the compile-diagnostic and fault log events.
        Assert.NotEqual(0, call.EventId.Id);
    }

    /// <summary>A rule swap notification without an identifier falls back to unnamed.</summary>
    [Fact]
    public void RuleSwap_NoIdentifier_FallsBackToUnnamed_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.NotifyRuleSwapped();

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal("(unnamed)", call.Field("RuleIdentifier"));
    }

    private static ILogger<RuleCompiler<RuleTestContext>> CreateEnabledLogger()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = Substitute.For<ILogger<RuleCompiler<RuleTestContext>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }

    private static async Task<LoggerTestExtensions.LoggedCall> EvaluateQuantifierAndGetWarningAsync(
        Func<RuleTestContext, IEnumerable<TruthValue>> selector,
        EvaluationOptions options
    )
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCollection<TruthValue>(
                    "items",
                    selector,
                    elements =>
                        elements.Add(
                            PredicateSchema.NoArguments("elementTrue", "elementTrue", "True iff the element itself is True."),
                            (e, _, _) => ValueTask.FromResult(e.Item)
                        )
                )
                .Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("FORALL(x IN items, elementTrue)").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        return Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
    }
}
