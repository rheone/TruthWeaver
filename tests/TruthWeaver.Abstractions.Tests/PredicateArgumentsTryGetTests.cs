namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateArgumentsTryGetTests
{
    private static readonly DateTimeOffset Moment = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

    /// <summary>A present argument of the requested kind returns true with its value.</summary>
    [Fact]
    public void TryGetScalar_MatchingKind_ReturnsTrueWithTheValue_Test()
    {
        PredicateArguments args = Sample();

        Assert.True(args.TryGetString("role", out string? text));
        Assert.Equal("Y", text);
        Assert.True(args.TryGetInt64("count", out long integer));
        Assert.Equal(3L, integer);
        Assert.True(args.TryGetDecimal("ratio", out decimal number));
        Assert.Equal(0.5m, number);
        Assert.True(args.TryGetBool("active", out bool flag));
        Assert.True(flag);
        Assert.True(args.TryGetDateTimeOffset("when", out DateTimeOffset moment));
        Assert.Equal(Moment, moment);
        Assert.True(args.TryGetGuid("id", out Guid id));
        Assert.Equal(Id, id);
    }

    /// <summary>A missing name or an argument of another kind returns false and never throws.</summary>
    [Fact]
    public void TryGetScalar_MissingNameOrWrongKind_ReturnsFalse_Test()
    {
        PredicateArguments args = Sample();

        Assert.False(args.TryGetString("absent", out _));
        Assert.False(args.TryGetString("count", out _));
        Assert.False(args.TryGetInt64("role", out _));
        Assert.False(args.TryGetDecimal("absent", out _));
        Assert.False(args.TryGetBool("role", out _));
        Assert.False(args.TryGetDateTimeOffset("role", out _));
        Assert.False(args.TryGetGuid("absent", out _));
    }

    /// <summary>A present array argument returns true with its elements in source order.</summary>
    [Fact]
    public void TryGetArray_MatchingKind_ReturnsTrueWithTheElements_Test()
    {
        PredicateArguments args = Sample();

        Assert.True(args.TryGetStringArray("roles", out IReadOnlyList<string>? roles));
        Assert.Equal(["a", "b"], roles);
        Assert.True(args.TryGetInt64Array("counts", out IReadOnlyList<long>? counts));
        Assert.Equal([1L, 2L], counts);
        Assert.True(args.TryGetDecimalArray("ratios", out IReadOnlyList<decimal>? ratios));
        Assert.Equal([0.5m], ratios);
        Assert.True(args.TryGetBoolArray("flags", out IReadOnlyList<bool>? flags));
        Assert.Equal([false], flags);
        Assert.True(args.TryGetDateTimeOffsetArray("whens", out IReadOnlyList<DateTimeOffset>? whens));
        Assert.Equal([Moment], whens);
        Assert.True(args.TryGetGuidArray("ids", out IReadOnlyList<Guid>? ids));
        Assert.Equal([Id], ids);
    }

    /// <summary>A missing name or an argument of another kind returns false and never throws.</summary>
    [Fact]
    public void TryGetArray_MissingNameOrWrongKind_ReturnsFalse_Test()
    {
        PredicateArguments args = Sample();

        Assert.False(args.TryGetStringArray("absent", out _));
        Assert.False(args.TryGetStringArray("counts", out _));
        Assert.False(args.TryGetInt64Array("roles", out _));
        Assert.False(args.TryGetDecimalArray("role", out _));
        Assert.False(args.TryGetBoolArray("absent", out _));
        Assert.False(args.TryGetDateTimeOffsetArray("ids", out _));
        Assert.False(args.TryGetGuidArray("whens", out _));
    }

    /// <summary>The raw literal is returned when the name exists; a missing name returns false.</summary>
    [Fact]
    public void TryGetRaw_ReturnsTrueOnlyForAPresentName_Test()
    {
        PredicateArguments args = Sample();

        Assert.True(args.TryGetRaw("count", out LiteralValue raw));
        Assert.Equal(LiteralValue.OfInt64(3), raw);
        Assert.False(args.TryGetRaw("absent", out _));
    }

    /// <summary>The throwing getters keep throwing for a missing name and a wrong kind.</summary>
    [Fact]
    public void Get_MissingNameOrWrongKind_StillThrows_Test()
    {
        PredicateArguments args = Sample();

        Assert.Throws<KeyNotFoundException>(() => args.GetString("absent"));
        Assert.Throws<InvalidOperationException>(() => args.GetString("count"));
    }

    private static PredicateArguments Sample()
    {
        return new(
            new Dictionary<string, LiteralValue>
            {
                ["role"] = LiteralValue.OfString("Y"),
                ["count"] = LiteralValue.OfInt64(3),
                ["ratio"] = LiteralValue.OfDecimal(0.5m),
                ["active"] = LiteralValue.OfBoolean(true),
                ["when"] = LiteralValue.OfDateTimeOffset(Moment),
                ["id"] = LiteralValue.OfGuid(Id),
                ["roles"] = LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a"), LiteralValue.OfString("b")]),
                ["counts"] = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]),
                ["ratios"] = LiteralValue.OfArray(LiteralKind.Decimal, [LiteralValue.OfDecimal(0.5m)]),
                ["flags"] = LiteralValue.OfArray(LiteralKind.Boolean, [LiteralValue.OfBoolean(false)]),
                ["whens"] = LiteralValue.OfArray(LiteralKind.DateTimeOffset, [LiteralValue.OfDateTimeOffset(Moment)]),
                ["ids"] = LiteralValue.OfArray(LiteralKind.Guid, [LiteralValue.OfGuid(Id)]),
            }
        );
    }
}
