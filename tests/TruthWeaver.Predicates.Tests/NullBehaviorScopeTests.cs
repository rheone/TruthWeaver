namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// Pins the host-wide default for <see cref="NullBehavior"/>: a <see cref="NullBehaviorScope"/> sets what a built-in
/// factory uses when the caller passes no option, an explicit option always wins, a family scope beats an all-families
/// scope, and disposing a scope restores the previous default.
/// </summary>
public class NullBehaviorScopeTests
{
    private static readonly PredicateArguments NoArguments = new(new Dictionary<string, LiteralValue>());

    /// <summary>Without a scope, a factory call that passes no option answers Unknown for a null selection.</summary>
    [Fact]
    public async Task Factory_NoScopeAndNoOption_AnswersUnknownForNull_Test()
    {
        TruthValue result = await EvaluateNullStringAsync();

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary>A scope that sets False makes every factory call without an option answer False for a null selection.</summary>
    [Fact]
    public async Task Begin_FalseForAllFamilies_FactoryWithoutOptionAnswersFalse_Test()
    {
        using NullBehaviorScope scope = NullBehaviorScope.Begin(NullBehavior.False);

        Assert.Equal(TruthValue.False, await EvaluateNullStringAsync());
        Assert.Equal(TruthValue.False, await EvaluateNullNumberAsync());
        Assert.Equal(TruthValue.False, await EvaluateNullCollectionAsync());
    }

    /// <summary>An option passed to the factory wins over the scope, in both directions.</summary>
    [Fact]
    public async Task Begin_ExplicitOptionOnFactory_WinsOverTheScope_Test()
    {
        using (NullBehaviorScope.Begin(NullBehavior.False))
        {
            Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync(NullBehavior.Unknown));
        }

        using (NullBehaviorScope.Begin(NullBehavior.Unknown))
        {
            Assert.Equal(TruthValue.False, await EvaluateNullStringAsync(NullBehavior.False));
        }
    }

    /// <summary>A scope for one family changes that family only.</summary>
    [Fact]
    public async Task Begin_FalseForOneFamily_OtherFamiliesStayUnknown_Test()
    {
        using NullBehaviorScope scope = NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.False);

