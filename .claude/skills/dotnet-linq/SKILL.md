---
name: dotnet-linq
description: 'Reference for C# LINQ — query syntax and method syntax over IEnumerable<T> (LINQ to Objects) and IQueryable<T> (provider-translated, e.g. EF Core), deferred vs. immediate execution, and the standard query operators, from the original C# 3.0 / .NET Framework 3.5 release through the BCL''s later System.Linq additions: Chunk/MinBy-MaxBy/*By-set-operators/TryGetNonEnumeratedCount/3-way Zip (.NET 6), Order/OrderDescending (.NET 7), Index/CountBy/AggregateBy (.NET 9), LeftJoin/RightJoin/Shuffle (.NET 10), and FullJoin/tuple-returning Join (.NET 11 RC). Use when writing, reviewing, or porting a LINQ query, choosing between query and method syntax, deciding whether a query is IEnumerable or IQueryable, diagnosing deferred-execution bugs (multiple enumeration, closures over loop variables), or gating a LINQ operator by target framework. Covers the pre-C#3 manual-loop fallback (.NET Framework 1.0–2.0) through .NET 11 RC1.'
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# LINQ

One core idea, fixed since C# 3.0 (2007): a small set of generic query operators over
`IEnumerable<T>`/`IQueryable<T>`, callable as query syntax or method syntax, most of them deferred
until enumerated. Everything after C# 3.0 is the BCL adding more operators to that same surface —
`Chunk`, `MinBy`/`MaxBy`, `Order`, `Index`, `CountBy`, `LeftJoin`, and so on are all **target
framework** additions (`net6.0`+, `net7.0`+, ...), not C# language changes; none of them need a
specific `<LangVersion>`, only the matching SDK. The baseline in
[references/csharp3-linq-fundamentals.md](references/csharp3-linq-fundamentals.md) still compiles
unchanged on every later target.

## Quick start (works everywhere, C# 3.0+ / .NET Framework 3.5+)

```csharp
IEnumerable<Order> pendingHighValue = orders
    .Where(o => o.Status == OrderStatus.Pending && o.Total > 500m)
    .OrderByDescending(o => o.Total);

foreach (Order order in pendingHighValue)
{
    Console.WriteLine($"{order.Id}: {order.Total:C}");
}
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets. All tiers after
the baseline are System.Linq/BCL additions gated by **target framework**, paired here with the C#
language version their SDK shipped alongside — not gated by `<LangVersion>` itself (each file says
so explicitly).

| Target | C# / .NET | Reference file |
| --- | --- | --- |
| .NET Framework 1.0 – 2.0 | C# 1.0 – 2.0 | [references/pre-csharp3-manual-filtering.md](references/pre-csharp3-manual-filtering.md) — no LINQ; `Predicate<T>`/manual-loop fallback pattern |
| .NET Framework 3.5+ | C# 3.0+ | [references/csharp3-linq-fundamentals.md](references/csharp3-linq-fundamentals.md) — query/method syntax, `IEnumerable<T>`/`IQueryable<T>`, deferred/immediate execution, standard query operators, expression trees; the universal baseline |
| .NET 6+ | C# 10+ (target framework, not `LangVersion`) | [references/csharp10-by-operators-and-chunking.md](references/csharp10-by-operators-and-chunking.md) — `Chunk`, `MinBy`/`MaxBy`, `DistinctBy`/`UnionBy`/`IntersectBy`/`ExceptBy`, `TryGetNonEnumeratedCount`, 3-way `Zip` |
| .NET 7+ | C# 11+ (target framework) | [references/csharp11-order-shorthand.md](references/csharp11-order-shorthand.md) — `Order`/`OrderDescending` shorthand |
| .NET 9+ | C# 13+ (target framework) | [references/csharp13-index-countby-aggregateby.md](references/csharp13-index-countby-aggregateby.md) — `Index`, `CountBy`, `AggregateBy` |
| .NET 10+ | C# 14+ (target framework) | [references/csharp14-leftjoin-rightjoin-shuffle.md](references/csharp14-leftjoin-rightjoin-shuffle.md) — `LeftJoin`, `RightJoin`, `Shuffle` |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | C# 15 (target framework) | [references/csharp15-fulljoin-tuple-joins.md](references/csharp15-fulljoin-tuple-joins.md) — `FullJoin`, tuple-returning `Join`/`GroupJoin` |

## Specialized patterns

- [specialized/ienumerable-vs-iqueryable-execution.md](specialized/ienumerable-vs-iqueryable-execution.md) — in-process LINQ to Objects vs. provider-translated `IQueryable<T>` (e.g. EF Core), and why a lambda that works against one can throw against the other
- [specialized/deferred-execution-pitfalls.md](specialized/deferred-execution-pitfalls.md) — multiple enumeration, mutated-source re-reads, closures over `for`-loop variables, side effects inside `Select`
- [specialized/ordering-grouping-joining.md](specialized/ordering-grouping-joining.md) — `OrderBy`/`ThenBy`, `GroupBy`, and the full `Join`/`LeftJoin`/`RightJoin`/`FullJoin`/`GroupJoin` family worked through together
- [specialized/testing-linq-for-test-data-and-assertions.md](specialized/testing-linq-for-test-data-and-assertions.md) — using LINQ to build test fixtures and write sequence/order-independent assertions
