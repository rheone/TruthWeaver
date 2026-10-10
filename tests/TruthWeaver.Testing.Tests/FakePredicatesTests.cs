namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;

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

    [Fact]
    public async Task Returning_Bool_True_AlwaysReturnsTrue()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", true);

        TruthValue result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("hasRole", schema.Name);
    }

    [Fact]
    public async Task Returning_Bool_False_AlwaysReturnsFalse()
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

    [Fact]
    public async Task Faulting_AlwaysThrowsGivenException()
    {
        InvalidOperationException exception = new("boom");
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Faulting<object?>("hasRole", exception);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Same(exception, thrown);
    }

    [Fact]
    public void Faulting_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FakePredicates.Faulting<object?>("hasRole", null!));
    }

    [Fact]
    public async Task Scripted_ReturnsSuccessiveAnswersInOrder()
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

    [Fact]
    public async Task Scripted_MoreCallsThanScriptedAnswers_Throws()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Scripted<object?>("flapping", [TruthValue.True]);

        await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
    }

    [Fact]
    public void Scripted_EmptyScript_Throws()
    {
        Assert.Throws<ArgumentException>(() => FakePredicates.Scripted<object?>("flapping", []));
    }
}