        Assert.Equal(TruthValue.False, await EvaluateNullNumberAsync());
        Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
        Assert.Equal(TruthValue.Unknown, await EvaluateNullCollectionAsync());
    }

    /// <summary>A family scope beats an all-families scope, whichever opened first.</summary>
    [Fact]
    public async Task Begin_FamilyScopeAndAllFamiliesScope_FamilyScopeWinsInEitherOrder_Test()
    {
        using (NullBehaviorScope.Begin(NullBehavior.False))
        using (NullBehaviorScope.Begin(PredicateFamily.String, NullBehavior.Unknown))
        {
            Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
            Assert.Equal(TruthValue.False, await EvaluateNullNumberAsync());
        }

        using (NullBehaviorScope.Begin(PredicateFamily.String, NullBehavior.Unknown))
        using (NullBehaviorScope.Begin(NullBehavior.False))
        {
            Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
            Assert.Equal(TruthValue.False, await EvaluateNullNumberAsync());
        }
    }

    /// <summary>Among scopes of the same kind, the innermost wins, and closing it restores the outer one.</summary>
    [Fact]
    public async Task Dispose_InnerScope_RestoresTheOuterScope_Test()
    {
        using NullBehaviorScope outer = NullBehaviorScope.Begin(NullBehavior.False);
        NullBehaviorScope inner = NullBehaviorScope.Begin(NullBehavior.Unknown);
        Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());

        inner.Dispose();

        Assert.Equal(TruthValue.False, await EvaluateNullStringAsync());
    }

    /// <summary>
    /// Disposing an outer scope before an inner one, then the inner one, leaves no scope in effect: the default is the
    /// no-scope Unknown again, not the disposed outer scope's False.
    /// </summary>
    [Fact]
    public async Task Dispose_OuterBeforeInner_LeavesNoScopeInEffect_Test()
    {
        NullBehaviorScope outer = NullBehaviorScope.Begin(NullBehavior.False);
        NullBehaviorScope inner = NullBehaviorScope.Begin(PredicateFamily.Type, NullBehavior.Unknown);

        outer.Dispose();
        inner.Dispose();

        Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
    }

    /// <summary>
    /// Disposing an outer scope while an inner scope is still open stops only the outer scope: a string factory, which the
    /// inner Type scope does not cover, gets the no-scope Unknown, and a Type factory still gets the inner scope's False.
    /// </summary>
    [Fact]
    public async Task Dispose_OuterWhileInnerIsOpen_StopsOnlyTheOuterScope_Test()
    {
        NullBehaviorScope outer = NullBehaviorScope.Begin(NullBehavior.False);
        using NullBehaviorScope inner = NullBehaviorScope.Begin(PredicateFamily.Type, NullBehavior.False);

        outer.Dispose();

        Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
        Assert.Equal(TruthValue.False, await EvaluateNullTypeAsync());
    }

    /// <summary>After the only scope closes, the default is Unknown again.</summary>
    [Fact]
    public async Task Dispose_OnlyScope_RestoresUnknown_Test()
    {
        NullBehaviorScope.Begin(NullBehavior.False).Dispose();

        Assert.Equal(TruthValue.Unknown, await EvaluateNullStringAsync());
    }

    /// <summary>A factory reads the scope when it runs, so a predicate built inside a scope keeps its answer after the scope closes.</summary>
    [Fact]
    public async Task Begin_PredicateBuiltInsideScope_KeepsItsAnswerAfterTheScopeCloses_Test()
    {
        Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate;
        using (NullBehaviorScope.Begin(NullBehavior.False))
        {
            (_, evaluate) = StringPredicates.IsEqualTo<TestContext>("isAlice", c => c.Value);
        }

        TruthValue result = await evaluate(new TestContext(null), StringArguments(), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><see cref="NullBehavior.Default"/> is not a choice, so a scope rejects it.</summary>
    [Fact]
    public void Begin_DefaultBehavior_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NullBehaviorScope.Begin(NullBehavior.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.Default)
        );
    }

    /// <summary>An undefined family is rejected.</summary>
    [Fact]
    public void Begin_UndefinedFamily_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NullBehaviorScope.Begin((PredicateFamily)99, NullBehavior.False));
    }

    /// <summary>The null tests take no option, so a scope does not change them.</summary>
    [Fact]
    public async Task Begin_FalseForAllFamilies_IsNullStaysDefinite_Test()
    {
        using NullBehaviorScope scope = NullBehaviorScope.Begin(NullBehavior.False);
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsNull<TestContext>("isNull", _ => (long?)null);

        TruthValue result = await evaluate(new TestContext(null), NoArguments, CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    private static PredicateArguments StringArguments()
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfString("alice") });
    }

    private static async Task<TruthValue> EvaluateNullStringAsync(NullBehavior? option = null)
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) = option is { } given
            ? StringPredicates.IsEqualTo<TestContext>("isAlice", c => c.Value, nullBehavior: given)
            : StringPredicates.IsEqualTo<TestContext>("isAlice", c => c.Value);

        return await evaluate(new TestContext(null), StringArguments(), CancellationToken.None);
    }

    private static async Task<TruthValue> EvaluateNullTypeAsync()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            TypePredicates.IsGuid<TestContext>("isGuid", c => c.Value);

        return await evaluate(new TestContext(null), NoArguments, CancellationToken.None);
    }

    private static async Task<TruthValue> EvaluateNullNumberAsync()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            NumericPredicates.IsGreaterThan<TestContext>("above", _ => (long?)null);
        PredicateArguments arguments = new(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfInt64(18) });

        return await evaluate(new TestContext(null), arguments, CancellationToken.None);
    }

    private static async Task<TruthValue> EvaluateNullCollectionAsync()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.Contains<TestContext>("has", c => c.Values);
        PredicateArguments arguments = new(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfString("a") });

        return await evaluate(new TestContext(null), arguments, CancellationToken.None);
    }
}
