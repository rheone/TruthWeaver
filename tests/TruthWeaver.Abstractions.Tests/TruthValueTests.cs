namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

/// <summary>
/// The zero value of <see cref="TruthValue"/> is <see cref="TruthValue.Unknown"/>, so a value that nobody set (a field, a
/// new array element, a defaulted parameter) never reads as a definite answer.
/// </summary>
public sealed class TruthValueTests
{
    /// <summary><c>default(TruthValue)</c> is <c>Unknown</c>.</summary>
    [Fact]
    public void Default_OfTruthValue_IsUnknown_Test()
    {
        const TruthValue value = default;

        Assert.Equal(TruthValue.Unknown, value);
    }

    /// <summary>Every element of a new <see cref="TruthValue"/> array is <c>Unknown</c> until it is assigned.</summary>
    [Fact]
    public void ArrayElement_NewArray_IsUnknown_Test()
    {
        TruthValue[] values = new TruthValue[3];

        Assert.All(values, value => Assert.Equal(TruthValue.Unknown, value));
    }

    /// <summary>The underlying integers are <c>Unknown = 0</c>, <c>False = 1</c> and <c>True = 2</c>.</summary>
    /// <param name="value">The member under test.</param>
    /// <param name="expected">Its underlying integer.</param>
    [Theory]
    [InlineData(TruthValue.Unknown, 0)]
    [InlineData(TruthValue.False, 1)]
    [InlineData(TruthValue.True, 2)]
    public void UnderlyingValue_EachMember_IsPinned_Test(TruthValue value, int expected)
    {
        int actual = (int)value;

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// The declaration order, which <see cref="Enum.GetValues{TEnum}"/> and an ordinal sort follow, is <c>Unknown</c>,
    /// <c>False</c>, <c>True</c>. It is not the truth order (<c>False &lt; Unknown &lt; True</c>).
    /// </summary>
    [Fact]
    public void GetValues_OfTruthValue_AreInDeclarationOrder_Test()
    {
        TruthValue[] values = Enum.GetValues<TruthValue>();

        Assert.Equal([TruthValue.Unknown, TruthValue.False, TruthValue.True], values);
    }
}
