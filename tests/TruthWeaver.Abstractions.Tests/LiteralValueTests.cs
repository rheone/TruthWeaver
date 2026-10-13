namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class LiteralValueTests
{
    public static TheoryData<LiteralKind> AllKinds()
    {
        return new(
            LiteralKind.String,
            LiteralKind.Int64,
            LiteralKind.Decimal,
            LiteralKind.Boolean,
            LiteralKind.DateTimeOffset,
            LiteralKind.Guid,
            LiteralKind.Int64Array
        );
    }

    /// <summary>Scalar factories round trip through their matching accessor.</summary>
    [Fact]
    public void ScalarFactory_MatchingAccessor_RoundTripsValue_Test()
    {
        Assert.Equal("Y", LiteralValue.OfString("Y").AsString());
        Assert.Equal(42L, LiteralValue.OfInt64(42).AsInt64());
        Assert.Equal(1.5m, LiteralValue.OfDecimal(1.5m).AsDecimal());
        Assert.True(LiteralValue.OfBoolean(true).AsBoolean());

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.Equal(now, LiteralValue.OfDateTimeOffset(now).AsDateTimeOffset());

        Guid guid = Guid.NewGuid();
        Assert.Equal(guid, LiteralValue.OfGuid(guid).AsGuid());
    }

    /// <summary>Reading an accessor for the wrong kind throws.</summary>
    [Fact]
    public void Accessor_WrongKind_Throws_Test()
    {
        LiteralValue stringValue = LiteralValue.OfString("Y");

        Assert.Throws<InvalidOperationException>(() => stringValue.AsInt64());
        Assert.Throws<InvalidOperationException>(() => stringValue.AsArray());
    }

    /// <summary>String literal comparison is case sensitive.</summary>
    [Fact]
    public void Equals_StringsDifferingInCase_AreNotEqual_Test()
    {
        LiteralValue lower = LiteralValue.OfString("y");
        LiteralValue upper = LiteralValue.OfString("Y");

        Assert.NotEqual(lower, upper);
    }

    /// <summary>The equality operator returns true for equal literals of the same kind.</summary>
    [Fact]
    public void OperatorEquals_EqualLiteralsOfSameKind_ReturnsTrue_Test()
    {
        LiteralValue left = LiteralValue.OfInt64(42);
        LiteralValue right = LiteralValue.OfInt64(42);

        Assert.True(left == right);
        Assert.False(left != right);
    }

    /// <summary>The equality operator returns false for unequal literals of the same kind.</summary>
    [Fact]
    public void OperatorEquals_UnequalLiteralsOfSameKind_ReturnsFalse_Test()
    {
        LiteralValue left = LiteralValue.OfInt64(42);
        LiteralValue right = LiteralValue.OfInt64(43);

        Assert.False(left == right);
        Assert.True(left != right);
    }

    /// <summary>Equals returns false when kinds differ, without inspecting payloads.</summary>
    [Fact]
    public void Equals_DifferentKinds_ReturnsFalseWithoutInspectingPayloads_Test()
    {
        LiteralValue stringValue = LiteralValue.OfString("42");
        LiteralValue int64Value = LiteralValue.OfInt64(42);

        Assert.False(stringValue.Equals(int64Value));
        Assert.False(stringValue == int64Value);
        Assert.True(stringValue != int64Value);
    }

    /// <summary>Array construction rejects an element that does not match the declared kind.</summary>
    [Fact]
    public void Array_ElementOfWrongKind_IsRejected_Test()
    {
        Assert.Throws<ArgumentException>(() =>
            LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a"), LiteralValue.OfInt64(1)])
        );
    }

    /// <summary>Array construction rejects an array kind as the declared element kind.</summary>
    [Fact]
    public void Array_ArrayKindAsElementKind_IsRejected_Test()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.OfArray(LiteralKind.StringArray, []));
    }

    /// <summary>Arrays are order sensitive and AsArray reports their elements.</summary>
    [Fact]
    public void AsArray_Arrays_AreOrderSensitiveAndReportElements_Test()
    {
        LiteralValue left = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);
        LiteralValue right = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(2), LiteralValue.OfInt64(1)]);

        Assert.NotEqual(left, right);
        Assert.Equal([1L, 2L], left.AsArray().Select(v => v.AsInt64()));
    }

    /// <summary>ToArrayKind and ToElementKind are inverses for every scalar kind.</summary>
    [Theory]
    [InlineData(LiteralKind.String, LiteralKind.StringArray)]
    [InlineData(LiteralKind.Int64, LiteralKind.Int64Array)]
    [InlineData(LiteralKind.Decimal, LiteralKind.DecimalArray)]
    [InlineData(LiteralKind.Boolean, LiteralKind.BooleanArray)]
    [InlineData(LiteralKind.DateTimeOffset, LiteralKind.DateTimeOffsetArray)]
    [InlineData(LiteralKind.Guid, LiteralKind.GuidArray)]
    public void ToArrayKind_EveryScalarKind_RoundTripsWithToElementKind_Test(LiteralKind scalar, LiteralKind array)
    {
        Assert.Equal(array, LiteralValue.ToArrayKind(scalar));
        Assert.Equal(scalar, LiteralValue.ToElementKind(array));
    }

    /// <summary>ToArrayKind rejects an array kind as input.</summary>
    [Fact]
    public void ToArrayKind_ArrayKindInput_IsRejected_Test()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.ToArrayKind(LiteralKind.StringArray));
    }

    /// <summary>ToElementKind rejects a scalar kind as input.</summary>
    [Fact]
    public void ToElementKind_ScalarKindInput_IsRejected_Test()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.ToElementKind(LiteralKind.String));
    }

    /// <summary>ToString renders a string literal as a quoted DSL literal with escapes.</summary>
    [Fact]
    public void ToString_StringLiteral_RendersQuotedDslLiteralWithEscapes_Test()
    {
        LiteralValue value = LiteralValue.OfString("a \"quoted\" \\ value");

        Assert.Equal("\"a \\\"quoted\\\" \\\\ value\"", value.ToString());
    }

    /// <summary>ToString renders a boolean literal in lowercase.</summary>
    [Fact]
    public void ToString_BooleanLiteral_RendersLowercase_Test()
    {
        Assert.Equal("true", LiteralValue.OfBoolean(true).ToString());
        Assert.Equal("false", LiteralValue.OfBoolean(false).ToString());
    }

    /// <summary>ToString renders an array literal as a bracketed, comma separated list.</summary>
    [Fact]
    public void ToString_ArrayLiteral_RendersBracketedCommaList_Test()
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);

        Assert.Equal("[1, 2]", array.ToString());
    }

    /// <summary>GetHashCode does not throw and matches for equal literals of every kind.</summary>
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void GetHashCode_EqualLiteralsOfEveryKind_DoesNotThrowAndMatches_Test(LiteralKind kind)
    {
        LiteralValue left = MakeSample(kind);
        LiteralValue right = MakeSample(kind);

        Assert.True(left.Equals(right));

        int leftHash = left.GetHashCode();
        int rightHash = right.GetHashCode();

        Assert.Equal(leftHash, rightHash);
    }

    /// <summary>Literal values work as hash set members across kinds.</summary>
    [Fact]
    public void GetHashCode_HashSetMembership_WorksAcrossKinds_Test()
    {
        LiteralValue arrayValue = LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a")]);

        HashSet<LiteralValue> set =
        [
            LiteralValue.OfString("Y"),
            LiteralValue.OfInt64(42),
            LiteralValue.OfDecimal(1.5m),
            LiteralValue.OfBoolean(true),
            LiteralValue.OfDateTimeOffset(DateTimeOffset.UtcNow),
            LiteralValue.OfGuid(Guid.NewGuid()),
            arrayValue,
        ];

        Assert.Contains(arrayValue, set);
        Assert.DoesNotContain(LiteralValue.OfString("N"), set);
    }

    private static LiteralValue MakeSample(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => LiteralValue.OfString("Y"),
            LiteralKind.Int64 => LiteralValue.OfInt64(42),
            LiteralKind.Decimal => LiteralValue.OfDecimal(1.5m),
            LiteralKind.Boolean => LiteralValue.OfBoolean(true),
            LiteralKind.DateTimeOffset => LiteralValue.OfDateTimeOffset(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            LiteralKind.Guid => LiteralValue.OfGuid(new Guid("11111111-1111-1111-1111-111111111111")),
            LiteralKind.Int64Array => LiteralValue.OfArray(
                LiteralKind.Int64,
                [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported kind for this test."),
        };
    }
}
