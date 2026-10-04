namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// How a variable reference resolves or fails (ADR-0006 decisions 6, 7, 9 and 11, data-sources ticket 02): cardinality,
/// conversion, failure kinds and once-per-evaluation memoization.
/// </summary>
public sealed class VariableResolutionTests
{
    /// <summary>
    /// Gets the conversions ADR-0006 decision 7 allows: exact kinds, a string to date or GUID, integer to decimal, whole decimal
    /// to integer. Values are written as <c>kind:text</c> (see <see cref="Literal"/>) so the rows stay serializable.
    /// </summary>
    public static TheoryData<string, string, string> AllowedConversions =>
        new()
        {
            { "takesString", "s:admin", "s:admin" },
            { "takesInt64", "i:18", "i:18" },
            { "takesDecimal", "d:1.5", "d:1.5" },
            { "takesDecimal", "i:18", "d:18" },
            { "takesInt64", "d:18.0", "i:18" },
            { "takesBoolean", "b:true", "b:true" },
            { "takesDateTime", "t:2026-10-03T12:00:00.0000000+00:00", "t:2026-10-03T12:00:00.0000000+00:00" },
            { "takesDateTime", "s:2026-10-03T12:00:00.0000000+00:00", "t:2026-10-03T12:00:00.0000000+00:00" },
            { "takesGuid", "g:3f2504e0-4f89-11d3-9a0c-0305e82c3301", "g:3f2504e0-4f89-11d3-9a0c-0305e82c3301" },
            { "takesGuid", "s:3f2504e0-4f89-11d3-9a0c-0305e82c3301", "g:3f2504e0-4f89-11d3-9a0c-0305e82c3301" },
        };

    /// <summary>Gets the conversions ADR-0006 decision 7 refuses: a string is never a number or boolean, a number never a string, and no rounding.</summary>
    public static TheoryData<string, string> RefusedConversions =>
        new()
        {
            { "takesString", "i:5" },
            { "takesString", "b:true" },
            { "takesInt64", "s:18" },
            { "takesInt64", "d:1.5" },
            { "takesInt64", "d:100000000000000000000" },
            { "takesInt64", "b:true" },
            { "takesDecimal", "s:1.5" },
            { "takesBoolean", "s:true" },
            { "takesBoolean", "i:1" },
            { "takesDateTime", "s:not a date" },
            { "takesDateTime", "i:5" },
            { "takesGuid", "s:not a guid" },
            { "takesGuid", "i:5" },
        };

    /// <summary>Gets every array predicate in the harness.</summary>
    public static TheoryData<string> ArrayPredicates =>
        ["takesStrings", "takesInt64s", "takesDecimals", "takesBooleans", "takesDateTimes", "takesGuids"];

    /// <summary>Gets every scalar predicate in the harness.</summary>
    public static TheoryData<string> ScalarPredicates =>
        ["takesString", "takesInt64", "takesDecimal", "takesBoolean", "takesDateTime", "takesGuid"];

