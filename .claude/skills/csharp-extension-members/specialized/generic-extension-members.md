# Generic Extension Members

Generics extend to both extension syntaxes. This file assumes familiarity with
[csharp3-extension-methods.md](../references/csharp3-extension-methods.md) and, for the block
syntax, [csharp14-extension-members.md](../references/csharp14-extension-members.md).

## Classic generic extension methods (C# 3.0+)

Type parameters are declared per method, inferred from the receiver or an argument:

```csharp
public static class CollectionExtensions
{
    public static bool IsEmpty<T>(this IEnumerable<T> source) => !source.Any();

    public static Dictionary<TKey, TValue> ToDictionarySafe<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var kvp in source)
        {
            result[kvp.Key] = kvp.Value; // last-write-wins, no throw on duplicate key
        }
        return result;
    }
}
```

### Multiple type parameters and constraint combinations

```csharp
public static class MapExtensions
{
    public static IEnumerable<TResult> ZipWith<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst, TSecond, TResult> selector)
        where TResult : notnull
        => first.Zip(second, selector);

    public static bool AllEqual<T>(this IEnumerable<T> source) where T : IEquatable<T>
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            return true;
        }
        var first = e.Current;
        while (e.MoveNext())
        {
            if (!first.Equals(e.Current))
            {
                return false;
            }
        }
        return true;
    }
}
```

### Extending an already-generic type (open vs. closed)

```csharp
// open: works for every TKey/TValue
public static class DictionaryExtensions
{
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key)
        where TKey : notnull =>
        map.TryGetValue(key, out var value) ? value : default;
}

// closed: only for this exact instantiation
public static class OrderIdMapExtensions
{
    public static bool HasPendingOrders(this Dictionary<OrderId, Order> orders) =>
        orders.Values.Any(o => o.Status == OrderStatus.Pending);
}
```

## Generic extension blocks (C# 14+)

The type parameter lives on the block, not the member — every member inside shares it:

```csharp
public static class ReadOnlyListExtensions
{
    extension<T>(IReadOnlyList<T> items) where T : IComparable<T>
    {
        public T? Max() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) >= 0 ? a : b);
        public T? Min() => items.Count == 0 ? default : items.Aggregate((a, b) => a.CompareTo(b) <= 0 ? a : b);
        public bool IsSorted => items.Zip(items.Skip(1), (a, b) => a.CompareTo(b) <= 0).All(ok => ok);
    }
}
```

`Max`, `Min`, and `IsSorted` all share the same `T` and the same `where T : IComparable<T>`
constraint declared once on the block — this is the main ergonomic win over classic syntax when a
receiver has many related generic members, versus repeating `<T> where T : IComparable<T>` on
three separate classic methods.

### Static generic extension members

A block can declare a generic parameter over the receiver *type itself* (no instance), useful for
constraint-based static members. See
[specialized/static-extension-members.md](static-extension-members.md#advanced-generic-constrained-static-member-interface-you-dont-control)
for the fully worked example — declaring `extension<T>(T) where T : IParsable<T> { public static T
ParseOrDefault(...) }` and consuming it from inside another generic method via `T.ParseOrDefault(...)`.

## Fallback

A generic classic extension method (top half of this file) compiles on any target from .NET
Framework 3.5 onward — see [pre-csharp3-no-extensions.md](../references/pre-csharp3-no-extensions.md)
for the .NET Framework 1.0–2.0 static-helper equivalent. A generic extension **block** requires C#
14/.NET 10 — port it down by turning each block member back into a classic method with `<T>`
repeated on every method and the receiver as `this T ...` (or, for the static-block case, a
differently-named static helper method, since classic syntax has no static extension members at
all).
