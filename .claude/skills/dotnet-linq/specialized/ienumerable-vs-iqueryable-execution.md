# `IEnumerable<T>` vs. `IQueryable<T>` Execution

A deeper look at the split introduced in
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md#ienumerablet-vs-iqueryablet):
same operator names, two entirely different execution models, and the specific ways mixing them
goes wrong.

## Basic: the same query, two execution paths

```csharp
List<Order> inMemoryOrders = GetOrdersFromCache();
DbSet<Order> dbOrders = dbContext.Orders;

IEnumerable<Order> queryA = inMemoryOrders.Where(o => o.Total > 500m); // Enumerable.Where — compiled delegate
IQueryable<Order> queryB = dbOrders.Where(o => o.Total > 500m);        // Queryable.Where — expression tree
```

`inMemoryOrders` is `List<Order>`, which implements `IEnumerable<Order>` but not
`IQueryable<Order>`, so `Where` resolves to `Enumerable.Where`, taking a compiled
`Func<Order, bool>`. `dbOrders` is `IQueryable<Order>` (an EF Core `DbSet<T>`), so the identical
lambda syntax instead builds an `Expression<Func<Order, bool>>` for `Queryable.Where`, and nothing
runs until the provider translates and executes it — typically as SQL.

## Basic: a method that works against `IEnumerable<T>` but throws against `IQueryable<T>`

```csharp
static bool IsPremiumTier(Order order) => order.Total > 500m && ContainsGiftItem(order.LineItems);

inMemoryOrders.Where(IsPremiumTier);     // fine: runs IsPremiumTier as ordinary compiled code
dbOrders.Where(o => IsPremiumTier(o));   // throws at query-execution time: the EF Core provider
                                          // has no SQL translation for an arbitrary C# method call
```

An `IQueryable<T>` provider only understands the subset of C# it has translation rules for —
comparisons, arithmetic, property access, and a known list of translatable BCL methods (most
`string`/`DateTime` members, for instance). A call to a private helper method, a regex match, or
anything the provider's translator doesn't recognize fails, usually with a runtime exception
naming the untranslatable expression — not a compile error, because the lambda is syntactically
valid C# either way.

## Advanced: `AsEnumerable()` to force client-side evaluation partway through a query

```csharp
IEnumerable<Order> premiumOrders = dbContext.Orders
    .Where(o => o.Status == OrderStatus.Pending)  // translated to SQL, runs in the database
    .AsEnumerable()                                // materializes results as IEnumerable<Order> here
    .Where(IsPremiumTier);                          // runs in-process; ContainsGiftItem is now fine
```

`AsEnumerable()` re-types an `IQueryable<T>` as `IEnumerable<T>` without changing the underlying
sequence — everything before it still translates and executes server-side (filtered by `Status`
in the database), and everything after it runs as ordinary compiled LINQ to Objects. This is the
standard escape hatch for logic a provider can't translate, at the cost of pulling every row
matching the first filter into memory before the second filter runs — the earlier the split point
sits in the pipeline, the more of the filtering stays translated and cheap.

## Advanced: `ToList()`/`ToArray()` mid-query has the same materializing effect, with intent

```csharp
List<Order> pendingOrders = await dbContext.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .ToListAsync();                                 // query executes here, exactly once

List<Order> premiumOrders = pendingOrders
    .Where(IsPremiumTier)
    .ToList();
```

Prefer an explicit `ToListAsync()`/`ToList()` boundary like this over `AsEnumerable()` when the
goal genuinely is "stop the database round-trip here and continue in memory" — it reads as a
deliberate materialization point rather than a type-system workaround, and pairs naturally with
`async`/`await` for provider calls that support it (EF Core's `ToListAsync`, `FirstOrDefaultAsync`,
etc., which have no `IEnumerable<T>` equivalent since there's no I/O to await there).

## Fallback

Everything here needs only C# 3.0 (`IQueryable<T>`, `Expression<TDelegate>`, and `AsEnumerable()`
are all part of the original LINQ release) — see
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md). No later tier changes
this behavior; it applies identically whichever standard query operators are in play.
