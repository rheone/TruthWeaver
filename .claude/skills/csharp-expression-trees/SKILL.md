---
name: csharp-expression-trees
description: Reference for C# expression trees — `Expression<TDelegate>` and the `System.Linq.Expressions` namespace, representing a lambda as an inspectable data structure instead of compiled IL, and how `IQueryable<T>` providers like EF Core consume them (C# 3.0 / .NET Framework 3.5), plus the DLR-era expansion of manually-buildable node types (`BlockExpression`, `LoopExpression`, `TryExpression`, `ExpressionVisitor`, `DynamicExpression`) for hand-assembled trees (C# 4.0 / .NET Framework 4.0). Use when writing, reviewing, or porting code that declares an `Expression<TDelegate>`; deciding whether a lambda's target type resolves to a delegate or an expression tree; building or rewriting an expression tree manually with `Expression.Parameter`/`Expression.Call`/`Expression.Lambda` or an `ExpressionVisitor` subclass; diagnosing a "cannot be converted to an expression tree" compiler error (`CS0834` and related); reasoning about how an `IQueryable<T>` provider translates a query; or using expression trees to build property-name-safe test helpers or expression-based argument matchers.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Expression Trees

An expression tree is the *other* thing a lambda can compile to: instead of IL wrapped in a
delegate, the same syntax becomes an object graph — `Expression`, `BinaryExpression`,
`MethodCallExpression`, and so on — that a library inspects and reinterprets instead of invoking.
This is the origin point of the feature (C# 3.0, alongside LINQ); the one later change that
matters is .NET Framework 4.0's expansion of what you can *manually* build (loops, exception
handling, labels) beyond what the compiler's own lambda-to-tree conversion has ever produced —
still, as of the latest verified C# version, a single expression only. Out of scope: what
generics are and how they work in general (any `Expression<Func<T, TResult>>` or generic helper
method below assumes that background), and the LINQ query operators/lambda syntax themselves —
both are covered elsewhere; this skill is self-contained on expression-tree mechanics specifically.

## Quick start (works everywhere, C# 3.0+)

```csharp
using System.Linq.Expressions;

Expression<Func<int, bool>> isEven = n => n % 2 == 0; // data, not IL — same syntax as a delegate

Console.WriteLine(isEven.Body); // "(n % 2) == 0" — inspectable before it's ever compiled

Func<int, bool> compiled = isEven.Compile(); // turn the tree into a real, invokable delegate
bool result = compiled(4);                   // true
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0+ | C# 1.0+ | [references/pre-csharp3-no-expression-trees.md](references/pre-csharp3-no-expression-trees.md) — no `Expression<TDelegate>` yet; `Reflection.Emit` or a hand-rolled node/interpreter hierarchy fill the gap |
| .NET Framework 3.5+ | C# 3.0+ | [references/csharp3-expression-trees-fundamentals.md](references/csharp3-expression-trees-fundamentals.md) — `Expression<TDelegate>`, the lambda-to-tree-vs-delegate target-type rule, why `IQueryable<T>` providers exist, single-expression manual construction |
| .NET Framework 4.0+ | C# 4.0+ | [references/csharp4-dlr-expression-node-types.md](references/csharp4-dlr-expression-node-types.md) — `BlockExpression`/`LoopExpression`/`TryExpression`/`SwitchExpression`/`DynamicExpression` for hand-built trees, plus `ExpressionVisitor` |

C# 5.0 through the latest version verified for this skill (15.0, in preview as of September 2026)
add nothing expression-tree-capability-specific — each new C# release instead adds *another kind of
syntax that's disallowed inside* a compiler-converted expression tree (see
[specialized/expression-tree-limitations-and-pitfalls.md](specialized/expression-tree-limitations-and-pitfalls.md)
for the full, current, version-by-version restriction list). No reference file exists for those
versions; the C# 4.0 tier above still applies unchanged.

## Specialized patterns

- [specialized/building-expression-trees-manually.md](specialized/building-expression-trees-manually.md) — `Expression.Parameter`/`Expression.Call`/`Expression.Lambda`, `.Compile()`, generic tree-building helper methods, combining two trees onto a shared parameter
- [specialized/expression-visitor-and-tree-rewriting.md](specialized/expression-visitor-and-tree-rewriting.md) — subclassing `ExpressionVisitor`, rewriting vs. purely observing a tree, `node.Update(...)`, generic visitor-driven validators
- [specialized/expression-tree-limitations-and-pitfalls.md](specialized/expression-tree-limitations-and-pitfalls.md) — the full, current "what can't appear in an expression tree" table with compiler error codes, `CompileToMethod`'s removal on .NET Core+, `Compile()` caching, `IQueryable<T>` translation failures at execution time
- [specialized/testing-expression-trees.md](specialized/testing-expression-trees.md) — extracting a property name from `Expression<Func<T,TProperty>>` to avoid magic strings, expression-based mock argument matchers, structural tree-equality assertions
