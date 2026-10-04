# `Expression<TDelegate>` Fundamentals (C# 3.0 / .NET Framework 3.5)

C# 3.0 (.NET Framework 3.5, November 19, 2007) introduced the `System.Linq.Expressions` namespace
and `Expression<TDelegate>` alongside LINQ: the same lambda syntax that compiles to a delegate can,
depending on its *target type*, compile instead to a data structure describing the lambda's code —
an expression tree — that a library can inspect, transform, and reinterpret at runtime instead of
merely invoking. This is the origin point of the feature; there is no earlier C# version where any
form of it exists (see [pre-csharp3-no-expression-trees.md](pre-csharp3-no-expression-trees.md) for
what filled the gap before).

## Syntax

```csharp
Func<int, bool> isEvenDelegate = n => n % 2 == 0;              // compiles to IL — a delegate
Expression<Func<int, bool>> isEvenTree = n => n % 2 == 0;      // compiles to a data structure
```

The right-hand side is *identical* C# syntax in both lines. Only the declared target type changes
what the compiler produces: assigned to `Func<int, bool>`, the compiler emits IL for a method and
wraps it in a delegate as usual. Assigned to `Expression<Func<int, bool>>`, the compiler instead
emits code that *builds an object graph* — a `BinaryExpression` node for `%`, wrapped in another
`BinaryExpression` for `==`, wrapped in a `LambdaExpression` — describing the same logic as data.

## Basic use case: inspecting, then compiling, a lambda

```csharp
Expression<Func<int, bool>> isEvenTree = n => n % 2 == 0;

Console.WriteLine(isEvenTree.Body);        // "(n % 2) == 0"
Console.WriteLine(isEvenTree.Parameters[0].Name); // "n"

Func<int, bool> isEven = isEvenTree.Compile(); // turns the tree into a real, invokable delegate
bool result = isEven(4);                       // true
```

`Expression<TDelegate>.Compile()` walks the tree and emits IL at runtime (effectively the same
mechanism `System.Reflection.Emit` exposes directly), producing an ordinary delegate of type
`TDelegate`. Nothing about `isEvenTree.Body` or `.Parameters` requires compiling first — those are
readable immediately, which is the entire point: the tree is data before it's ever code.

## Advanced use case: why this exists — `IQueryable<T>` and provider translation

LINQ ships two query surfaces with the same operator names but different signatures:
`IEnumerable<T>`'s extension methods take `Func<>` (delegates — "run this in-process"), and
`IQueryable<T>`'s take `Expression<Func<>>` (trees — "here is data describing what to run"). A
provider behind `IQueryable<T>` (Entity Framework, or any other `IQueryProvider` implementation)
never executes the tree as C# — it walks the `Expression` object graph and translates each node
into something else entirely, most commonly SQL:

```csharp
IQueryable<Order> orders = dbContext.Orders;                 // an IQueryable<Order>
IQueryable<Order> pending = orders.Where(o => o.Status == OrderStatus.Pending);
```

`orders.Where(...)` here resolves to `Queryable.Where`, not `Enumerable.Where`, because `orders`'s
static type is `IQueryable<Order>` — so the lambda `o => o.Status == OrderStatus.Pending` compiles
to `Expression<Func<Order, bool>>`, not `Func<Order, bool>`. `Queryable.Where` doesn't invoke that
expression at all; it hands the whole `IQueryable<T>.Expression` tree (the `Where` call itself
becomes another node, wrapping the tree built so far) to `IQueryProvider.CreateQuery`, and the
provider decides what to do with it — build a `WHERE Status = @p0` clause, in EF Core's case, only
when the query actually enumerates. This is the mechanism every "LINQ provider" (EF Core, an
in-memory test double, a search-index adapter) is built on: they all consume the same
`Expression`/`ExpressionVisitor` API this skill documents, whatever they translate it into.

The essential consequence for writing `IQueryable<T>` queries: only what a given provider's
`ExpressionVisitor` knows how to translate can appear in the lambda. A LINQ-to-Objects lambda
(`Func<>`) can call any method that compiles; an `IQueryable<T>` lambda (`Expression<Func<>>`) can
only call what the provider recognizes — an arbitrary local C# method usually isn't one of them,
and fails at provider-translation time (a runtime error from the provider), not at compile time.

## Which target types compile to a tree, not a delegate

The compiler picks based on the *declared* target type at the point of conversion — a parameter
type, a field/variable type, or (from C# 10 onward) inference through `var`, is never enough on its
own without an existing `Expression<TDelegate>`-typed context to infer *toward*:

```csharp
void AcceptsDelegate(Func<int, bool> predicate) { }
void AcceptsTree(Expression<Func<int, bool>> predicate) { }

AcceptsDelegate(n => n > 0); // compiles to IL — a delegate
AcceptsTree(n => n > 0);     // compiles to an expression tree — same syntax, different target
```

This is also why a lambda passed to `Enumerable.Where` (parameter type `Func<TSource, bool>`)
never produces a tree, while the same lambda passed to `Queryable.Where` (parameter type
`Expression<Func<TSource, bool>>`) always does — the method overload resolved determines the
target type, which determines what the compiler builds.

## Building a tree manually, briefly

The compiler-driven conversion above only ever produces a *single expression* — the restrictions
this implies (no statement bodies, no loops, no local variables) are covered in
[expression-tree-limitations-and-pitfalls.md](../specialized/expression-tree-limitations-and-pitfalls.md).
The `Expression` static factory class lets you build a tree node by node instead, without writing
any lambda syntax at all — useful when the shape of the logic is only known at runtime:

```csharp
ParameterExpression n = Expression.Parameter(typeof(int), "n");
BinaryExpression body = Expression.GreaterThan(n, Expression.Constant(0));
Expression<Func<int, bool>> isPositive = Expression.Lambda<Func<int, bool>>(body, n);

Func<int, bool> compiled = isPositive.Compile();
bool result = compiled(5); // true
```

The full manual-construction workflow — `Expression.Parameter`, `Expression.Call`,
`Expression.Lambda`, and generic helper methods that build trees for an arbitrary `T` — is covered
in [specialized/building-expression-trees-manually.md](../specialized/building-expression-trees-manually.md).

## Requirements and restrictions

- Only *expression-bodied* lambdas (`params => expr`) convert to `Expression<TDelegate>`; a
  statement-bodied lambda (`params => { ... }`) does not — `CS0834`, still true as of the latest
  documented C# version (verified against the current compiler-messages reference; see
  [expression-tree-limitations-and-pitfalls.md](../specialized/expression-tree-limitations-and-pitfalls.md)
  for the full, current restriction list and why this specific one has never been lifted despite
  repeated language-design discussion).
- An anonymous method (`delegate(...) { ... }`) never converts to an expression tree, regardless of
  body shape — only lambda syntax does.
- `TDelegate` in `Expression<TDelegate>` must itself be a delegate type (`CS0835` otherwise).

## Fallback

There is no fallback — this is the feature's origin point. On .NET Framework 1.0–2.0 (C# 1.0–2.0),
use the workaround patterns in
[pre-csharp3-no-expression-trees.md](pre-csharp3-no-expression-trees.md) instead: `Reflection.Emit`
for runtime-generated executable logic, or a hand-rolled node/interpreter hierarchy for logic that
needs to be inspected or translated rather than merely run.
