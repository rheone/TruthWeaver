# Ordering, Grouping, and Joining Together

The three relational-style operator families from
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md#standard-query-operators-linq-to-objects-baseline),
worked through as they're actually combined in practice, plus where the later join operators
(.NET 10's `LeftJoin`/`RightJoin`, .NET 11's `FullJoin`) fit against the C# 3.0 baseline.

## Basic: multi-key ordering with `OrderBy`/`ThenBy`

```csharp
List<Order> sorted = orders
    .OrderBy(o => o.Status)
    .ThenByDescending(o => o.Total)
    .ToList();
```

`OrderBy` returns `IOrderedEnumerable<TSource>`, the type `ThenBy`/`ThenByDescending` are defined
on — chaining `OrderBy(...).OrderBy(...)` instead of `.ThenBy(...)` re-sorts from scratch on the
second key, destroying the first sort instead of refining it. Always reach for `ThenBy` after the
first `OrderBy`/`OrderByDescending`.

## Basic: `GroupBy` producing one sequence per key

```csharp
IEnumerable<IGrouping<OrderStatus, Order>> byStatus = orders.GroupBy(o => o.Status);

foreach (IGrouping<OrderStatus, Order> group in byStatus)
{
    Console.WriteLine($"{group.Key}: {group.Count()} orders, {group.Sum(o => o.Total):C}");
}
```

`IGrouping<TKey, TElement>` is itself `IEnumerable<TElement>` with a `Key` property — every
grouping is a deferred sub-sequence, so `group.Count()`/`group.Sum(...)` above each enumerate that
group's elements. A `GroupBy` overload taking a `resultSelector` skips materializing
`IGrouping<TKey, TElement>` at all when only the aggregate matters:

```csharp
IEnumerable<(OrderStatus Status, int Count, decimal Total)> summary = orders.GroupBy(
    o => o.Status,
    (status, group) => (status, group.Count(), group.Sum(o => o.Total)));
```

(.NET 9's `CountBy`/`AggregateBy` — see
[csharp13-index-countby-aggregateby.md](../references/csharp13-index-countby-aggregateby.md) —
cover the single-aggregate case of this same pattern with less code.)

## Basic: inner `Join`

```csharp
IEnumerable<(Order Order, Customer Customer)> matched = orders.Join(
    customers,
    outerKeySelector: o => o.CustomerId,
    innerKeySelector: c => c.Id,
    resultSelector: (o, c) => (o, c));
```

`Join` is an equijoin only — the key selectors must produce equatable values, and only exact key
matches appear in the result. An order whose `CustomerId` matches no customer is silently dropped;
that's the exact gap `LeftJoin` exists to close.

## Advanced: `GroupJoin` for a one-to-many shape, including the empty-group case

```csharp
IEnumerable<(Customer Customer, IEnumerable<Order> Orders)> withOrders = customers.GroupJoin(
    orders,
    outerKeySelector: c => c.Id,
    innerKeySelector: o => o.CustomerId,
    resultSelector: (c, os) => (c, os));

// customers with zero matching orders still appear, with an empty `Orders` sequence
foreach (var (customer, customerOrders) in withOrders)
{
    Console.WriteLine($"{customer.Name}: {customerOrders.Count()} orders");
}
```

Unlike `Join`, `GroupJoin` keeps every outer element even with zero inner matches — the mismatch is
represented as an empty grouping, not a dropped row. Flattening a `GroupJoin` with `SelectMany` and
`DefaultIfEmpty()` on the inner sequence is exactly how a left outer join was written before .NET
10's `LeftJoin`:

```csharp
IEnumerable<(Customer Customer, Order? Order)> flattenedLeftJoin = customers
    .GroupJoin(orders, c => c.Id, o => o.CustomerId, (c, os) => (c, os))
    .SelectMany(
        x => x.os.DefaultIfEmpty(),
        (x, o) => (x.c, o));
```

## Advanced: choosing among `Join`, `LeftJoin`, `RightJoin`, `FullJoin`

| Need | Operator | Since |
| --- | --- | --- |
| Only matched pairs | `Join` | C# 3.0 |
| Every left-side row, matched or not | `LeftJoin` | .NET 10 |
| Every right-side row, matched or not | `RightJoin` | .NET 10 |
| Every row from both sides, matched or not | `FullJoin` | .NET 11 (RC1 as of Sept 2026) |
| Grouped one-to-many, including empty groups | `GroupJoin` | C# 3.0 |

Details and fallbacks for `LeftJoin`/`RightJoin` are in
[csharp14-leftjoin-rightjoin-shuffle.md](../references/csharp14-leftjoin-rightjoin-shuffle.md);
for `FullJoin`, in
[csharp15-fulljoin-tuple-joins.md](../references/csharp15-fulljoin-tuple-joins.md).

## Fallback

`OrderBy`/`ThenBy`, `GroupBy`, `Join`, and `GroupJoin` are all C# 3.0 baseline — see
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md). `LeftJoin`/`RightJoin`
fall back to the `GroupJoin`/`SelectMany`/`DefaultIfEmpty` pattern shown above on `net6.0`–`net9.0`
targets; `FullJoin` falls back to `LeftJoin` and `RightJoin` combined (.NET 10+) or the same
`GroupJoin` pattern applied twice on older targets — see each operator's own reference file for
the exact fallback code.
