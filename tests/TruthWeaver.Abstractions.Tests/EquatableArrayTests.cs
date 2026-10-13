namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class EquatableArrayTests
{
    /// <summary>Arrays with the same elements in the same order are equal.</summary>
    [Fact]
    public void Equals_SameElementsInSameOrder_AreEqual_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([1, 2, 3]);

        Assert.True(left == right);
        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>Arrays with the same elements in a different order are not equal.</summary>
    [Fact]
    public void Equals_SameElementsInDifferentOrder_AreNotEqual_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([3, 2, 1]);

        Assert.True(left != right);
        Assert.False(left.Equals(right));
    }

    /// <summary>The default array and an explicitly empty array are equal and have a count of zero.</summary>
    [Fact]
    public void Default_EmptyArrays_AreEqualWithZeroCount_Test()
    {
        EquatableArray<int> defaultArray = default;
        EquatableArray<int> explicitEmpty = EquatableArray<int>.Empty;

        Assert.Empty(defaultArray);
        Assert.Empty(explicitEmpty);
        Assert.True(defaultArray.Equals(explicitEmpty));
    }

    /// <summary>The indexer and enumeration return elements in source order.</summary>
    [Fact]
    public void Indexer_Enumeration_ReturnsElementsInSourceOrder_Test()
    {
        EquatableArray<string> array = new(["a", "b", "c"]);

        Assert.Equal("a", array[0]);
        Assert.Equal("c", array[2]);
        Assert.Equal(["a", "b", "c"], array);
    }

    /// <summary>The equality operator returns false for arrays with different elements.</summary>
    [Fact]
    public void OperatorEquals_DifferentElements_ReturnsFalse_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([1, 2, 4]);

        Assert.False(left == right);
    }

    /// <summary>The inequality operator negates the equality operator for equal arrays.</summary>
    [Fact]
    public void OperatorNotEquals_EqualArrays_IsNegationOfEquals_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([1, 2, 3]);

        Assert.True(left == right);
        Assert.False(left != right);
    }

    /// <summary>The inequality operator negates the equality operator for unequal arrays.</summary>
    [Fact]
    public void OperatorNotEquals_UnequalArrays_IsNegationOfEquals_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([1, 2, 4]);

        Assert.False(left == right);
        Assert.True(left != right);
    }

    /// <summary>The equality operator returns false when one side is empty and the other is not.</summary>
    [Fact]
    public void OperatorEquals_NonEmptyAgainstEmpty_ReturnsFalse_Test()
    {
        EquatableArray<int> nonEmpty = new([1, 2, 3]);
        EquatableArray<int> empty = EquatableArray<int>.Empty;

        Assert.False(nonEmpty == empty);
        Assert.False(empty == nonEmpty);
    }

    /// <summary>The inequality operator returns true when one side is empty and the other is not.</summary>
    [Fact]
    public void OperatorNotEquals_NonEmptyAgainstEmpty_ReturnsTrue_Test()
    {
        EquatableArray<int> nonEmpty = new([1, 2, 3]);
        EquatableArray<int> empty = EquatableArray<int>.Empty;

        Assert.True(nonEmpty != empty);
        Assert.True(empty != nonEmpty);
    }

    /// <summary>The equality operator returns true when both sides are empty.</summary>
    [Fact]
    public void OperatorEquals_BothSidesEmpty_ReturnsTrue_Test()
    {
        EquatableArray<int> empty = EquatableArray<int>.Empty;
        EquatableArray<int> alsoEmpty = new([]);

        Assert.True(empty == alsoEmpty);
        Assert.False(empty != alsoEmpty);
    }

    /// <summary>Boxed Equals returns true for a boxed array with equal elements.</summary>
    [Fact]
    public void Equals_BoxedArrayWithEqualElements_ReturnsTrue_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        object right = new EquatableArray<int>([1, 2, 3]);

        Assert.True(left.Equals(right));
    }

    /// <summary>Boxed Equals returns false for a boxed array with different elements.</summary>
    [Fact]
    public void Equals_BoxedArrayWithDifferentElements_ReturnsFalse_Test()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        object right = new EquatableArray<int>([1, 2, 4]);

        Assert.False(left.Equals(right));
    }

    /// <summary>Boxed Equals returns false for an unrelated object.</summary>
    [Fact]
    public void Equals_UnrelatedObject_ReturnsFalse_Test()
    {
        EquatableArray<int> array = new([1, 2, 3]);
        object unrelated = "not an array";

        Assert.False(array.Equals(unrelated));
    }

    /// <summary>Boxed Equals returns false for null.</summary>
    [Fact]
    public void Equals_Null_ReturnsFalse_Test()
    {
        EquatableArray<int> array = new([1, 2, 3]);

        Assert.False(array.Equals(null));
    }
}
