# LINQ Fundamentals (C# 3.0 / .NET Framework 3.5)

LINQ (Language Integrated Query) shipped with C# 3.0 in .NET Framework 3.5, GA November 19, 2007.
It added, as one coordinated set of features, exactly what query syntax needs: extension methods,
lambda expressions, `var`, anonymous types, object/collection initializers, and expression trees.
`System.Linq.Enumerable` (for `IEnumerable<T>`) and `System.Linq.Queryable` (for `IQueryable<T>`)
are the two static classes holding the standard query operators. Everything in this file is the
universal baseline — it compiles unchanged on every later C# version.

## Syntax: two forms, one compiled result

**Query syntax** — reads like SQL, compiler-translated:

```csharp
IEnumerable<Order> pendingHighValue =
    from o in orders
    where o.Status == OrderStatus.Pending && o.Total > 500m
    orderby o.Total descending
    select o;
```

**Method syntax** — the same operators called directly as generic extension methods:

```csharp
IEnumerable<Order> pendingHighValue = orders
    .Where(o => o.Status == OrderStatus.Pending && o.Total > 500m)
    .OrderByDescending(o => o.Total);
```

The compiler lowers query syntax to method syntax before anything else happens — `from`/`where`
becomes `.Where(...)`, `orderby ... descending` becomes `.OrderByDescending(...)`, `select`
becomes `.Select(...)` (omitted here because `select o` is the identity projection). The two forms
are interchangeable at the call site; mixing them in one query (a query-syntax clause followed by
a method call, e.g. `(from o in orders select o).Take(5)`) is normal and common, since query syntax
itself has no operators for `Take`, `Skip`, `Count`, or most of the newer operators covered in this
skill's later tiers — those are always written in method syntax even inside an otherwise
query-syntax pipeline.

Not every operator has query-syntax sugar. `join`, `group ... by`, `let`, and `into` do; `Take`,
`Skip`, `Distinct`, `Any`, `First`, `Sum`, and everything added after C# 3.0 don't — method syntax
is the only way to reach them.

## Basic use case: filter, project, materialize

```csharp
List<string> pendingOrderIds = orders
    .Where(o => o.Status == OrderStatus.Pending)
    .Select(o => o.Id)
    .ToList();
```

`Select<TSource, TResult>` and `Where<TSource>` are generic methods — `TSource` and `TResult` are
inferred from the lambda, never written explicitly. `ToList()` is what actually runs the query;
see "Deferred vs. immediate execution" below.

## `IEnumerable<T>` vs. `IQueryable<T>`

Both interfaces expose the same standard query operators, but they execute completely differently:

- **`IEnumerable<T>`** ("LINQ to Objects") — operators are ordinary delegates
  (`Func<TSource, bool>`, etc.) compiled to IL and run in-process, item by item, entirely in
  memory. `List<T>`, arrays, and any hand-written `IEnumerable<T>` all execute this way.
- **`IQueryable<T>`** — operators build an **expression tree** instead of running the lambda
  directly. A `Queryable`-implementing provider (Entity Framework Core, an OData client, LINQ to
  SQL) inspects that tree and translates it into another language entirely — typically SQL — then
  executes it out-of-process and materializes the results back into objects.

```csharp
IEnumerable<Order> inMemory = orders.Where(o => o.Total > 500m);      // runs as compiled IL, in-process
IQueryable<Order> inDatabase = dbContext.Orders.Where(o => o.Total > 500m); // becomes a SQL WHERE clause
```

The method call looks identical; the *type* of `orders` decides which overload set binds
(`Enumerable.Where` vs. `Queryable.Where`), and that decides whether the lambda is compiled to a
delegate or captured as an `Expression<Func<TSource, bool>>` for the provider to translate. Full
treatment, including why a lambda that runs fine against `IEnumerable<T>` can throw at runtime
against `IQueryable<T>`, is in
[specialized/ienumerable-vs-iqueryable-execution.md](../specialized/ienumerable-vs-iqueryable-execution.md).

## Deferred vs. immediate execution

Most standard query operators are **deferred**: calling `.Where(...)` or `.Select(...)` builds a
query object but runs nothing. The query executes only when something actually pulls values out of
it — a `foreach`, or a call to a materializing operator:

```csharp
IEnumerable<Order> query = orders.Where(o => o.Status == OrderStatus.Pending); // nothing has run yet
orders.Add(new Order { Status = OrderStatus.Pending });                       // still nothing has run
foreach (Order o in query) { /* the Where predicate runs here, per item, on THIS collection's current contents */ }
```

