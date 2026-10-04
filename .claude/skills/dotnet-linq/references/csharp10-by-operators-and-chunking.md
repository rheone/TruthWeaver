# `*By` Operators, `Chunk`, Non-Enumerated Count, 3-Way `Zip` (.NET 6)

.NET 6 (C# 10, GA November 8, 2021) added a batch of `System.Linq.Enumerable` methods. These are
**BCL/runtime additions, not C# language features** — nothing here is gated by `<LangVersion>`;
what gates it is the target framework, because the methods live in the `System.Linq.dll` shipped
with .NET 6 and later. A project on `<LangVersion>10</LangVersion>` but still targeting
`net5.0` or `netstandard2.0` does **not** get these methods; a project targeting `net6.0`+ gets
them regardless of its configured `LangVersion`. They're filed here (paired with C# 10, .NET 6's
shipped language version) to match this skill's tier-per-SDK-release convention, not because the
language version itself changed anything.

## Syntax

```csharp
IEnumerable<Order[]> batches = orders.Chunk(100);

Order? highestValue = orders.MaxBy(o => o.Total);
Order? lowestValue = orders.MinBy(o => o.Total);

IEnumerable<Order> uniqueByCustomer = orders.DistinctBy(o => o.CustomerId);

bool hasCountCheaply = orders.TryGetNonEnumeratedCount(out int count);

IEnumerable<(string Name, int Age, string City)> triples =
    names.Zip(ages, cities);
```

## Basic use case: batching for downstream processing

```csharp
foreach (Order[] batch in orders.Chunk(500))
{
    await bulkInsertService.InsertBatchAsync(batch);
}
```

`Chunk<TSource>(int size)` splits a sequence into fixed-size arrays, the last one shorter if the
source doesn't divide evenly — the standard shape for batching API calls or bulk database writes
without buffering the entire source into one giant list first.

## Basic use case: picking an extremum without a manual comparer

```csharp
Order? mostExpensive = orders.MaxBy(o => o.Total);
Order? oldestCustomer = customers.MinBy(c => c.DateOfBirth);
```

Before .NET 6, the equivalent was `orders.OrderByDescending(o => o.Total).FirstOrDefault()`, which
sorts the entire sequence (`O(n log n)`) just to read one element; `MaxBy`/`MinBy` compare in a
single pass (`O(n)`) and return the whole matching element, not just the key.

## Basic use case: the `*By` family for set operations with a projection

```csharp
IEnumerable<Order> latestPerCustomer = orders
    .OrderByDescending(o => o.PlacedAt)
    .DistinctBy(o => o.CustomerId);

IEnumerable<Customer> vipOnly = allCustomers
    .IntersectBy(vipCustomerIds, c => c.Id);

IEnumerable<Customer> notBlocked = allCustomers
    .ExceptBy(blockedCustomerIds, c => c.Id);

IEnumerable<Customer> combined = tier1Customers
    .UnionBy(tier2Customers, c => c.Id);
```

`DistinctBy`, `UnionBy`, `IntersectBy`, and `ExceptBy` each take a `keySelector` instead of relying
on the element's own equality — the pre-.NET-6 workaround was a custom `IEqualityComparer<T>`
passed to the non-`By` operator, which needed a whole extra type just to compare on one property.
`IntersectBy`/`ExceptBy` also let the second sequence be a different element type (here, a plain
`IEnumerable<Guid>` of IDs against `IEnumerable<Customer>`), which the older `IEqualityComparer<T>`
approach couldn't do at all since a comparer only ever compares two values of the *same* type.

## Advanced use case: avoiding a needless enumeration to get a count

```csharp
public static string DescribeSize(IEnumerable<Order> orders)
{
    if (orders.TryGetNonEnumeratedCount(out int count))
    {
        return $"{count} orders";
    }
    return "an unknown number of orders"; // sequence would need full enumeration to count
}
```

`TryGetNonEnumeratedCount<TSource>` checks whether the count is available for free — the source is
an `ICollection<T>`, an array, or another type LINQ can introspect without walking it — and returns
`false` rather than force a full enumeration when it isn't. Prefer it over `Count()` specifically
when the sequence might be a single-pass, possibly expensive `IEnumerable<T>` (a database cursor,
a generator method) and a wrong-but-cheap answer is acceptable, or when avoiding a double
enumeration matters more than always having an exact count.

## Advanced use case: 3-way `Zip`

```csharp
IEnumerable<(string Name, int Score, DateTime SubmittedAt)> submissions =
    names.Zip(scores, submittedAtTimes);

foreach (var (name, score, submittedAt) in submissions)
{
    Console.WriteLine($"{name}: {score} at {submittedAt}");
}
```

The pre-.NET-6 two-sequence `Zip` needed a result selector lambda to combine into anything richer
than a pair; the three-sequence overload returns a `ValueTuple` directly, matching how the
two-sequence overload's tuple-returning form already worked.

## Requirements and restrictions

- All of these require the **target framework** to be `net6.0` or later (or a `netstandard2.0`/
  `net472`-style project referencing a `System.Linq`-providing package new enough to include them
  — rare in practice; the ordinary path is just targeting `net6.0`+).
- `MinBy`/`MaxBy` return the *element*, not the key, and return `default` (`null` for a reference
  type) on an empty sequence rather than throwing — unlike `Min()`/`Max()` on a value-type sequence,
  which throw on empty.
- `TryGetNonEnumeratedCount` returning `false` is not an error — it's the expected, common result
  for a plain `IEnumerable<T>` with no cheap count available; always branch on the `bool`, never
  assume `true`.

## Fallback

Below `net6.0`, write the pre-.NET-6 equivalents: `orders.OrderByDescending(o => o.Total)
.FirstOrDefault()` for `MaxBy`, a custom `IEqualityComparer<T>` passed to `Distinct`/`Union`/
`Intersect`/`Except` for the `*By` operators, manual `size`-based batching with `Skip`/`Take` in a
loop for `Chunk`, and `Count()` (paying the enumeration cost) for `TryGetNonEnumeratedCount` — see
[csharp3-linq-fundamentals.md](csharp3-linq-fundamentals.md) for the underlying baseline operators
those fallbacks build on.
