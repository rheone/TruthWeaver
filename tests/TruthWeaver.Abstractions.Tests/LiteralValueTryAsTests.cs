namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class LiteralValueTryAsTests
{
    private static readonly DateTimeOffset Moment = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

    /// <summary>A value of the requested kind returns true and the wrapped value.</summary>
    [Fact]
    public void TryAs_MatchingKind_ReturnsTrueWithTheValue_Test()
    {
        Assert.True(LiteralValue.OfString("Y").TryAsString(out string? text));
        Assert.Equal("Y", text);
        Assert.True(LiteralValue.OfInt64(42).TryAsInt64(out long integer));
        Assert.Equal(42L, integer);
        Assert.True(LiteralValue.OfDecimal(1.5m).TryAsDecimal(out decimal number));
        Assert.Equal(1.5m, number);
        Assert.True(LiteralValue.OfBoolean(true).TryAsBoolean(out bool flag));
        Assert.True(flag);
        Assert.True(LiteralValue.OfDateTimeOffset(Moment).TryAsDateTimeOffset(out DateTimeOffset moment));
        Assert.Equal(Moment, moment);
        Assert.True(LiteralValue.OfGuid(Id).TryAsGuid(out Guid id));
        Assert.Equal(Id, id);
    }

    /// <summary>A value of a different kind returns false and a default value, and never throws.</summary>
    [Fact]
    public void TryAs_WrongKind_ReturnsFalseWithoutThrowing_Test()
    {
        LiteralValue text = LiteralValue.OfString("Y");

        Assert.False(text.TryAsInt64(out long integer));
        Assert.Equal(0L, integer);
        Assert.False(text.TryAsDecimal(out _));
        Assert.False(text.TryAsBoolean(out _));
        Assert.False(text.TryAsDateTimeOffset(out _));
        Assert.False(text.TryAsGuid(out _));
        Assert.False(LiteralValue.OfInt64(1).TryAsString(out string? none));
        Assert.Null(none);
    }

    /// <summary>An array value returns true and its elements; a scalar returns false.</summary>
    [Fact]
    public void TryAsArray_ReturnsTrueOnlyForAnArrayKind_Test()
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);

        Assert.True(array.TryAsArray(out EquatableArray<LiteralValue> elements));
        Assert.Equal(2, elements.Count);
        Assert.False(LiteralValue.OfInt64(1).TryAsArray(out _));
    }
}
