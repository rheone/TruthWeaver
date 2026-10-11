namespace TruthWeaver.Generators;

using System.Collections;

/// <summary>
/// An immutable array with value equality. The incremental pipeline compares each step result with the previous one
/// to decide whether later steps run again. <see cref="ImmutableArray{T}"/> compares by reference, so a model that
/// holds one never compares equal and the cache never hits.
/// </summary>
/// <typeparam name="T">The element type. Its own equality decides the equality of the array.</typeparam>
/// <param name="items">The elements.</param>
internal readonly struct EquatableArray<T>(ImmutableArray<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> items = items;

    /// <summary>Gets the elements. A default instance has no elements.</summary>
    public ImmutableArray<T> Items => this.items.IsDefault ? [] : this.items;

    /// <summary>Gets the number of elements.</summary>
    public int Count => this.Items.Length;

    /// <summary>Compares two arrays element by element.</summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true"/> when both arrays hold equal elements in the same order.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two arrays element by element.</summary>
    /// <param name="left">The first array.</param>
    /// <param name="right">The second array.</param>
    /// <returns><see langword="true"/> when the arrays differ in length or in an element.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EquatableArray<T> other)
    {
        return this.Items.SequenceEqual(other.Items);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (T item in this.Items)
        {
            hash = unchecked((hash * 31) + item.GetHashCode());
        }

        return hash;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)this.Items).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
