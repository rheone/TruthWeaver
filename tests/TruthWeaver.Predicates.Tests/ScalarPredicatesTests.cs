namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

/// <summary>Behavior of <see cref="ScalarPredicates"/> over <c>Boolean</c>, <c>Guid</c> and <c>DateTimeOffset</c> selections.</summary>
public class ScalarPredicatesTests
{
    private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Boolean Equal and NotEqual are complements for true, false and null (Unknown).</summary>
    [Theory]
    [InlineData(true, TruthValue.True, TruthValue.False)]
    [InlineData(false, TruthValue.False, TruthValue.True)]
    [InlineData(null, TruthValue.Unknown, TruthValue.Unknown)]
    public async Task EqualAndNotEqual_Boolean_AreK3Complements_Test(bool? selected, TruthValue equal, TruthValue notEqual)
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> eq) = ScalarPredicates.Equal<Scalars>(
            "e",
            c => c.Flag
        );
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> ne) =
            ScalarPredicates.NotEqual<Scalars>("n", c => c.Flag);
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfBoolean(true) });

        Assert.Equal(equal, await eq(new Scalars(selected, null, null), args, CancellationToken.None));
        Assert.Equal(notEqual, await ne(new Scalars(selected, null, null), args, CancellationToken.None));
    }

    /// <summary>Guid Equal matches only the same value.</summary>
    [Fact]
    public async Task Equal_Guid_MatchesOnlyTheSameValue_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> eq) = ScalarPredicates.Equal<Scalars>(
            "e",
            c => c.Id
        );
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfGuid(A) });

        Assert.Equal(TruthValue.True, await eq(new Scalars(null, A, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await eq(new Scalars(null, B, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await eq(new Scalars(null, null, null), args, CancellationToken.None));
    }

    /// <summary>DateTimeOffset Equal compares the instant, so the same instant in another offset is equal.</summary>
    [Fact]
    public async Task Equal_DateTimeOffset_ComparesTheInstantAcrossOffsets_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> eq) = ScalarPredicates.Equal<Scalars>(
            "e",
            c => c.At
        );
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> ne) =
            ScalarPredicates.NotEqual<Scalars>("n", c => c.At);
        DateTimeOffset utc = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset plusTwo = new(2026, 1, 1, 14, 0, 0, TimeSpan.FromHours(2));
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["value"] = LiteralValue.OfDateTimeOffset(utc) });

        Assert.Equal(TruthValue.True, await eq(new Scalars(null, null, plusTwo), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await ne(new Scalars(null, null, plusTwo), args, CancellationToken.None));
        Assert.Equal(TruthValue.True, await ne(new Scalars(null, null, utc.AddTicks(1)), args, CancellationToken.None));
    }

    /// <summary>In and NotIn over Guid candidates are complements and Unknown for null.</summary>
    [Fact]
    public async Task InAndNotIn_Guid_AreComplementsAndUnknownForNull_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> inEval) = ScalarPredicates.In<Scalars>(
            "i",
            c => c.Id
        );
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> notInEval) =
            ScalarPredicates.NotIn<Scalars>("n", c => c.Id);
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["values"] = LiteralValue.OfArray(LiteralKind.Guid, [LiteralValue.OfGuid(A)]),
            }
        );

        Assert.Equal(TruthValue.True, await inEval(new Scalars(null, A, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await notInEval(new Scalars(null, A, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.False, await inEval(new Scalars(null, B, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.True, await notInEval(new Scalars(null, B, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await inEval(new Scalars(null, null, null), args, CancellationToken.None));
        Assert.Equal(TruthValue.Unknown, await notInEval(new Scalars(null, null, null), args, CancellationToken.None));
    }

    /// <summary>In over booleans and date-times is registerable and tests membership.</summary>
    [Fact]
    public async Task In_BooleanAndDateTimeOffset_TestMembership_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> boolIn) = ScalarPredicates.In<Scalars>(
            "b",
            c => c.Flag
        );
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> dateIn) = ScalarPredicates.In<Scalars>(
            "d",
            c => c.At
        );
        DateTimeOffset at = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        PredicateArguments boolArgs = new(
            new Dictionary<string, LiteralValue>
            {
                ["values"] = LiteralValue.OfArray(LiteralKind.Boolean, [LiteralValue.OfBoolean(true)]),
            }
        );
        PredicateArguments dateArgs = new(
            new Dictionary<string, LiteralValue>
            {
                ["values"] = LiteralValue.OfArray(LiteralKind.DateTimeOffset, [LiteralValue.OfDateTimeOffset(at)]),
            }
        );

        Assert.Equal(TruthValue.True, await boolIn(new Scalars(true, null, null), boolArgs, CancellationToken.None));
        Assert.Equal(TruthValue.False, await boolIn(new Scalars(false, null, null), boolArgs, CancellationToken.None));
        Assert.Equal(TruthValue.True, await dateIn(new Scalars(null, null, at), dateArgs, CancellationToken.None));
        Assert.Equal(TruthValue.False, await dateIn(new Scalars(null, null, at.AddDays(1)), dateArgs, CancellationToken.None));
    }

    /// <summary>IsNull and IsNotNull are definite for every kind, never Unknown.</summary>
    [Fact]
    public async Task IsNullAndIsNotNull_AllKinds_AreDefinite_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> flagNull) =
            ScalarPredicates.IsNull<Scalars>("a", c => c.Flag);
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> idNotNull) =
            ScalarPredicates.IsNotNull<Scalars>("b", c => c.Id);
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> atNull) =
            ScalarPredicates.IsNull<Scalars>("c", c => c.At);

        Assert.Equal(
            TruthValue.True,
            await flagNull(new Scalars(null, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await flagNull(new Scalars(false, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await idNotNull(new Scalars(null, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.True,
            await idNotNull(new Scalars(null, Guid.Empty, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await atNull(new Scalars(null, null, DateTimeOffset.UnixEpoch), PredicateArguments.Empty, CancellationToken.None)
        );
    }

    /// <summary>IsDefault is false for false, Guid.Empty and default(DateTimeOffset) tests, with complements and Unknown for null.</summary>
    [Fact]
    public async Task IsDefaultAndIsNotDefault_AllKinds_FollowComplementAndUnknownForNull_Test()
    {
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> flagDefault) =
            ScalarPredicates.IsDefault<Scalars>("a", c => c.Flag);
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> idNotDefault) =
            ScalarPredicates.IsNotDefault<Scalars>("b", c => c.Id);
        (_, Func<Scalars, PredicateArguments, CancellationToken, ValueTask<TruthValue>> atDefault) =
            ScalarPredicates.IsDefault<Scalars>("c", c => c.At);

        Assert.Equal(
            TruthValue.True,
            await flagDefault(new Scalars(false, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await flagDefault(new Scalars(true, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.Unknown,
            await flagDefault(new Scalars(null, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.False,
            await idNotDefault(new Scalars(null, Guid.Empty, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.True,
            await idNotDefault(new Scalars(null, A, null), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.True,
            await atDefault(new Scalars(null, null, default(DateTimeOffset)), PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Equal(
            TruthValue.Unknown,
            await atDefault(new Scalars(null, null, null), PredicateArguments.Empty, CancellationToken.None)
        );
    }

    /// <summary>Each overload declares an argument of its own kind.</summary>
    [Fact]
    public void Equal_EachKind_DeclaresMatchingArgumentKind_Test()
    {
        Assert.Equal(
            LiteralKind.Boolean,
            Assert.Single(ScalarPredicates.Equal<Scalars>("a", c => c.Flag).Schema.Arguments).Type
        );
        Assert.Equal(LiteralKind.Guid, Assert.Single(ScalarPredicates.Equal<Scalars>("a", c => c.Id).Schema.Arguments).Type);
        Assert.Equal(
            LiteralKind.DateTimeOffset,
            Assert.Single(ScalarPredicates.Equal<Scalars>("a", c => c.At).Schema.Arguments).Type
        );
        Assert.Equal(LiteralKind.GuidArray, Assert.Single(ScalarPredicates.In<Scalars>("a", c => c.Id).Schema.Arguments).Type);
    }

    private sealed record Scalars(bool? Flag, Guid? Id, DateTimeOffset? At);
}
