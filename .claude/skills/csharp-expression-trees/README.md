# C# Expression Trees

Helps you write, review, or port code that builds or consumes `Expression<TDelegate>` (a lambda
represented as an inspectable object graph instead of compiled IL), including how `IQueryable<T>`
providers like EF Core translate them. Assumes familiarity with generics and LINQ syntax
themselves; this skill is self-contained on expression-tree mechanics specifically.

## When to reach for it

- Deciding whether a lambda's target type should resolve to a delegate or an `Expression<TDelegate>`
- Building or rewriting a tree by hand with `Expression.Parameter`/`Expression.Call`/`Expression.Lambda`
- Diagnosing a "cannot be converted to an expression tree" compiler error (`CS0834` and related)
- Writing an `ExpressionVisitor` subclass to rewrite or inspect a tree
- Building a property-name-safe test helper or expression-based argument matcher

## Using it

This skill is model-invoked: it fires automatically when you're working with `Expression<TDelegate>`,
manually assembling expression nodes, or diagnosing an expression-tree compiler error.

## What it covers

| Topic | Reference |
| --- | --- |
| No expression trees yet: Reflection.Emit or hand-rolled interpreters | [references/pre-csharp3-no-expression-trees.md](references/pre-csharp3-no-expression-trees.md) |
| `Expression<TDelegate>` fundamentals, `IQueryable<T>` translation | [references/csharp3-expression-trees-fundamentals.md](references/csharp3-expression-trees-fundamentals.md) |
| DLR-era node types: `BlockExpression`, `LoopExpression`, `TryExpression`, `ExpressionVisitor` | [references/csharp4-dlr-expression-node-types.md](references/csharp4-dlr-expression-node-types.md) |

## Example prompts

- "Why won't this lambda compile when I assign it to an `Expression<Func<T, bool>>`?"
- "Build an expression tree by hand that calls `Contains` on a property picked at runtime."
- "Write an `ExpressionVisitor` that replaces every constant with a parameter reference."
