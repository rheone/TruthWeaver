# Generic Math: Numeric Abstractions

Deep dive into [csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md)'s
generic-math introduction — writing algorithms once, generic over every numeric type, instead of
once per type or via `dynamic`/`double`-and-cast tricks.

## The problem generic math solves

Before C# 11, a "sum a sequence of numbers" helper needed either one overload per numeric type,
or a cast through `double` that loses precision for `decimal`/`long`:

```csharp
// pre-C#11: one overload per type, or lossy double-casting
public static int Sum(IEnumerable<int> values) => values.Aggregate(0, (a, b) => a + b);
public static double Sum(IEnumerable<double> values) => values.Aggregate(0.0, (a, b) => a + b);
public static decimal Sum(IEnumerable<decimal> values) => values.Aggregate(0m, (a, b) => a + b);
// ... repeat per type
```

## Generic math version

```csharp
public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
{
    T total = T.Zero;
    foreach (var value in values)
    {
        total += value;
    }
    return total;
}

int intTotal = Sum(new[] { 1, 2, 3 });
decimal decimalTotal = Sum(new[] { 1.5m, 2.5m });
```

One method, no boxing, no precision loss — `T.Zero` and `+=` both resolve through
`INumber<T>`'s `static abstract` members, so the compiler generates a specialized version of
`Sum` per concrete `T` at JIT time (the same mechanism as any other generic method over a value
type — no runtime dispatch overhead).

## Key BCL interfaces

| Interface | Provides |
| --- | --- |
| `INumber<TSelf>` | the full arithmetic/comparison contract most algorithms want — implemented by `int`, `long`, `double`, `decimal`, `float`, etc. |
| `IAdditionOperators<TSelf, TOther, TResult>` | just `+`, for algorithms that need nothing else |
| `IComparisonOperators<TSelf, TOther, TResult>` | `<`, `>`, `<=`, `>=` |
| `IParsable<TSelf>` | `TSelf.TryParse(string, ...)` — same static-member-through-a-constraint shape as C# 14 static extension members |
| `IMinMaxValue<TSelf>` | `TSelf.MinValue` / `TSelf.MaxValue` |

Constrain to the narrowest interface the algorithm actually needs — `IAdditionOperators<T, T, T>`
if all you do is add, not the much larger `INumber<T>` — the same "smallest sufficient interface"
principle as any other constraint choice.

## Advanced use case: a generic statistics helper

```csharp
public static class Statistics
{
    public static T Average<T>(IEnumerable<T> values) where T : INumber<T>
    {
        T sum = T.Zero;
        int count = 0;
        foreach (var value in values)
        {
            sum += value;
            count++;
        }
        return count == 0 ? T.Zero : sum / T.CreateChecked(count);
    }
}
```

`T.CreateChecked(count)` converts the `int count` into `T` — necessary because `count` isn't
itself a `T`, and there's no implicit `int → T` conversion for an arbitrary numeric type.
`INumericBase<TSelf>` (a base of `INumber<TSelf>`) supplies `CreateChecked`/`CreateSaturating`/
`CreateTruncating` for exactly this cross-type-conversion need.

## Fallback

No equivalent before C# 11 — write one overload per concrete numeric type, or accept a
`Func<T, T, T>` operation delegate passed in alongside an unconstrained `T` (see
[csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md#fallback)).
