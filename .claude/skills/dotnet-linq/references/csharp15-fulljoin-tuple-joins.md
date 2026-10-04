# `FullJoin`, Tuple-Returning `Join`/`GroupJoin` (.NET 11)

**RC caveat:** .NET 11 is at Release Candidate 1 as of September 2026 (RC1 carries a go-live
license); GA is expected November 2026. The APIs below are documented on the official .NET 11
"what's new" page as of RC1, but, as with any pre-GA release, details could still shift before
GA — re-verify against the GA release notes before relying on this tier in a shipping codebase.

.NET 11 (C# 15) extends the join operators further: `Enumerable.FullJoin`, plus tuple-returning
overloads of `Join` and `GroupJoin` — across `Enumerable`, `Queryable`, and `AsyncEnumerable`. Same
BCL-addition pattern as the three prior tiers: gated by target framework (`net11.0`+), not by
`<LangVersion>`.

## Syntax

```csharp
IEnumerable<(Customer? Customer, Order? Order)> everyRow =
    customers.FullJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => (c, o));

IEnumerable<(Customer Customer, Order Order)> matchedPairs =
    customers.Join(orders, c => c.Id, o => o.CustomerId); // tuple-returning overload, no result selector
```

## Basic use case: a full outer join in one call

```csharp
IEnumerable<(Customer? Customer, Order? Order)> reconciliation = customers.FullJoin(
    orders,
    outerKeySelector: c => c.Id,
    innerKeySelector: o => o.CustomerId,
    resultSelector: (customer, order) => (customer, order));
```

`FullJoin` includes every row from *both* sequences — matched pairs, customers with no orders
(`Order? Order` is `default`), and orders with no matching customer (`Customer? Customer` is
`default`) — completing the `LeftJoin`/`RightJoin` pair added in .NET 10
([csharp14-leftjoin-rightjoin-shuffle.md](csharp14-leftjoin-rightjoin-shuffle.md)) the way SQL's
`FULL OUTER JOIN` completes `LEFT`/`RIGHT JOIN`. Before .NET 11, a full outer join needed a
`LeftJoin` and a `RightJoin` unioned together with duplicate-matched-pair removal — straightforward
to get wrong.

## Basic use case: `Join`/`GroupJoin` without writing a result selector

```csharp
IEnumerable<(Customer Customer, Order Order)> pairs =
    customers.Join(orders, c => c.Id, o => o.CustomerId);
```

The classic `Join` always required a `resultSelector` lambda — even the trivial
`(c, o) => (c, o)` case had to spell it out. The new overload omits the parameter entirely and
returns the matched pair as a `ValueTuple` directly, mirroring how `Zip`'s tuple-returning overload
already worked since .NET 6.

## Requirements and restrictions

- Requires targeting `net11.0`+ once GA; on RC1, requires the .NET 11 RC1 SDK with a project
  targeting `net11.0`.
- `FullJoin`'s result selector receives `default` on *either* side — always guard both parameters
  for `null`/default before use, not just one as with `LeftJoin`/`RightJoin`.
- As with every join operator, there's no query-syntax keyword — `full join`, like `left join` and
  `right join`, is method-syntax only.

## Fallback

Below `net11.0`: build a full outer join from `LeftJoin` and `RightJoin` (.NET 10+,
[csharp14-leftjoin-rightjoin-shuffle.md](csharp14-leftjoin-rightjoin-shuffle.md)) or, below that,
from `GroupJoin`/`SelectMany`/`DefaultIfEmpty` twice with `Union`/`Concat`-and-deduplicate; write
`resultSelector: (c, o) => (c, o)` explicitly for the classic `Join`/`GroupJoin` instead of the
tuple-returning overload. See
[specialized/ordering-grouping-joining.md](../specialized/ordering-grouping-joining.md) for the
join family worked through end to end.
