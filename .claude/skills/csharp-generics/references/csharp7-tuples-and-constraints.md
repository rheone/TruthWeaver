# Value Tuples and New Constraint Kinds (C# 7.0 – 7.3 / .NET Core 1.0 – .NET Framework 4.7.2)

C# 7.x (2017–2018) added value tuples — themselves a generic type family — and three new
constraint kinds that close gaps left open since C# 2.0.

## Value tuples as generics

`(T1, T2, ...)` syntax is sugar over the generic `System.ValueTuple<...>` struct family
(`ValueTuple<T1>` through `ValueTuple<T1, ..., T7, TRest>` for more than seven elements):

```csharp
public static (int Min, int Max) MinMax(IEnumerable<int> values) =>
    (values.Min(), values.Max());

var (min, max) = MinMax(numbers); // deconstruction
```

On .NET Framework (pre-4.7) or .NET Standard 1.x, this needs the `System.ValueTuple` NuGet
package; from .NET Framework 4.7 / .NET Core 1.0 onward it's part of the shared framework.

Generic methods can return or accept tuples of type parameters directly:

```csharp
public static (T First, T Second) FirstTwo<T>(this IList<T> list) => (list[0], list[1]);
```

## New constraints (C# 7.3)

| Constraint | Meaning |
| --- | --- |
| `where T : unmanaged` | `T` is a non-nullable value type with no reference-type fields anywhere in its layout (recursively) — usable with pointers and `stackalloc` |
| `where T : Enum` | `T` is an enum type |
| `where T : Delegate` | `T` is a delegate type |

```csharp
public static unsafe T* AllocateOnStack<T>(int count) where T : unmanaged
{
    T* buffer = stackalloc T[count];
    return buffer;
}

public static string DescribeFlags<T>(T value) where T : Enum =>
    string.Join(", ", Enum.GetValues(typeof(T)).Cast<T>().Where(v => value.HasFlag(v)));
```

C# 7.3 also allows `==`/`!=` between two values of an unconstrained type parameter, provided both
operands' runtime types support the operator — checked at compile time via the constraint, not
inferred implicitly for every `T`.

## Basic use case

```csharp
public static class RetryPolicy
{
    public static (bool Succeeded, T? Result) TryExecute<T>(Func<T> operation, int maxAttempts)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return (true, operation());
            }
            catch when (attempt < maxAttempts)
            {
            }
        }
        return (false, default);
    }
}
```

## Advanced use case: `unmanaged` for interop buffers

```csharp
public static Span<byte> ToBytes<T>(ref T value) where T : unmanaged =>
    MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
```

Only compiles because `unmanaged` guarantees `T` has a fixed, blittable memory layout — without
the constraint, the compiler can't prove that's safe for an arbitrary `T`.

## Fallback

Value tuples: use `System.Tuple<T1, T2, ...>` (a reference type, C# 2.0-era generics, since .NET
Framework 4.0) instead of `(T1, T2)` — heap-allocated and without named-element deconstruction,
but works everywhere generics do. `unmanaged`/`Enum`/`Delegate` constraints: no equivalent before
C# 7.3 — the nearest substitute is `where T : struct` (weaker; permits value types with reference
fields) or no constraint at all with a runtime type check.
