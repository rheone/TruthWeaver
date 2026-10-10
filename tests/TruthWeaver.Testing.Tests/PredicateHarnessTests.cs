namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateHarnessTests
{
    private static readonly PredicateSchema MinAgeSchema = new(
        "ageAtLeast",
        "Age at least",
        "Is the person at least the given age?",
        [new PredicateArgumentSchema("min", "The smallest age that passes.", LiteralKind.Int64)]
    );

    /// <summary>A pure predicate that reads every declared argument passes every check.</summary>
    [Fact]
    public async Task RunAsync_WellBehavedPredicate_PassesEveryCheck_Test()
    {
        PredicateHarnessReport report = await PredicateHarness.RunAsync(
            MinAgeSchema,
            (age, args, _) => ValueTask.FromResult(age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False),
            30,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(report.Passed);
        Assert.Empty(report.Failures);
        Assert.Contains(report.Outcomes, o => o.Check == PredicateHarnessCheck.Determinism);
        Assert.Contains(report.Outcomes, o => o.Check == PredicateHarnessCheck.BoundaryValue);
        Assert.Contains(report.Outcomes, o => o.Check == PredicateHarnessCheck.SchemaConformance);
        Assert.Contains(report.Outcomes, o => o.Check == PredicateHarnessCheck.Cancellation);
    }

    /// <summary>A predicate that answers differently on a repeat call fails the determinism check.</summary>
    [Fact]
    public async Task RunAsync_PredicateGivesDifferentAnswers_FailsDeterminism_Test()
    {
        int calls = 0;

        PredicateHarnessReport report = await RunMinAgeAsync(
            (_, args, _) =>
            {
                _ = args.GetInt64("min");
                calls++;
                return ValueTask.FromResult(calls % 2 == 0 ? TruthValue.True : TruthValue.False);
            }
        );

        PredicateHarnessOutcome determinism = Assert.Single(report.Outcomes, o => o.Check == PredicateHarnessCheck.Determinism);
        Assert.Equal(PredicateHarnessStatus.Failed, determinism.Status);
        Assert.False(report.Passed);
    }

    /// <summary>The harness generates the documented boundary values for each declared literal kind.</summary>
    [Fact]
    public async Task RunAsync_ArgumentOfEachKind_GeneratesBoundaryValues_Test()
    {
        LiteralKind[] kinds = Enum.GetValues<LiteralKind>();
        PredicateSchema schema = new(
            "everyKind",
            "Every kind",
            "Takes one argument of each kind.",
            [.. kinds.Select(k => new PredicateArgumentSchema(k.ToString(), "An argument.", k))]
        );
        List<LiteralValue> seen = [];

        await PredicateHarness.RunAsync(
            schema,
            (object? _, PredicateArguments args, CancellationToken _) =>
            {
                seen.AddRange(kinds.Select(k => args.GetRaw(k.ToString())));
                return ValueTask.FromResult(TruthValue.True);
            },
            null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(LiteralValue.OfString(string.Empty), seen);
        Assert.Contains(seen, v => v.Kind == LiteralKind.String && v.AsString().Length >= 10_000);
        Assert.Contains(LiteralValue.OfInt64(long.MinValue), seen);
        Assert.Contains(LiteralValue.OfInt64(long.MaxValue), seen);
        Assert.Contains(LiteralValue.OfDecimal(decimal.MinValue), seen);
        Assert.Contains(LiteralValue.OfDecimal(decimal.MaxValue), seen);
        Assert.Contains(LiteralValue.OfGuid(Guid.Empty), seen);
        Assert.Contains(LiteralValue.OfDateTimeOffset(DateTimeOffset.MinValue), seen);
        Assert.Contains(LiteralValue.OfDateTimeOffset(DateTimeOffset.MaxValue), seen);
        Assert.Contains(LiteralValue.OfBoolean(false), seen);
        foreach (LiteralKind arrayKind in kinds.Where(k => k.ToString().EndsWith("Array", StringComparison.Ordinal)))
        {
            Assert.Contains(seen, v => v.Kind == arrayKind && v.AsArray().Count == 0);
        }
    }

    /// <summary>An exception from a boundary value is a failure that records Unknown with a fault, as the engine does.</summary>
    [Fact]
    public async Task RunAsync_BoundaryValueThrows_ReportsFailureWithFault_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (age, args, _) =>
                ValueTask.FromResult(checked(age - args.GetInt64("min")) <= 0 ? TruthValue.False : TruthValue.True)
        );

        PredicateHarnessOutcome failure = Assert.Single(report.Failures);
        Assert.Equal(PredicateHarnessCheck.BoundaryValue, failure.Check);
        Assert.Equal("min = Int64.MinValue", failure.Case);
        Assert.IsType<OverflowException>(failure.Exception);
        Assert.True(failure.HasFault);
        Assert.Equal(TruthValue.Unknown, failure.Value);
    }

    /// <summary>An exception type on the allow-list reports as an expected fault and the run passes.</summary>
    [Fact]
    public async Task RunAsync_ExceptionTypeAllowListed_ReportsExpectedFault_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (age, args, _) =>
                ValueTask.FromResult(checked(age - args.GetInt64("min")) <= 0 ? TruthValue.False : TruthValue.True),
            new PredicateHarnessOptions { ExpectedFaults = [PredicateHarnessExpectedFault.OfType<ArithmeticException>()] }
        );

        Assert.True(report.Passed);
        PredicateHarnessOutcome expected = Assert.Single(
            report.Outcomes,
            o => o.Status == PredicateHarnessStatus.ExpectedFault
        );
        Assert.Equal("min = Int64.MinValue", expected.Case);
        Assert.True(expected.HasFault);
    }

    /// <summary>An argument on the allow-list excuses that argument's boundary values only, not another argument's.</summary>
    [Fact]
    public async Task RunAsync_ArgumentAllowListed_ExcusesOnlyThatArgument_Test()
    {
        PredicateSchema schema = new(
            "between",
            "Between",
            "Is the value between the bounds?",
            [
                new PredicateArgumentSchema("lower", "The lower bound.", LiteralKind.Int64),
                new PredicateArgumentSchema("upper", "The upper bound.", LiteralKind.Int64),
            ]
        );

        PredicateHarnessReport report = await PredicateHarness.RunAsync(
            schema,
            (value, args, _) =>
            {
                long lower = args.GetInt64("lower");
                long upper = args.GetInt64("upper");
                if (lower > upper)
                {
                    throw new ArgumentException("The lower bound is greater than the upper bound.");
                }

                return ValueTask.FromResult(value >= lower && value <= upper ? TruthValue.True : TruthValue.False);
            },
            1L,
            new PredicateHarnessOptions { ExpectedFaults = [PredicateHarnessExpectedFault.ForArgument("lower")] },
            TestContext.Current.CancellationToken
        );

        Assert.Contains(
            report.Outcomes,
            o => o.Status == PredicateHarnessStatus.ExpectedFault && o.Case == "lower = Int64.MaxValue"
        );
        Assert.NotEmpty(report.Failures);
        Assert.All(report.Failures, f => Assert.StartsWith("upper = ", f.Case, StringComparison.Ordinal));
    }

    /// <summary>Baseline arguments from the options replace the generated ones.</summary>
    [Fact]
    public async Task RunAsync_BaselineArgumentsInOptions_ReplaceGeneratedValues_Test()
    {
        List<long> seen = [];

        await RunMinAgeAsync(
            (_, args, _) =>
            {
                seen.Add(args.GetInt64("min"));
                return ValueTask.FromResult(TruthValue.True);
            },
            new PredicateHarnessOptions
            {
                Arguments = new Dictionary<string, LiteralValue> { ["min"] = LiteralValue.OfInt64(42) },
            }
        );

        Assert.Equal(42, seen[0]);
    }

    /// <summary>A declared argument that the predicate never reads fails schema conformance and names the argument.</summary>
    [Fact]
    public async Task RunAsync_DeclaredArgumentNeverRead_FailsSchemaConformance_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync((_, _, _) => ValueTask.FromResult(TruthValue.True));

        PredicateHarnessOutcome failure = Assert.Single(report.Failures);
        Assert.Equal(PredicateHarnessCheck.SchemaConformance, failure.Check);
        Assert.Contains("'min'", failure.Detail, StringComparison.Ordinal);
        Assert.Contains("never reads", failure.Detail, StringComparison.Ordinal);
    }

    /// <summary>A read of an undeclared argument fails schema conformance and names the argument.</summary>
    [Fact]
    public async Task RunAsync_UndeclaredArgumentRead_FailsSchemaConformance_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (_, args, _) =>
            {
                _ = args.GetInt64("min");
                return ValueTask.FromResult(args.TryGetString("region", out _) ? TruthValue.True : TruthValue.False);
            }
        );

        PredicateHarnessOutcome failure = Assert.Single(report.Failures);
        Assert.Equal(PredicateHarnessCheck.SchemaConformance, failure.Check);
        Assert.Contains("'region'", failure.Detail, StringComparison.Ordinal);
        Assert.Contains("does not declare", failure.Detail, StringComparison.Ordinal);
    }

    /// <summary>A predicate that throws for a cancelled token is observed, not judged.</summary>
    [Fact]
    public async Task RunAsync_PredicateObservesCancellation_ReportsObserved_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (age, args, token) =>
            {
                token.ThrowIfCancellationRequested();
                return ValueTask.FromResult(age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False);
            }
        );

        PredicateHarnessOutcome cancellation = Assert.Single(
            report.Outcomes,
            o => o.Check == PredicateHarnessCheck.Cancellation
        );
        Assert.Equal(PredicateHarnessStatus.Observed, cancellation.Status);
        Assert.IsType<OperationCanceledException>(cancellation.Exception, exactMatch: false);
        Assert.True(report.Passed);
    }

    /// <summary>A predicate that ignores a cancelled token is observed and the run still passes.</summary>
    [Fact]
    public async Task RunAsync_PredicateIgnoresCancellation_ReportsObservedAndPasses_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (age, args, _) => ValueTask.FromResult(age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False)
        );

        PredicateHarnessOutcome cancellation = Assert.Single(
            report.Outcomes,
            o => o.Check == PredicateHarnessCheck.Cancellation
        );
        Assert.Equal(PredicateHarnessStatus.Observed, cancellation.Status);
        Assert.Contains("did not observe", cancellation.Detail, StringComparison.Ordinal);
        Assert.True(report.Passed);
    }

    /// <summary>A returned Unknown is a valid answer and the outcome says that it came without a fault.</summary>
    [Fact]
    public async Task RunAsync_PredicateReturnsUnknown_PassesWithoutFault_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (_, args, _) =>
            {
                _ = args.GetInt64("min");
                return ValueTask.FromResult(TruthValue.Unknown);
            }
        );

        Assert.True(report.Passed);
        PredicateHarnessOutcome baseline = Assert.Single(report.Outcomes, o => o.Check == PredicateHarnessCheck.Baseline);
        Assert.Equal(TruthValue.Unknown, baseline.Value);
        Assert.False(baseline.HasFault);
        Assert.Contains("without a fault", baseline.Detail, StringComparison.Ordinal);
    }

    /// <summary>An optional argument without a default gets a case where the argument is absent.</summary>
    [Fact]
    public async Task RunAsync_OptionalArgumentWithoutDefault_AddsOmittedCase_Test()
    {
        PredicateSchema schema = new(
            "hasTag",
            "Has tag",
            "Does the item carry the tag?",
            [new PredicateArgumentSchema("tag", "The tag.", LiteralKind.String, Required: false)]
        );

        PredicateHarnessReport report = await PredicateHarness.RunAsync(
            schema,
            (item, args, _) =>
            {
                if (!args.TryGetString("tag", out string? tag))
                {
                    return ValueTask.FromResult(TruthValue.Unknown);
                }

                return ValueTask.FromResult(item == tag ? TruthValue.True : TruthValue.False);
            },
            "text",
            cancellationToken: TestContext.Current.CancellationToken
        );

        PredicateHarnessOutcome omitted = Assert.Single(report.Outcomes, o => o.Case == "tag omitted");
        Assert.Equal(TruthValue.Unknown, omitted.Value);
        Assert.True(report.Passed);
    }

    /// <summary>ShouldPass throws a harness exception whose message lists every failure.</summary>
    [Fact]
    public async Task ShouldPass_FailedChecks_ThrowsListingFailures_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (_, args, _) => ValueTask.FromResult(args.TryGetString("region", out _) ? TruthValue.True : TruthValue.False)
        );

        PredicateHarnessException exception = Assert.Throws<PredicateHarnessException>(report.ShouldPass);

        Assert.Contains("ageAtLeast", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'min'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'region'", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>ShouldPass returns without an exception when every check holds.</summary>
    [Fact]
    public async Task ShouldPass_AllChecksHold_DoesNotThrow_Test()
    {
        PredicateHarnessReport report = await RunMinAgeAsync(
            (age, args, _) => ValueTask.FromResult(age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False)
        );

        report.ShouldPass();
    }

    /// <summary>A null schema is a programming error and throws ArgumentNullException.</summary>
    [Fact]
    public Task RunAsync_NullSchema_ThrowsArgumentNullException_Test()
    {
        return Assert.ThrowsAsync<ArgumentNullException>(() =>
            PredicateHarness.RunAsync(
                null!,
                (_, _, _) => ValueTask.FromResult(TruthValue.True),
                0,
                cancellationToken: TestContext.Current.CancellationToken
            )
        );
    }

    private static Task<PredicateHarnessReport> RunMinAgeAsync(
        Func<int, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate,
        PredicateHarnessOptions? options = null
    )
    {
        return PredicateHarness.RunAsync(MinAgeSchema, evaluate, 30, options, TestContext.Current.CancellationToken);
    }
}