    /// <summary>A single match converts to the argument kind exactly as ADR-0006 decision 7 allows.</summary>
    [Theory]
    [MemberData(nameof(AllowedConversions))]
    public async Task EvaluateAsync_AllowedScalarConversion_PassesTheConvertedValue_Test(
        string predicate,
        string match,
        string expected
    )
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(predicate, [Literal(match)]);

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(Literal(expected), Assert.Single(harness.Received));
    }

    /// <summary>A match that decision 7 does not allow is Unknown plus a type-mismatch fault, and the predicate is not called.</summary>
    [Theory]
    [MemberData(nameof(RefusedConversions))]
    public async Task EvaluateAsync_RefusedScalarConversion_IsUnknownWithATypeMismatchFault_Test(string predicate, string match)
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(predicate, [Literal(match)]);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(VariableFailureKind.TypeMismatch, FaultKind(Assert.Single(decision.Faults)));
        Assert.Empty(harness.Received);
    }

    /// <summary>A scalar argument whose query matches nothing is a Missing fault.</summary>
    [Theory]
    [MemberData(nameof(ScalarPredicates))]
    public async Task EvaluateAsync_ScalarWithZeroMatches_IsUnknownWithAMissingFault_Test(string predicate)
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(predicate, []);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(VariableFailureKind.Missing, FaultKind(Assert.Single(decision.Faults)));
        Assert.Empty(harness.Received);
    }

    /// <summary>A scalar argument whose query matches several nodes is an Ambiguous fault, never a guess at the first.</summary>
    [Fact]
    public async Task EvaluateAsync_ScalarWithManyMatches_IsUnknownWithAnAmbiguousFault_Test()
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(
            "takesInt64",
            [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(VariableFailureKind.Ambiguous, FaultKind(Assert.Single(decision.Faults)));
        Assert.Empty(harness.Received);
    }

    /// <summary>An array argument whose query matches nothing is an empty array, not a fault, for every element kind.</summary>
    [Theory]
    [MemberData(nameof(ArrayPredicates))]
    public async Task EvaluateAsync_ArrayWithZeroMatches_PassesAnEmptyArray_Test(string predicate)
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(predicate, []);

        decision.Should().BeSatisfied().HaveNoFaults();
        LiteralValue received = Assert.Single(harness.Received);
        Assert.True(received.IsArray);
        Assert.Empty(received.AsArray());
    }

    /// <summary>An array argument collects every match, in document order.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayWithManyMatches_CollectsEveryMatchInOrder_Test()
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(
            "takesStrings",
            [LiteralValue.OfString("admin"), LiteralValue.OfString("auditor")]
        );

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(
            LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("admin"), LiteralValue.OfString("auditor")]),
            Assert.Single(harness.Received)
        );
    }

    /// <summary>An array argument applies the scalar conversions to each element, so integers widen to decimals.</summary>
    [Fact]
    public async Task EvaluateAsync_DecimalArrayWithIntegerMatches_WidensEachElement_Test()
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(
            "takesDecimals",
            [LiteralValue.OfInt64(1), LiteralValue.OfDecimal(2.5m)]
        );

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(
            LiteralValue.OfArray(LiteralKind.Decimal, [LiteralValue.OfDecimal(1m), LiteralValue.OfDecimal(2.5m)]),
            Assert.Single(harness.Received)
        );
    }

    /// <summary>One element that cannot convert makes the whole array argument a type-mismatch fault.</summary>
    [Fact]
    public async Task EvaluateAsync_ArrayWithAnUnconvertibleElement_IsUnknownWithATypeMismatchFault_Test()
    {
        (Decision decision, VariableHarness harness) = await EvaluateWithMatchesAsync(
            "takesInt64s",
            [LiteralValue.OfInt64(1), LiteralValue.OfString("2")]
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(VariableFailureKind.TypeMismatch, FaultKind(Assert.Single(decision.Faults)));
        Assert.Empty(harness.Received);
    }

    /// <summary>A source that reports an unsupported node type as data is a type-mismatch fault.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReportsUnsupportedType_IsATypeMismatchFault_Test()
    {
        FakeDataSource source = new FakeDataSource().Failing("$.q", "an object", DataQueryErrorKind.UnsupportedType);

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(VariableFailureKind.TypeMismatch, FaultKind(Assert.Single(decision.Faults)));
    }

    /// <summary>An error returned as data (a failing backend) is a source-error fault carrying the source's message.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReturnsFailure_IsUnknownWithASourceErrorFault_Test()
    {
        FakeDataSource source = new FakeDataSource().Failing("$.q", "source offline");

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal(VariableFailureKind.SourceError, FaultKind(fault));
        Assert.Contains("source offline", fault.Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A malformed query reported as data is a source-error fault when no validator caught it at compile time.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceReportsMalformedQuery_IsASourceErrorFault_Test()
    {
        FakeDataSource source = new FakeDataSource().Failing("$.q", "bad syntax", DataQueryErrorKind.MalformedQuery);

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(VariableFailureKind.SourceError, FaultKind(Assert.Single(decision.Faults)));
    }

    /// <summary>A source that throws is a source-error fault that keeps the thrown exception as its cause.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceThrows_IsUnknownWithASourceErrorFaultKeepingTheCause_Test()
    {
        InvalidOperationException cause = new("backend exploded");
        FakeDataSource source = new FakeDataSource().Throwing("$.q", cause);

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal(VariableFailureKind.SourceError, FaultKind(fault));
        Assert.Same(cause, fault.Exception.InnerException);
    }

    /// <summary>A source's own timeout is a source-error fault; it does not abort the evaluation.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceTimesOutOnItsOwn_IsUnknownWithASourceErrorFault_Test()
    {
        FakeDataSource source = new FakeDataSource().Throwing("$.q", new TimeoutException("backend slow"));

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(VariableFailureKind.SourceError, FaultKind(Assert.Single(decision.Faults)));
    }

    /// <summary>A source that is cancelled on its own (the evaluation's token is untouched) is a source-error fault.</summary>
    [Fact]
    public async Task EvaluateAsync_SourceCancelsItself_IsUnknownWithASourceErrorFault_Test()
    {
        FakeDataSource source = new FakeDataSource().Throwing("$.q", new OperationCanceledException("source gave up"));

        Decision decision = await EvaluateAsync("takesString", source);

        Assert.Equal(VariableFailureKind.SourceError, FaultKind(Assert.Single(decision.Faults)));
    }

    /// <summary>A failed variable is Unknown, so Strong Kleene still lets <c>Unknown OR True</c> be <c>True</c>.</summary>
    [Fact]
    public async Task EvaluateAsync_FailedVariableOrTrue_EvaluatesToTrueAndKeepsTheFault_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"user\", \"$.q\")) OR yes", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource().Throwing("$.q", new InvalidOperationException("down")) };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied();
        Assert.Single(decision.Faults);
    }

    /// <summary>A failed variable also leaves <c>Unknown AND False</c> as <c>False</c>.</summary>
    [Fact]
    public async Task EvaluateAsync_FailedVariableAndFalse_EvaluatesToFalse_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"user\", \"$.q\")) AND no", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource().Failing("$.q", "offline") };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Single(decision.Faults);
    }

    /// <summary>A source failure counts against the fault budget like a predicate fault does.</summary>
    [Fact]
    public async Task EvaluateAsync_VariableFaultsBeyondTheBudget_AbortTheRemainingTerms_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile(
            "takesString(v: from(\"user\", \"$.a\")) OR takesString(v: from(\"user\", \"$.b\")) OR yes",
            "user"
        );
        DataSources sources = new() { ["user"] = new FakeDataSource() };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            new EvaluationOptions(FaultBudget: 0),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Single(decision.Faults);
    }

    /// <summary>A reference used by several terms is queried once, however many terms use it.</summary>
    [Fact]
    public async Task EvaluateAsync_ReferenceUsedByTwoTerms_QueriesTheSourceOnce_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile(
            "takesInt64(v: from(\"user\", \"$.age\")) AND takesDecimal(v: from(\"user\", \"$.age\"))",
            "user"
        );
        FakeDataSource source = new FakeDataSource().With("$.age", 18L);

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied().HaveNoFaults();
        Assert.Equal(["$.age"], source.Queries);
        Assert.Equal([LiteralValue.OfInt64(18), LiteralValue.OfDecimal(18m)], harness.Received);
    }

    /// <summary>A reference that faults is also queried once: the failure is replayed, not retried.</summary>
    [Fact]
    public async Task EvaluateAsync_FaultingReferenceUsedByTwoTerms_QueriesOnceAndFaultsBothTerms_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile(
            "takesInt64(v: from(\"user\", \"$.age\")) OR takesDecimal(v: from(\"user\", \"$.age\"))",
            "user"
        );
        FakeDataSource source = new FakeDataSource().Throwing("$.age", new InvalidOperationException("down"));

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Equal(["$.age"], source.Queries);
        Assert.Equal(2, decision.Faults.Count);
    }

    /// <summary>The same term repeated is memoized as a term: one query, one predicate call, one fault.</summary>
    [Fact]
    public async Task EvaluateAsync_IdenticalTermRepeated_CallsThePredicateAndTheSourceOnce_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile(
            "takesInt64(v: from(\"user\", \"$.age\")) AND takesInt64(v: from(\"user\", \"$.age\"))",
            "user"
        );
        FakeDataSource source = new FakeDataSource().With("$.age", 18L);

        await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Single(source.Queries);
        Assert.Single(harness.Received);
    }

    /// <summary>Each evaluation resolves afresh: nothing is carried over from the previous evaluation.</summary>
    [Fact]
    public async Task EvaluateAsync_TwoEvaluations_QueryTheSourceOncePerEvaluation_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.age\"))", "user");
        FakeDataSource source = new FakeDataSource().With("$.age", 18L);
        DataSources sources = new() { ["user"] = source };

        await VariableHarness.EvaluateAsync(rule, sources, cancellationToken: TestContext.Current.CancellationToken);
        await VariableHarness.EvaluateAsync(rule, sources, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["$.age", "$.age"], source.Queries);
    }

    /// <summary>Faults name the reference and the failure but never the resolved value.</summary>
    [Fact]
    public async Task EvaluateAsync_TypeMismatchFault_NamesTheKindsButNotTheValue_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesInt64(v: from(\"user\", \"$.secret\"))", "user");
        DataSources sources = new() { ["user"] = new FakeDataSource().With("$.secret", "hunter2") };

        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Fault fault = Assert.Single(decision.Faults);
        Assert.Contains("String", fault.Exception.Message, StringComparison.Ordinal);
        Assert.Contains("Int64", fault.Exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", fault.Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Cancelling the evaluation while a source is working cancels the evaluation, exactly as it does for a predicate.</summary>
    [Fact]
    public async Task EvaluateAsync_CallerCancelsDuringQuery_ThrowsOperationCanceled_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"user\", \"$.q\"))", "user");
        using CancellationTokenSource cancellation = new();
        DataSources sources = new() { ["user"] = new CancellingSource(cancellation) };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await VariableHarness.EvaluateAsync(rule, sources, cancellationToken: cancellation.Token)
        );
    }

    /// <summary>The evaluation timeout cancels a source that is still waiting.</summary>
    [Fact]
    public Task EvaluateAsync_EvaluationTimeoutElapsesDuringQuery_ThrowsOperationCanceled_Test()
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile("takesString(v: from(\"user\", \"$.q\"))", "user");
        DataSources sources = new() { ["user"] = new HangingSource() };

        return Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            VariableHarness.EvaluateAsync(
                rule,
                sources,
                new EvaluationOptions(Timeout: TimeSpan.FromMilliseconds(50)),
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>Parses <c>kind:text</c> (s string, i integer, d decimal, b boolean, t date and time, g GUID) into a literal.</summary>
    private static LiteralValue Literal(string encoded)
    {
        string text = encoded[2..];
        return encoded[0] switch
        {
            's' => LiteralValue.OfString(text),
            'i' => LiteralValue.OfInt64(long.Parse(text, CultureInfo.InvariantCulture)),
            'd' => LiteralValue.OfDecimal(decimal.Parse(text, CultureInfo.InvariantCulture)),
            'b' => LiteralValue.OfBoolean(bool.Parse(text)),
            't' => LiteralValue.OfDateTimeOffset(DateTimeOffset.Parse(text, CultureInfo.InvariantCulture)),
            'g' => LiteralValue.OfGuid(Guid.Parse(text)),
            _ => throw new ArgumentException($"Unknown literal tag in '{encoded}'.", nameof(encoded)),
        };
    }

    private static VariableFailureKind FaultKind(Fault fault)
    {
        return Assert.IsType<VariableResolutionException>(fault.Exception).Kind;
    }

    private static async Task<(Decision Decision, VariableHarness Harness)> EvaluateWithMatchesAsync(
        string predicate,
        LiteralValue[] matches
    )
    {
        FakeDataSource source = new FakeDataSource().WithMatches("$.q", matches);
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile($"{predicate}(v: from(\"user\", \"$.q\"))", "user");
        Decision decision = await VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );
        return (decision, harness);
    }

    private static Task<Decision> EvaluateAsync(string predicate, FakeDataSource source)
    {
        VariableHarness harness = new();
        CompiledRule<object?> rule = harness.Compile($"{predicate}(v: from(\"user\", \"$.q\"))", "user");
        return VariableHarness.EvaluateAsync(
            rule,
            new DataSources { ["user"] = source },
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    /// <summary>A source that cancels the caller's token while it is answering.</summary>
    private sealed class CancellingSource(CancellationTokenSource cancellation) : IDataSource
    {
        public async ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            await cancellation.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return DataQueryResult.Empty();
        }

        public ValueTask<IDataSource> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<IDataSource>(this);
        }
    }

    /// <summary>A source that never answers until its token is cancelled.</summary>
    private sealed class HangingSource : IDataSource
    {
        public async ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return DataQueryResult.Empty();
        }

        public ValueTask<IDataSource> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<IDataSource>(this);
        }
    }
}
