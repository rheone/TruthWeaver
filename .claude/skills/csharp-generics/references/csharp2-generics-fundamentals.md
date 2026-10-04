# Generic Fundamentals (C# 2.0+ / .NET Framework 2.0+)

Generics shipped with C# 2.0 (.NET Framework 2.0, 2005) — the same release that added
`Nullable<T>`, iterators, and anonymous methods. Everything in this file is the universal floor:
it compiles unchanged on every later C# version, including 14 and 15.

## Generic types

```csharp
public class Box<T>
{
    public T Value { get; set; }

    public Box(T value) => Value = value;
}

var intBox = new Box<int>(42);
var stringBox = new Box<string>("hello");
```

`Box<int>` and `Box<string>` are **closed constructed types** — `Box<T>` itself, with `T`
unbound, is an **open generic type** and cannot be instantiated directly.

## Generic methods

Type arguments are inferred from the call site when possible; only ambiguous or unconstrained
cases need `<T>` spelled out:

```csharp
public static class Swapper
{
    public static void Swap<T>(ref T left, ref T right)
    {
        (left, right) = (right, left);
    }
}

int a = 1, b = 2;
Swapper.Swap(ref a, ref b);      // T inferred as int
Swapper.Swap<int>(ref a, ref b); // explicit — equivalent
```

## Constraints (`where`)

Constrain a type parameter to widen what the compiler lets the body do:

```csharp
public class Repository<T> where T : class, IEntity, new()
{
    public T CreateDefault() => new T();      // needs `new()`
    public void Validate(T entity) => entity.Validate(); // needs the IEntity constraint
}
```

| Constraint | Meaning |
| --- | --- |
| `where T : struct` | `T` is a non-nullable value type |
| `where T : class` | `T` is a reference type |
| `where T : new()` | `T` has an accessible public parameterless constructor |
| `where T : BaseType` | `T` is `BaseType` or derives from it |
| `where T : IInterface` | `T` implements `IInterface` |
| `where T : U` | `T` is or derives from another type parameter `U` (naked type constraint) |

Multiple constraints on the same parameter combine with commas, in this fixed order:
value-type/reference-type constraint first, then base type, then interfaces, then `new()` last.
The full, version-spanning list (including `unmanaged`, `notnull`, `enum`, `delegate`, and
`allows ref struct`) is in
[specialized/generic-constraints-reference.md](../specialized/generic-constraints-reference.md).

## `default(T)`

Produces the zero value for a value type or `null` for a reference type, when `T` isn't known to
be one or the other:

```csharp
public static T? FirstOrDefault<T>(this IList<T> list) =>
    list.Count > 0 ? list[0] : default(T);
```

(`default` without `(T)` — the target-typed form — needs C# 7.1+; `default(T)` is valid from
C# 2.0.)

## Basic use case: a generic stack

```csharp
public class Stack<T>
{
    private readonly List<T> _items = new();

    public void Push(T item) => _items.Add(item);

    public T Pop()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty.");
        }
        T item = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        return item;
    }

    public int Count => _items.Count;
}
```

## Advanced use case: multiple type parameters and nested generics

```csharp
public class MultiMap<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TValue>> _map = new();

    public void Add(TKey key, TValue value)
    {
        if (!_map.TryGetValue(key, out var values))
        {
            values = new List<TValue>();
            _map[key] = values;
        }
        values.Add(value);
    }

    public IReadOnlyList<TValue> this[TKey key] =>
        _map.TryGetValue(key, out var values) ? values : Array.Empty<TValue>();
}
```

`Dictionary<TKey, List<TValue>>` nests one generic type inside another — no special syntax beyond
ordinary type-argument substitution.

## Fallback

None — C# 2.0 is the floor. Below .NET Framework 2.0 (C# 1.0–1.2), there is no generics mechanism
at all; use `object`-typed collections (`ArrayList`, `Hashtable`) with runtime casts instead.
