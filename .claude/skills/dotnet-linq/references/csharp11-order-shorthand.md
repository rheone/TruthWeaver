# `Order` / `OrderDescending` Shorthand (.NET 7)

.NET 7 (C# 11, GA November 8, 2022) added `Enumerable.Order` and `Enumerable.OrderDescending`.
Like the previous tier, this is a **BCL addition tied to the target framework**, not a language
feature — availability depends on targeting `net7.0`+, not on `<LangVersion>`.

## Syntax

```csharp
IEnumerable<int> ascending = numbers.Order();
IEnumerable<int> descending = numbers.OrderDescending();
```

## Basic use case: sorting by the element's own natural ordering

```csharp
List<decimal> sortedTotals = orderTotals.Order().ToList();
List<string> newestFirst = versionTags.OrderDescending(StringComparer.OrdinalIgnoreCase).ToList();
```

Before .NET 7, sorting a sequence by its own value (rather than by some derived key) needed the
redundant identity-selector idiom: `orderTotals.OrderBy(x => x)`. `Order()`/`OrderDescending()`
name that specific, common case directly and read as what they mean.

## Requirements and restrictions

- The element type must implement `IComparable<T>` for the parameterless overload, or an
  `IComparer<T>` must be supplied — the same constraint `OrderBy(x => x)` already had, since
  `OrderBy`'s default comparer resolution is identical.
- Both return `IOrderedEnumerable<TSource>`, so `ThenBy`/`ThenByDescending` chain onto them exactly
  as onto `OrderBy`/`OrderByDescending`.
- `Queryable.Order`/`Queryable.OrderDescending` exist as the `IQueryable<T>` counterparts, so an
  EF Core query can use the same shorthand and have it translate to `ORDER BY` — see
  [specialized/ienumerable-vs-iqueryable-execution.md](../specialized/ienumerable-vs-iqueryable-execution.md).

## Fallback

Below `net7.0`, write `OrderBy(x => x)` / `OrderByDescending(x => x)` — semantically identical,
just without the shorthand name. See
[csharp3-linq-fundamentals.md](csharp3-linq-fundamentals.md) for `OrderBy`/`OrderByDescending`
themselves, which are the C# 3.0 baseline this shorthand sits directly on top of.
