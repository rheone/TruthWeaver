# LINQ as a Test-Authoring Tool

This file is about *using LINQ to build test data and assertions* — projecting fixtures, asserting
sequence equality and shape, deduplicating expected/actual collections for comparison — not about
testing this skill's own query examples. See
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md) for the underlying
operator syntax these patterns build on.

## Basic: projecting a range into test fixtures

```csharp
List<Order> testOrders = Enumerable.Range(1, 10)
    .Select(i => new Order { Id = $"ORD-{i:D3}", Total = i * 25m, Status = OrderStatus.Pending })
    .ToList();
```

`Enumerable.Range` plus `Select` is the standard way to generate N distinct-but-related test
fixtures without ten copy-pasted object initializers — each fixture derives its identity and
values from the loop index, keeping them obviously related and obviously distinct.

## Basic: asserting sequence equality

```csharp
[Fact]
public void GetPendingOrders_Test_ReturnsExpectedIds()
{
    IEnumerable<string> actualIds = orderService.GetPendingOrders().Select(o => o.Id);

    Assert.Equal(["ORD-001", "ORD-003", "ORD-007"], actualIds);
}
```

xUnit's `Assert.Equal` on two `IEnumerable<T>` sequences checks element-by-element equality in
order — equivalent to `Enumerable.SequenceEqual` — so a `.Select(...)` projecting only the field
under test (here, just `Id`) keeps the assertion focused instead of comparing whole `Order` objects
where unrelated property differences would cause unrelated test failures.

## Advanced: order-independent collection assertions

```csharp
[Fact]
public void GetTags_Test_ReturnsExpectedTagsRegardlessOfOrder()
{
    IEnumerable<string> actual = repository.GetTags(orderId: 42);

    Assert.True(actual.OrderBy(t => t).SequenceEqual(expectedTags.OrderBy(t => t)));
    // equivalently: Assert.Empty(expectedTags.Except(actual).Union(actual.Except(expectedTags)));
}
```

Sorting both sides before `SequenceEqual` turns an order-independent comparison into an
order-dependent one both sides now satisfy — the right choice when the production code makes no
ordering guarantee and the test shouldn't fail on order alone. The `Except`/`Union` alternative
(a symmetric-difference check) is the right choice instead when duplicates should be tolerated
asymmetrically or a descriptive failure listing exactly which elements differ matters more than a
single boolean.

## Advanced: `DistinctBy`/`GroupBy` for de-duplicating and diffing expected/actual test data

```csharp
[Fact]
public void ImportOrders_Test_DeduplicatesByCustomerKeepingLatest()
{
    List<Order> imported = importService.Import(rawRecords);

    List<Order> expectedDeduplicated = rawRecords
        .OrderByDescending(r => r.Timestamp)
        .DistinctBy(r => r.CustomerId)
        .Select(r => r.ToOrder())
        .ToList();

    Assert.Equal(expectedDeduplicated.Select(o => o.Id), imported.Select(o => o.Id));
}
```

Building the *expected* result with the same LINQ operators the production code is supposed to use
(here, `OrderByDescending` + `DistinctBy` to keep the latest record per customer) documents the
exact business rule under test in the test itself — the assertion reads as a restatement of the
requirement ("dedupe by customer, keep latest"), not an opaque literal list a future reader has to
reverse-engineer.

## Advanced: a generic sequence-assertion helper

```csharp
public static class SequenceAssertions
{
    public static void ShouldMatchIgnoringOrder<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        where T : IComparable<T>
    {
        Assert.Equal(expected.Order(), actual.Order());
    }
}
```

```csharp
[Fact]
public void GetOrderIds_Test_MatchesIgnoringOrder() =>
    orderService.GetOrderIds().ShouldMatchIgnoringOrder(["ORD-1", "ORD-2", "ORD-3"]);
```

A generic (`<T>`) assertion extension reused across every `IComparable<T>` element type in the
test suite, instead of a type-specific order-independent comparer per test class — the type
parameter is inferred from the call site exactly as with any other generic LINQ operator.
`Order()` needs .NET 7 (`net7.0`+); on an older target use `.OrderBy(x => x)` instead — see
[csharp11-order-shorthand.md](../references/csharp11-order-shorthand.md#fallback).

## Fallback

`Enumerable.Range`, `Select`, `OrderBy`, `SequenceEqual`, `Except`, `Union`, and `GroupBy` are all
C# 3.0 baseline and work on every target this skill covers. `DistinctBy` needs `net6.0`+ — on an
older target, use `GroupBy(r => r.CustomerId).Select(g => g.OrderByDescending(r =>
r.Timestamp).First())` instead, per
[csharp10-by-operators-and-chunking.md#fallback](../references/csharp10-by-operators-and-chunking.md#fallback).
The `Order()` helper example needs `net7.0`+, noted inline above.
