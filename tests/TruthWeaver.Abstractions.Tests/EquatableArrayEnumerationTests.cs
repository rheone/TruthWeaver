namespace TruthWeaver.Abstractions.Tests;

using System.Collections;
using TruthWeaver.Abstractions;

public sealed class EquatableArrayEnumerationTests
{
    /// <summary>The non-generic enumerator yields the same elements in order.</summary>
    [Fact]
    public void GetEnumerator_NonGenericEnumeration_YieldsElementsInOrder_Test()
    {
        IEnumerable array = new EquatableArray<int>([1, 2, 3]);

        // Cast<T> short-circuits for a typed source, so walk the untyped enumerator by hand.
        List<object> items = [];
        IEnumerator enumerator = array.GetEnumerator();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        Assert.Equal([1, 2, 3], items.Cast<int>());
    }

    /// <summary>A default (uninitialized) array enumerates as empty.</summary>
    [Fact]
    public void GetEnumerator_DefaultArray_YieldsNothing_Test()
    {
        EquatableArray<int> array = default;

        Assert.Empty(array);
    }
}
