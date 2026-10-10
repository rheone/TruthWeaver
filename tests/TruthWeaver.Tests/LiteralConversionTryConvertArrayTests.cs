namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Ticket 25: <see cref="LiteralConversion.TryConvert"/>'s array-conversion failure paths — a
/// non-array (or elements-less) raw literal presented against an array-typed <see cref="LiteralKind"/>,
/// and an array literal whose elements don't all match the expected element kind.
/// </summary>
public sealed class LiteralConversionTryConvertArrayTests
{
    private static readonly SourceSpan Span = new(0, 0);

    /// <summary>TryConvert against an array kind with a non array literal fails without throwing.</summary>
    [Fact]
    public void TryConvert_ArrayKindNonArrayLiteral_FailsWithoutThrowing_Test()
    {
        RawLiteral raw = RawLiteral.OfNumber("1", Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.Int64Array, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    /// <summary>TryConvert against an array kind with null elements fails without throwing.</summary>
    [Fact]
    public void TryConvert_ArrayKindNullElements_FailsWithoutThrowing_Test()
    {
        RawLiteral raw = new(RawLiteralForm.Array, default, default, default, Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.StringArray, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    /// <summary>TryConvert against an array kind with a mismatched element fails and exposes no partial value.</summary>
    [Fact]
    public void TryConvert_ArrayKindMismatchedElement_FailsWithNoPartialValue_Test()
    {
        RawLiteral raw = RawLiteral.OfArray([RawLiteral.OfNumber("1", Span), RawLiteral.OfString("not-a-number", Span)], Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.Int64Array, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }
}
