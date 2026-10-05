namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Evaluation;

/// <summary>How a variable's value narrows to the kind a predicate argument declares.</summary>
public sealed class VariableConversionTests
{
    /// <summary>A whole decimal at the largest long narrows to an integer argument.</summary>
    [Fact]
    public void TryConvert_DecimalAtLongMaxValue_NarrowsToInt64_Test()
    {
        bool converted = VariableConversion.TryConvert(
            [LiteralValue.OfDecimal(long.MaxValue)],
            LiteralKind.Int64,
            out LiteralValue value,
            out _,
            out _
        );

        Assert.True(converted);
        Assert.Equal(long.MaxValue, value.AsInt64());
    }

    /// <summary>A whole decimal at the smallest long narrows to an integer argument.</summary>
    [Fact]
    public void TryConvert_DecimalAtLongMinValue_NarrowsToInt64_Test()
    {
        bool converted = VariableConversion.TryConvert(
            [LiteralValue.OfDecimal(long.MinValue)],
            LiteralKind.Int64,
            out LiteralValue value,
            out _,
            out _
        );

        Assert.True(converted);
        Assert.Equal(long.MinValue, value.AsInt64());
    }
}
