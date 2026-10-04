# Full Control-Flow Node Types for Manually-Built Trees (C# 4.0 / .NET Framework 4.0)

.NET Framework 4.0 (April 2010, coinciding with C# 4.0's `dynamic` keyword) substantially expanded
`System.Linq.Expressions` beyond the LINQ-only subset .NET Framework 3.5 shipped. The Dynamic
Language Runtime (DLR) — built to host dynamic languages like IronPython and IronRuby on .NET —
needed expression trees that could represent whole *statement*-level programs (loops, exception
handling, gotos, labeled blocks), not just single expressions, and that expanded node set shipped
as part of the same `System.Linq.Expressions` namespace everyone already used for LINQ. `dynamic`
itself and this node-type expansion landed together but are separate things: `dynamic` is a C#
language feature; the new node types are a `System.Linq.Expressions` API surface you can use
whether or not your code touches `dynamic` at all.

## Syntax

```csharp
using System.Linq.Expressions;

BlockExpression block = Expression.Block(/* ... */);
LoopExpression loop = Expression.Loop(/* ... */);
TryExpression tryCatch = Expression.TryCatch(/* ... */);
SwitchExpression switchExpr = Expression.Switch(/* ... */);
```

## Basic use case: a manually-built tree with a loop and a local variable

```csharp
// Builds, from nodes, the equivalent of:
//   int Sum(int n) { int total = 0; for (int i = 1; i <= n; i++) { total += i; } return total; }

ParameterExpression n = Expression.Parameter(typeof(int), "n");
ParameterExpression total = Expression.Variable(typeof(int), "total");
ParameterExpression i = Expression.Variable(typeof(int), "i");
LabelTarget breakLabel = Expression.Label(typeof(int));

BlockExpression body = Expression.Block(
    variables: new[] { total, i },
    Expression.Assign(total, Expression.Constant(0)),
    Expression.Assign(i, Expression.Constant(1)),
    Expression.Loop(
        Expression.IfThenElse(
            Expression.LessThanOrEqual(i, n),
            Expression.Block(
                Expression.AddAssign(total, i),
                Expression.PostIncrementAssign(i)),
            Expression.Break(breakLabel, total)),
        breakLabel));

var sum = Expression.Lambda<Func<int, int>>(body, n).Compile();
int result = sum(5); // 15
```

This is not something a C# lambda-to-expression-tree conversion can ever produce — the compiler's
own conversion only ever builds a single-expression tree (see
[csharp3-expression-trees-fundamentals.md](csharp3-expression-trees-fundamentals.md)). Everything
here is assembled by hand from `Expression.Block`, `Expression.Loop`, `Expression.Variable`,
`Expression.Assign`, and a `LabelTarget` for the loop's exit — the DLR-era node types the compiler
itself never emits but that are fully usable (and `Compile()`-able) by your own code.

## Advanced use case: `TryExpression` and generic exception-safe trees

```csharp
public static Expression<Func<TInput, TResult>> BuildSafeInvoke<TInput, TResult>(
    Expression<Func<TInput, TResult>> body, TResult fallback)
{
    ParameterExpression input = body.Parameters[0];
    Expression tryBody = new ExpressionReplacer(body.Parameters[0], input).Visit(body.Body)!;

    TryExpression guarded = Expression.TryCatch(
        Expression.Convert(tryBody, typeof(TResult)),
        Expression.Catch(typeof(Exception), Expression.Constant(fallback, typeof(TResult))));

    return Expression.Lambda<Func<TInput, TResult>>(guarded, input);
}
```

A generic helper like `BuildSafeInvoke<TInput, TResult>` wraps an arbitrary caller-supplied tree
in a `TryExpression`/`CatchBlock` pair so the compiled delegate returns a fallback instead of
throwing — the node types from this tier (`TryExpression`, `CatchBlock`) compose with generics the
same way the rest of `System.Linq.Expressions` does, since `Expression<TDelegate>` was always
generic over its delegate type. (`ExpressionReplacer` here is an `ExpressionVisitor` subclass; see
[specialized/expression-visitor-and-tree-rewriting.md](../specialized/expression-visitor-and-tree-rewriting.md).)

## What else this tier added

- **`ExpressionVisitor`** — the abstract base class for walking and rewriting a tree, introduced in
  this same .NET Framework 4.0 release. Every visitor-based pattern in
  [specialized/expression-visitor-and-tree-rewriting.md](../specialized/expression-visitor-and-tree-rewriting.md)
  depends on it.
- **`DynamicExpression`** — represents a late-bound (dynamic) operation. This is *not* something a
  C# `dynamic`-typed lambda-to-expression-tree conversion ever produces — C#'s own conversion
  still rejects `dynamic` operations inside an expression tree lambda (`CS1963`, unchanged since).
  `DynamicExpression` exists for other DLR-hosted languages and for code that builds trees by hand
  and wants a dynamically-bound call site as one node in an otherwise ordinary tree.
- **`GotoExpression` / `LabelExpression` / `LabelTarget`** — arbitrary jumps and labeled targets,
  the primitive `Expression.Break`/`Expression.Continue`/`Expression.Return` above build on.
- **`SwitchExpression`** — a full multi-branch switch as a single node, predating C#'s own switch
  *expression* syntax (added C# 8.0) by close to a decade.
- **`RuntimeVariablesExpression`** and **`DebugInfoExpression`** — runtime variable boxes for
  closures built entirely from nodes, and source-location metadata for debugger support,
  respectively; both niche outside DLR-hosted-language implementation work.

## Requirements and restrictions

- None of these node types are reachable through ordinary C# lambda syntax converted to
  `Expression<TDelegate>` — the compiler's lambda-to-tree conversion is unchanged since C# 3.0 and
  still only ever produces a single expression. Everything on this tier requires calling the
  `Expression` factory methods directly.
- A tree built with these node types still needs an eventual `LambdaExpression`/`Expression<TDelegate>`
  wrapper (via `Expression.Lambda`) to be `Compile()`-able into an invokable delegate.

## Fallback

There is no earlier tier that offers any subset of this — on .NET Framework 3.5 (C# 3.0), a
manually-built tree is limited to the single-expression node types documented in
[csharp3-expression-trees-fundamentals.md](csharp3-expression-trees-fundamentals.md)
(`BinaryExpression`, `MethodCallExpression`, `ConditionalExpression`, and similar); anything that
needs a loop, a mutable local, or exception handling has to be restructured as nested conditional
and method-call expressions, or run through `Reflection.Emit` directly as in
[pre-csharp3-no-expression-trees.md](pre-csharp3-no-expression-trees.md) instead.
