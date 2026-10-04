# No LINQ (C# 1.0 – 2.0 / .NET Framework 1.0 – 2.0)

LINQ does not exist before C# 3.0 (.NET Framework 3.5, released November 19, 2007). Targeting
.NET Framework 1.0, 1.1, or 2.0 — or compiling with `<LangVersion>1</LangVersion>` /
`<LangVersion>2</LangVersion>` — none of the query syntax, method-chain syntax, or
`System.Linq.Enumerable`/`Queryable` extension methods in this skill are available: `System.Linq`
itself doesn't ship until .NET Framework 3.5.

## What .NET 2.0 does have

- **Generics** (.NET 2.0 / C# 2.0) — `List<T>`, `Dictionary<TKey, TValue>`, and generic methods
  exist, so a hand-written filter can still be strongly typed. What's missing is the query
  operators over them, not generics itself.
- **`Predicate<T>` and `Comparison<T>`** — the BCL's pre-LINQ answer to "pass a condition as a
  value": `List<T>.FindAll(Predicate<T>)`, `Array.FindAll<T>`, `List<T>.Sort(Comparison<T>)`.
- **Anonymous methods** (`delegate(int x) { return x > 0; }`, C# 2.0) — usable as a `Predicate<T>`
  or `Comparison<T>` argument, though without the lambda arrow syntax LINQ later relies on.
- **Iterators / `yield return`** (C# 2.0) — the mechanism deferred execution and `IEnumerable<T>`
  generator methods depend on already exists; LINQ's operators are the first thing to use it
  pervasively over generic sequences, not the first thing to introduce it.

## The fallback pattern: manual loops and `Predicate<T>`

Instead of `orders.Where(o => o.Status == OrderStatus.Pending).OrderBy(o => o.Total)`, filter and
sort by hand:

```csharp
// C# 2.0 / .NET Framework 2.0 — compiles everywhere
public static List<Order> GetPendingOrdersSortedByTotal(List<Order> orders)
{
    List<Order> pending = orders.FindAll(delegate(Order o) { return o.Status == OrderStatus.Pending; });
    pending.Sort(delegate(Order a, Order b) { return a.Total.CompareTo(b.Total); });
    return pending;
}
```

Or the equivalent as an explicit loop, for a projection `Select` would later express:

```csharp
public static List<string> GetOrderSummaries(List<Order> orders)
{
    List<string> summaries = new List<string>();
    foreach (Order o in orders)
    {
        if (o.Status == OrderStatus.Pending)
        {
            summaries.Add(o.Id + ": " + o.Total.ToString("C"));
        }
    }
    return summaries;
}
```

Grouping without `GroupBy` means a hand-rolled `Dictionary<TKey, List<TSource>>`:

```csharp
public static Dictionary<OrderStatus, List<Order>> GroupByStatus(List<Order> orders)
{
    Dictionary<OrderStatus, List<Order>> groups = new Dictionary<OrderStatus, List<Order>>();
    foreach (Order o in orders)
    {
        List<Order> bucket;
        if (!groups.TryGetValue(o.Status, out bucket))
        {
            bucket = new List<Order>();
            groups[o.Status] = bucket;
        }
        bucket.Add(o);
    }
    return groups;
}
```

## Porting forward

When a project later moves to .NET Framework 3.5+ (C# 3.0+), these collapse into method-chain
LINQ: `FindAll`/`foreach`-with-`if` becomes `.Where(...)`, the `foreach`-building-a-list projection
becomes `.Select(...)`, and the hand-rolled grouping dictionary becomes `.GroupBy(...)` — see
[csharp3-linq-fundamentals.md](csharp3-linq-fundamentals.md). The `Predicate<T>`/`Comparison<T>`
delegates themselves are structurally the same shape as the `Func<T, bool>` predicates LINQ takes;
porting is a mechanical rewrite of the delegate call sites, not a redesign.
