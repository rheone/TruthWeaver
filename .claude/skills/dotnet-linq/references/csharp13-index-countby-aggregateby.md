# `Index`, `CountBy`, `AggregateBy` (.NET 9)

.NET 9 (C# 13, GA November 12, 2024) added `Enumerable.Index`, `Enumerable.CountBy`, and
`Enumerable.AggregateBy`. As with the previous two tiers, these are **BCL additions gated by
target framework** (`net9.0`+), not by `<LangVersion>`.

## Syntax

```csharp
foreach ((int index, Order order) in orders.Index()) { }

IEnumerable<KeyValuePair<OrderStatus, int>> countsByStatus = orders.CountBy(o => o.Status);

IEnumerable<KeyValuePair<OrderStatus, decimal>> totalsByStatus =
    orders.AggregateBy(o => o.Status, seed: 0m, (sum, o) => sum + o.Total);
```

## Basic use case: index-aware iteration without a manual counter

```csharp
foreach ((int index, Order order) in orders.Index())
{
    Console.WriteLine($"{index}: {order.Id}");
}
```

Before .NET 9, the common workaround was `orders.Select((order, index) => (index, order))` — a
`Select` overload with an index parameter that most readers have to double-check the argument
order of. `Index()` names the intent directly and returns `IEnumerable<(int Index, TSource Item)>`.

## Basic use case: counting per key without materializing groups

```csharp
IEnumerable<KeyValuePair<OrderStatus, int>> statusCounts = orders.CountBy(o => o.Status);

foreach (var (status, count) in statusCounts)
{
    Console.WriteLine($"{status}: {count}");
}
```

Before .NET 9: `orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() })` —
correct, but it builds an intermediate grouping (allocating a collection per key) purely to throw
away everything except the count. `CountBy` computes the tally in one pass with no group
materialization.

## Advanced use case: `AggregateBy` for a per-key running aggregate

```csharp
IEnumerable<KeyValuePair<OrderStatus, decimal>> revenueByStatus = orders.AggregateBy(
    keySelector: o => o.Status,
    seed: 0m,
    (runningTotal, order) => runningTotal + order.Total);
```

`AggregateBy` is `GroupBy(...).Select(g => g.Aggregate(...))` collapsed into one pass, generic over
the accumulator type the same way `Aggregate` itself is — here `TAccumulate` is `decimal`, but any
`Func<TAccumulate, TSource, TAccumulate>` folds. A `seed` that must be computed per key (rather
than a constant) has a second overload taking `Func<TKey, TAccumulate> seedSelector` instead of a
plain `seed` value.

## Requirements and restrictions

- All three require targeting `net9.0`+.
- `CountBy`/`AggregateBy` return `IEnumerable<KeyValuePair<TKey, TValue>>`, not a `Dictionary` —
  materialize with `.ToDictionary(kv => kv.Key, kv => kv.Value)` if key lookup is needed afterward.
- `Index()` is unrelated to `ElementAt`/`ElementAtOrDefault` — it doesn't look up a single element
  by position, it decorates every element of the sequence with its position while preserving
  deferred execution over the whole thing.

## Fallback

Below `net9.0`: `Select((item, i) => (i, item))` for `Index()`; `GroupBy(...).Select(g => new {
g.Key, Count = g.Count() })` for `CountBy`; `GroupBy(...).Select(g => new { g.Key, Value =
g.Aggregate(seed, func) })` for `AggregateBy`. See
[csharp3-linq-fundamentals.md](csharp3-linq-fundamentals.md) for `GroupBy`, `Select`, and
`Aggregate` themselves.
