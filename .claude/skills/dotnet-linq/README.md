# LINQ

This skill covers C# LINQ: query syntax and method syntax over `IEnumerable<T>` and `IQueryable<T>`,
deferred vs. immediate execution, and the standard query operators, from the original C# 3.0
release through the BCL's later additions to `System.Linq`.

## When to reach for it

- Writing, reviewing, or porting a LINQ query and deciding between query syntax and method syntax.
- Deciding whether a query should be `IEnumerable<T>` (evaluated locally) or `IQueryable<T>`
  (translated by a provider like EF Core).
- Diagnosing a deferred-execution bug: a query enumerated more than once, or a closure that
  captured a loop variable unexpectedly.
- Checking whether a newer operator like `Chunk`, `Order`, `Index`, or `LeftJoin` is available on
  your project's target framework.

## Using it

This skill fires automatically when your request involves writing, reviewing, or debugging a LINQ
query. You can also invoke it directly with `/dotnet-linq`.

## What it covers

| Topic | Reference |
| --- | --- |
| Manual-loop/`Predicate<T>` fallback before LINQ existed | [references/pre-csharp3-manual-filtering.md](references/pre-csharp3-manual-filtering.md) |
| Query/method syntax, deferred execution, standard operators (C# 3.0+) | [references/csharp3-linq-fundamentals.md](references/csharp3-linq-fundamentals.md) |
| `Chunk`, `MinBy`/`MaxBy`, `*By` set operators, 3-way `Zip` (.NET 6+) | [references/csharp10-by-operators-and-chunking.md](references/csharp10-by-operators-and-chunking.md) |
| `Order`/`OrderDescending` shorthand (.NET 7+) | [references/csharp11-order-shorthand.md](references/csharp11-order-shorthand.md) |
| `Index`, `CountBy`, `AggregateBy` (.NET 9+) | [references/csharp13-index-countby-aggregateby.md](references/csharp13-index-countby-aggregateby.md) |
| `LeftJoin`, `RightJoin`, `Shuffle` (.NET 10+) | [references/csharp14-leftjoin-rightjoin-shuffle.md](references/csharp14-leftjoin-rightjoin-shuffle.md) |
| `FullJoin`, tuple-returning `Join`/`GroupJoin` (.NET 11 RC1+) | [references/csharp15-fulljoin-tuple-joins.md](references/csharp15-fulljoin-tuple-joins.md) |

## Example prompts

- "Is this LINQ query going to hit the database once or once per iteration?"
- "Rewrite this foreach loop with manual filtering as a LINQ query."
- "Is `LeftJoin` available on .NET 8, or do I need to write the join manually?"
