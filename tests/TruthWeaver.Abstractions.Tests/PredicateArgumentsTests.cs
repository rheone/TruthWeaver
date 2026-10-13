namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateArgumentsTests
{
    /// <summary>The typed getters return the supplied value.</summary>
    [Fact]
    public void TypedGetters_Supplied_ReturnSuppliedValue_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["role"] = LiteralValue.OfString("Y"),
                ["count"] = LiteralValue.OfInt64(3),
                ["ratio"] = LiteralValue.OfDecimal(0.5m),
                ["active"] = LiteralValue.OfBoolean(true),
            }
        );

        Assert.Equal("Y", args.GetString("role"));
        Assert.Equal(3L, args.GetInt64("count"));
        Assert.Equal(0.5m, args.GetDecimal("ratio"));
        Assert.True(args.GetBool("active"));
    }

    /// <summary>The array getters return elements in source order.</summary>
    [Fact]
    public void Array_Getters_ReturnElementsInSourceOrder_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["roles"] = LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a"), LiteralValue.OfString("b")]),
            }
        );

        Assert.Equal(["a", "b"], args.GetStringArray("roles"));
    }

    /// <summary>GetInt64Array returns elements in source order.</summary>
    [Fact]
    public void GetInt64Array_Elements_ReturnInSourceOrder_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["counts"] = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]),
            }
        );

        Assert.Equal([1L, 2L], args.GetInt64Array("counts"));
    }

    /// <summary>GetDecimalArray returns elements in source order.</summary>
    [Fact]
    public void GetDecimalArray_Elements_ReturnInSourceOrder_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["ratios"] = LiteralValue.OfArray(
                    LiteralKind.Decimal,
                    [LiteralValue.OfDecimal(0.5m), LiteralValue.OfDecimal(1.5m)]
                ),
            }
        );

        Assert.Equal([0.5m, 1.5m], args.GetDecimalArray("ratios"));
    }

    /// <summary>GetBoolArray returns elements in source order.</summary>
    [Fact]
    public void GetBoolArray_Elements_ReturnInSourceOrder_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["flags"] = LiteralValue.OfArray(
                    LiteralKind.Boolean,
                    [LiteralValue.OfBoolean(true), LiteralValue.OfBoolean(false)]
                ),
            }
        );

        Assert.Equal([true, false], args.GetBoolArray("flags"));
    }

    /// <summary>GetDateTimeOffsetArray returns elements in source order.</summary>
    [Fact]
    public void GetDateTimeOffsetArray_Elements_ReturnInSourceOrder_Test()
    {
        DateTimeOffset first = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset second = new(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["dates"] = LiteralValue.OfArray(
                    LiteralKind.DateTimeOffset,
                    [LiteralValue.OfDateTimeOffset(first), LiteralValue.OfDateTimeOffset(second)]
                ),
            }
        );

        Assert.Equal([first, second], args.GetDateTimeOffsetArray("dates"));
    }

    /// <summary>GetGuidArray returns elements in source order.</summary>
    [Fact]
    public void GetGuidArray_Elements_ReturnInSourceOrder_Test()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["ids"] = LiteralValue.OfArray(LiteralKind.Guid, [LiteralValue.OfGuid(first), LiteralValue.OfGuid(second)]),
            }
        );

        Assert.Equal([first, second], args.GetGuidArray("ids"));
    }

    /// <summary>GetStringArray returns an empty list for an empty array argument.</summary>
    [Fact]
    public void GetStringArray_EmptyArrayArgument_ReturnsEmptyList_Test()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue> { ["roles"] = LiteralValue.OfArray(LiteralKind.String, []) }
        );

        Assert.Empty(args.GetStringArray("roles"));
    }

    /// <summary>Requesting a missing argument throws KeyNotFoundException.</summary>
    [Fact]
    public void Requesting_MissingArgument_ThrowsKeyNotFound_Test()
    {
        PredicateArguments args = PredicateArguments.Empty;

        Assert.Throws<KeyNotFoundException>(() => args.GetString("role"));
    }

    /// <summary>A typed getter that does not match the argument kind throws.</summary>
    [Fact]
    public void Requesting_TypedGetterWrongKind_Throws_Test()
    {
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["role"] = LiteralValue.OfString("Y") });

        Assert.Throws<InvalidOperationException>(() => args.GetInt64("role"));
    }

    /// <summary>GetGuid returns the supplied value.</summary>
    [Fact]
    public void GetGuid_Supplied_ReturnsSuppliedValue_Test()
    {
        Guid value = Guid.NewGuid();
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = LiteralValue.OfGuid(value) });

        Assert.Equal(value, args.GetGuid("id"));
    }

    /// <summary>A kind mismatch message names the argument and both kinds.</summary>
    [Fact]
    public void Requesting_KindMismatch_MessageNamesArgumentAndBothKinds_Test()
    {
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = LiteralValue.OfString("not-a-guid") });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => args.GetGuid("id"));

        Assert.Contains("'id'", exception.Message);
        Assert.Contains($"'{LiteralKind.String}'", exception.Message);
        Assert.Contains($"'{LiteralKind.Guid}'", exception.Message);
    }

    /// <summary>GetRaw returns the literal regardless of its kind.</summary>
    [Fact]
    public void GetRaw_AnyKind_ReturnsLiteral_Test()
    {
        LiteralValue value = LiteralValue.OfGuid(Guid.NewGuid());
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = value });

        Assert.Equal(value, args.GetRaw("id"));
    }

    /// <summary>Empty has no arguments.</summary>
    [Fact]
    public void Empty_Default_HasNoArguments_Test()
    {
        Assert.Throws<KeyNotFoundException>(() => PredicateArguments.Empty.GetRaw("anything"));
    }
}