**Immediate** operators force enumeration right away and return a concrete result:
`ToList()`, `ToArray()`, `ToDictionary()`, `ToHashSet()`, `Count()`, `Sum()`, `Any()`, `First()`,
`Single()`, and every other operator that returns something other than `IEnumerable<T>`/
`IOrderedEnumerable<T>`/`IQueryable<T>`.

Deferred execution is the source of two very common bugs — re-running a query against data that
changed since it was built, and re-executing an expensive query (a database round-trip) once per
enumeration because the result was never materialized. Both are covered in
[specialized/deferred-execution-pitfalls.md](../specialized/deferred-execution-pitfalls.md).

## Standard query operators (LINQ to Objects baseline)

The full `Enumerable`/`Queryable` surface as of C# 3.0, grouped by what they do — every one of
these is a generic method, inferring its type parameters from the source sequence and the lambda:

| Category | Operators |
| --- | --- |
| Filtering | `Where`, `OfType<TResult>`, `Cast<TResult>` |
| Projection | `Select`, `SelectMany` |
| Ordering | `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Reverse` |
| Grouping | `GroupBy` |
| Joining | `Join`, `GroupJoin` |
| Set operations | `Distinct`, `Union`, `Intersect`, `Except` |
| Partitioning | `Take`, `Skip`, `TakeWhile`, `SkipWhile` |
| Quantifiers | `Any`, `All`, `Contains` |
| Aggregation | `Count`, `LongCount`, `Sum`, `Min`, `Max`, `Average`, `Aggregate` |
| Element | `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`, `Last`, `LastOrDefault`, `ElementAt`, `ElementAtOrDefault` |
| Conversion | `ToList`, `ToArray`, `ToDictionary`, `ToLookup`, `AsEnumerable`, `Cast<TResult>` |
| Generation | `Enumerable.Range`, `Enumerable.Repeat`, `Enumerable.Empty<TResult>` |
| Concatenation | `Concat` |
| Equality | `SequenceEqual` |

Ordering, grouping, and joining together are common enough as a combined pattern that they get
their own worked-example file:
[specialized/ordering-grouping-joining.md](../specialized/ordering-grouping-joining.md).

## Generic type inference threaded through

Every operator above is a generic method, and none of the examples in this skill ever write a type
argument explicitly — `orders.Where(o => o.Status == OrderStatus.Pending)` infers
`Where<Order>`, `orders.OfType<Manager>()` needs one written because there's no argument to infer
it from, and `orders.Select(o => o.Id)` infers `Select<Order, string>` from the lambda's return
type. `OfType<TResult>` and `Cast<TResult>` are the two operators from the table above that
routinely need an explicit type argument, since filtering or casting by type is precisely the case
generic inference can't help with:

```csharp
IEnumerable<Manager> managers = employees.OfType<Manager>();     // filters + casts, skips non-matches
IEnumerable<Manager> allManagers = employees.Cast<Manager>();    // casts every element, throws on mismatch
```

## Expression trees and `IQueryable<T>` providers

`Expression<TDelegate>` is the C# 3.0 type that makes `IQueryable<T>` possible at all: a lambda
assigned to `Expression<Func<Order, bool>>` compiles to a data structure describing the lambda's
logic (a tree of `BinaryExpression`, `MemberExpression`, `ConstantExpression` nodes, and so on)
instead of compiling to IL:

```csharp
Expression<Func<Order, bool>> predicateTree = o => o.Total > 500m;   // a tree, not a delegate
Func<Order, bool> predicateDelegate = o => o.Total > 500m;           // an ordinary compiled delegate
```

An `IQueryable<T>` provider walks that tree and emits its own target — SQL text, an HTTP query
string, whatever the provider translates to — instead of ever invoking the lambda as .NET code.
This is why an `IQueryable<T>` query can only use expressions the provider knows how to translate:
a call to an arbitrary C# method (a private helper, a regex match) inside the lambda has no
translation and either throws or falls back to client-side evaluation, depending on the provider.
See [specialized/ienumerable-vs-iqueryable-execution.md](../specialized/ienumerable-vs-iqueryable-execution.md)
for the practical consequences.

## Fallback

There is no fallback below this tier for the operators, syntax, and interfaces themselves — LINQ
requires C# 3.0 / .NET Framework 3.5 at minimum. On an older target, use manual `foreach` loops,
`Predicate<T>`/`Comparison<T>`, and hand-rolled grouping dictionaries instead — see
[pre-csharp3-manual-filtering.md](pre-csharp3-manual-filtering.md).
