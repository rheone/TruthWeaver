# `LeftJoin`, `RightJoin`, `Shuffle` (.NET 10)

.NET 10 (C# 14, GA November 11, 2025) added `Enumerable.LeftJoin`, `Enumerable.RightJoin`, and
`Enumerable.Shuffle` — plus `Queryable`/`AsyncEnumerable` counterparts for the joins. Same pattern
as the three prior tiers: a **BCL addition gated by target framework** (`net10.0`+), not by
`<LangVersion>`.

## Syntax

```csharp
IEnumerable<(Customer Customer, Order? Order)> withMissing =
    customers.LeftJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => (c, o));

IEnumerable<Order> shuffled = orders.Shuffle();
```

## Basic use case: outer join without `DefaultIfEmpty`

```csharp
IEnumerable<(Customer Customer, Order? Order)> customersWithLatestOrder = customers.LeftJoin(
    orders,
    outerKeySelector: c => c.Id,
    innerKeySelector: o => o.CustomerId,
    resultSelector: (customer, order) => (customer, order));

foreach (var (customer, order) in customersWithLatestOrder)
{
    Console.WriteLine(order is null
        ? $"{customer.Name}: no orders"
        : $"{customer.Name}: last order {order.Id}");
}
```

Before .NET 10, a left outer join needed `GroupJoin` followed by `SelectMany` with
`DefaultIfEmpty()` — correct, but the three-operator combination obscures the intent and is easy
to get subtly wrong (forgetting `DefaultIfEmpty()` silently turns it back into an inner join).
`LeftJoin` includes every element from the first sequence even when no match exists in the second,
passing `default` for the unmatched side — hence `Order?` above. `RightJoin` is the mirror image:
every element from the *second* sequence is guaranteed present, with the first-sequence match
(or `default`) passed to the result selector.

## Basic use case: `Shuffle`

```csharp
IEnumerable<Question> randomOrder = questionBank.Shuffle();
```

`Shuffle<TSource>()` returns the sequence reordered using `Random.Shared` — a non-cryptographic,
non-seedable shuffle intended for general-purpose randomization (quiz question order, a shuffled
playlist), not for anything requiring reproducibility or cryptographic unpredictability. There is
no overload accepting a caller-supplied `Random` as of .NET 10; a fixed-seed or verifiable shuffle
still needs a hand-rolled Fisher–Yates over the materialized sequence.

## Requirements and restrictions

- `LeftJoin`/`RightJoin`/`Shuffle` require targeting `net10.0`+.
- Query syntax has no `left join`/`right join` keyword — as with every LINQ operator introduced
  after C# 3.0, these are method-syntax only.
- `LeftJoin`'s result selector's inner-side parameter (and `RightJoin`'s outer-side parameter) can
  be `default` — always guard against `null` for a reference type, or check the value type's
  default meaningfully, before using it.

## Fallback

Below `net10.0`: `customers.GroupJoin(orders, c => c.Id, o => o.CustomerId, (c, os) => (c, os))
.SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => (x.c, o))` for `LeftJoin`; the symmetric
`GroupJoin`/`SelectMany`/`DefaultIfEmpty` combination with sequences swapped for `RightJoin`; a
hand-rolled Fisher–Yates shuffle over a materialized `List<T>` for `Shuffle`. See
[csharp3-linq-fundamentals.md](csharp3-linq-fundamentals.md) for `GroupJoin`, `SelectMany`, and
`DefaultIfEmpty`, and
[specialized/ordering-grouping-joining.md](../specialized/ordering-grouping-joining.md) for the
join operators worked through together.
