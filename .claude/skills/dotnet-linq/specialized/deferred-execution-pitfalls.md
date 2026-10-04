# Deferred Execution Pitfalls

Deferred execution (introduced in
[csharp3-linq-fundamentals.md](../references/csharp3-linq-fundamentals.md#deferred-vs-immediate-execution))
is the source of a specific, recurring family of bugs. Each one below compiles cleanly and often
works in a quick test, then misbehaves once the surrounding code changes shape.

## Basic: multiple enumeration re-running the query

```csharp
IEnumerable<Order> expensiveQuery = dbContext.Orders.Where(o => o.Status == OrderStatus.Pending);

int count = expensiveQuery.Count();          // query executes — round-trip #1
List<Order> list = expensiveQuery.ToList();  // query executes AGAIN — round-trip #2, identical work
```

Every enumeration of a deferred query re-runs it from scratch. Against `IQueryable<T>`, that's a
second database round-trip; against `IEnumerable<T>` over an expensive generator, it's the whole
computation twice. Materialize once and reuse the concrete result:

```csharp
List<Order> pendingOrders = dbContext.Orders.Where(o => o.Status == OrderStatus.Pending).ToList();
int count = pendingOrders.Count;   // property on List<T>, no re-query
```

## Basic: enumerating a mutated source mid-iteration

```csharp
List<int> numbers = [1, 2, 3];
IEnumerable<int> query = numbers.Where(n => n > 0);

numbers.Add(4);

foreach (int n in query) { Console.WriteLine(n); }
// prints 1, 2, 3, 4 — the query re-reads `numbers`' current contents at enumeration time,
// not its contents when .Where() was called
```

This is rarely the bug people expect (`InvalidOperationException: Collection was modified`); that
exception only fires when the source is mutated *during* the `foreach` itself, not between building
the query and starting to enumerate it. The actual bug is a query silently reflecting data that
changed after the code "looks like" it captured a snapshot — call `.ToList()` immediately after
the query is built if a snapshot at that point is what's actually needed.

## Advanced: closure over a loop variable inside a query

```csharp
// C# 5.0+ (foreach loop variable scoped per-iteration since C# 5.0): safe
List<IEnumerable<Order>> queriesPerStatus = [];
foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
{
    queriesPerStatus.Add(orders.Where(o => o.Status == status)); // captures THIS iteration's status
}
```

```csharp
// a `for` loop's variable is still shared across iterations on every C# version — always a bug here
List<IEnumerable<Order>> queriesByIndex = [];
for (int i = 0; i < pageBoundaries.Count; i++)
{
    queriesByIndex.Add(orders.Skip(pageBoundaries[i]).Take(pageSize)); // every closure captures the SAME `i`
}
// by the time any of these deferred queries actually enumerates, `i` has already reached its final value
```

`foreach`'s loop variable has been scoped fresh per iteration since C# 5.0 (a language-level fix,
not a LINQ one — every closure genuinely gets its own copy), so capturing it directly in a `.Where`
lambda is safe from C# 5.0 onward. A `for` loop's counter is never per-iteration — it's one
variable mutated across the whole loop — so a deferred query capturing `i` directly always
observes `i`'s value at *enumeration* time, not the iteration where the query was built. Fix it by
copying into a local first:

```csharp
for (int i = 0; i < pageBoundaries.Count; i++)
{
    int capturedIndex = i; // one fresh variable per iteration, safe to capture
    queriesByIndex.Add(orders.Skip(pageBoundaries[capturedIndex]).Take(pageSize));
}
```

The same rule applies identically inside query-syntax LINQ, since query syntax compiles to the
same method calls with the same lambda-capture semantics — a `from status in statuses select
orders.Where(o => o.Status == status)` closes over `status` exactly like the method-syntax `foreach`
example above.

## Advanced: side effects inside a `Select` that's enumerated more than once

```csharp
int callCount = 0;
IEnumerable<int> withSideEffect = numbers.Select(n =>
{
    callCount++;
    return n * 2;
});

List<int> first = withSideEffect.ToList();   // callCount incremented once per element
List<int> second = withSideEffect.ToList();  // callCount incremented AGAIN, same elements
// callCount is now 2x the sequence length, and any external effect (logging, a counter,
// a database write) inside the lambda ran twice
```

A `Select` projection is not guaranteed to run exactly once per element for the lifetime of a
program — it runs once per element **per enumeration**. Any lambda passed to a LINQ operator with
an observable side effect (logging, mutating external state, an I/O call) needs the same
"materialize once, reuse the result" discipline as the multiple-enumeration case above; it's the
same underlying cause; expressed through a projection instead of a re-run query.

## Fallback

Everything above is C# 3.0-era deferred execution behavior (`foreach`'s per-iteration loop-variable
scoping is the one C# 5.0-specific detail, called out inline). No later tier in this skill changes
deferred-execution semantics — `Chunk`, `MinBy`/`MaxBy`, `Order`, `Index`, `CountBy`,
`AggregateBy`, and the `*Join` operators are all deferred the same way `Where`/`Select` always
were, and are subject to the same pitfalls.
