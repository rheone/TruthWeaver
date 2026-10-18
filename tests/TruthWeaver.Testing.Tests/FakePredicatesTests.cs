namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

public sealed class FakePredicatesTests
{
    /// <summary>Without a label argument, each factory labels the predicate with its name, so two fakes have distinct labels.</summary>
    [Fact]
    public void Returning_NoLabel_UsesTheNameAsTheLabel_Test()
    {
        (PredicateSchema isAdmin, _) = FakePredicates.Returning<object?>("isAdmin", true);
        (PredicateSchema isActive, _) = FakePredicates.Returning<object?>("isActive", TruthValue.Unknown);
        (PredicateSchema faulting, _) = FakePredicates.Faulting<object?>("isLocked", new InvalidOperationException());
        (PredicateSchema scripted, _) = FakePredicates.Scripted<object?>("isNew", [TruthValue.True]);

        Assert.Equal(
            ["isAdmin", "isActive", "isLocked", "isNew"],
            [isAdmin.Label, isActive.Label, faulting.Label, scripted.Label]
        );
    }

    /// <summary>An explicit label still wins over the name.</summary>
    [Fact]
    public void Returning_ExplicitLabel_KeepsTheLabel_Test()
    {
        (PredicateSchema schema, _) = FakePredicates.Returning<object?>("isAdmin", true, label: "Is admin");

        Assert.Equal("Is admin", schema.Label);
    }

    /// <summary>Returning true always returns true.</summary>
    [Fact]
    public async Task Returning_BoolTrue_AlwaysReturnsTrue_Test()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", true);

        TruthValue result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("hasRole", schema.Name);
    }

    /// <summary>Returning false always returns false.</summary>
    [Fact]
    public async Task Returning_BoolFalse_AlwaysReturnsFalse_Test()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", false);

        TruthValue result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>Returning with any Kleene value answers exactly that value, including Unknown, without throwing.</summary>
    [Theory]
    [InlineData(TruthValue.True)]
    [InlineData(TruthValue.False)]
    [InlineData(TruthValue.Unknown)]
    public async Task Returning_AnyKleeneValue_ReturnsThatValueDirectly_Test(TruthValue value)
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", value);

        TruthValue result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(value, result);
    }

    /// <summary>Faulting always throws the given exception.</summary>
    [Fact]
    public async Task Faulting_Call_AlwaysThrowsGivenException_Test()
    {
        InvalidOperationException exception = new("boom");
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Faulting<object?>("hasRole", exception);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Same(exception, thrown);
    }

    /// <summary>Faulting throws for a null exception.</summary>
    [Fact]
    public void Faulting_NullException_Throws_Test()
    {
        Assert.Throws<ArgumentNullException>(() => FakePredicates.Faulting<object?>("hasRole", null!));
    }

    /// <summary>Scripted returns successive answers in order.</summary>
    [Fact]
    public async Task Scripted_Calls_ReturnSuccessiveAnswersInOrder_Test()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [TruthValue.True, TruthValue.False, TruthValue.Unknown]);

        TruthValue first = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        TruthValue second = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        TruthValue third = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, first);
        Assert.Equal(TruthValue.False, second);
        Assert.Equal(TruthValue.Unknown, third);
    }

    /// <summary>Scripted throws an <see cref="InvalidOperationException"/> when called more often than it has answers.</summary>
    [Fact]
    public async Task Scripted_MoreCallsThanScriptedAnswers_Throws_Test()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [TruthValue.True]);

        await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
    }

    /// <summary>Scripted throws for an empty script.</summary>
    [Fact]
    public void Scripted_EmptyScript_Throws_Test()
    {
        Assert.Throws<ArgumentException>(() => FakePredicates.Scripted<object?>("flapping", Array.Empty<TruthValue>()));
    }

    /// <summary>
    /// An exhausted script inside a rule is test misuse: the exception escapes <c>EvaluateAsync</c>, so the test fails
    /// instead of passing on an <c>Unknown</c> result.
    /// </summary>
    [Fact]
    public async Task Scripted_ExhaustedInsideARule_EscapesTheEvaluation_Test()
    {
        CompiledRule<object?> rule = Compile(FakePredicates.Scripted<object?>("flapping", [TruthValue.True]));
        await rule.EvaluateAsync(null, cancellationToken: TestContext.Current.CancellationToken);

        InvalidOperationException thrown = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            rule.EvaluateAsync(null, cancellationToken: TestContext.Current.CancellationToken)
        );
        Assert.Contains("'flapping' was called 2 times", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>Scripted steps answer in order, and a step that throws on purpose is a predicate failure, not misuse.</summary>
    [Fact]
    public async Task Scripted_Steps_AnswerInOrderAndThrowOnPurpose_Test()
    {
        TimeoutException timeout = new("service timed out");
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [AnswerTrue, TimeOut]);

        static TruthValue AnswerTrue()
        {
            return TruthValue.True;
        }

        TruthValue TimeOut()
        {
            throw timeout;
        }

        TruthValue first = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        TimeoutException thrown = await Assert.ThrowsAsync<TimeoutException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );

        Assert.Equal(TruthValue.True, first);
        Assert.Same(timeout, thrown);
    }

    /// <summary>
    /// An exception the script throws on purpose is absorbed by the evaluator like any predicate failure (ADR-0001): the
    /// term is <c>Unknown</c> plus a Fault that carries that exception.
    /// </summary>
    [Fact]
    public async Task Scripted_DeliberateExceptionInsideARule_IsUnknownWithFault_Test()
    {
        TimeoutException timeout = new("service timed out");
        CompiledRule<object?> rule = Compile(FakePredicates.Scripted<object?>("flapping", [TimeOut]));

        TruthValue TimeOut()
        {
            throw timeout;
        }

        Decision decision = await rule.EvaluateAsync(null, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Same(timeout, fault.Exception);
    }

    /// <summary>
    /// An exhausted script is test misuse in <see cref="PredicateHarness"/> too: the exception escapes the run instead of
    /// becoming an <c>Unknown</c> case result.
    /// </summary>
    [Fact]
    public async Task Scripted_ExhaustedInsideTheHarness_EscapesTheRun_Test()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [TruthValue.True]);
        await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            PredicateHarness.RunAsync(schema, evaluate, null, cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    /// <summary>
    /// A script that runs out on the harness's cancelled-token call is test misuse too: the exception escapes the run
    /// instead of becoming an observed cancellation outcome.
    /// </summary>
    [Fact]
    public Task Scripted_ExhaustedOnTheHarnessCancellationCall_EscapesTheRun_Test()
    {
        // Two answers cover the two baseline calls of the determinism check, so the cancellation call is the third.
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [TruthValue.True, TruthValue.True]);

        return Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            PredicateHarness.RunAsync(schema, evaluate, null, cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    private static CompiledRule<object?> Compile(
        (PredicateSchema Schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate) fake
    )
    {
        RuleCompiler<object?> compiler = new(
            PredicateRegistry<object?>.CreateBuilder().Add(fake.Schema, fake.Evaluate).Build()
        );
        return compiler.Compile(fake.Schema.Name).CompiledRule!;
    }
}
