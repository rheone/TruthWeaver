# Expression Tree Limitations and Pitfalls

What can appear inside a lambda that converts to `Expression<TDelegate>` is much narrower than what
can appear inside an ordinary delegate-typed lambda — deliberately: allowing every new C# syntax
form into expression trees would be a breaking change for every existing tree-consuming library
(every LINQ provider, every ORM), which would all need updating whenever the language added syntax.
The restriction list below is current as of the live `learn.microsoft.com` compiler-messages
reference checked for this skill (last updated by Microsoft September 2026) — it has grown with
almost every C# release since 3.0, as new syntax kept needing an explicit "not allowed here" entry,
while the *allowed* subset has stayed essentially the single-expression core from C# 3.0.

## Still true today: statement-bodied lambdas are never allowed

```csharp
Expression<Func<int, int>> square = x => x * x;              // fine — expression body

Expression<Func<int, int>> doubled = x =>                    // CS0834
{
    int result = x * 2;
    return result;
};
```

This is the single most common expression-tree compile error. It is not a temporary gap: a
language-design proposal to allow statement-bodied lambdas in expression trees
(`dotnet/csharplang` issue #4568) has been open for discussion without being adopted, and the
current compiler-messages reference still lists `CS0834` unchanged. Anonymous methods
(`delegate(...) { }`) never convert to expression trees at all, regardless of body shape — only
lambda syntax with an expression body does.

**Workaround:** extract the logic into an ordinary method or a separate expression-bodied helper
and call it, or (if the tree needs to be provider-translatable, not just locally compiled)
restructure the logic as a single conditional/method-call expression instead of a multi-statement
block.

## The full current restriction list

Every one of these is a distinct compiler error, verified live against Microsoft's
compiler-messages reference for this skill:

| What's disallowed | Error | Since (roughly) |
| --- | --- | --- |
| Statement-bodied lambdas | `CS0834` | C# 3.0 — never lifted |
| `async` lambdas | `CS1989` | C# 5.0 |
| `dynamic` operations | `CS1963` | C# 4.0 |
| Assignment operators | `CS0832` | C# 3.0 |
| `base` access to a virtual member | `CS0831` | C# 3.0 |
| Anonymous methods (`delegate(...)`) | `CS1945`/`CS1946` | C# 3.0 |
| Named/optional arguments | `CS0853`/`CS0854` | C# 4.0 |
| Multidimensional array initializers | `CS0838` | C# 3.0 |
| `ref`/`out`/`in` lambda parameters, `out` variable declarations | `CS1951`/`CS8198` | C# 3.0 / C# 7.0 |
| Methods returning by `ref`, or `ref`-returning lambdas | `CS8153`/`CS8155` | C# 7.0 |
| Local function references | `CS8110` | C# 7.0 |
| Tuple literals and tuple `==`/`!=` | `CS8143`/`CS8144`/`CS8382` | C# 7.0 |
| `is` pattern-matching expressions | `CS8122` | C# 7.0 |
| Discards (`_`) | `CS8207` | C# 7.0 |
| Null-propagating (`?.`/`?[]`) and null-coalescing-assignment (`??=`) operators | `CS8072`/`CS8642` | C# 6.0 / C# 8.0 |
| `throw` expressions | `CS8188` | C# 7.0 |
| Switch *expressions* (`x switch { ... }`) | `CS8514` | C# 8.0 |
| `with`-expressions (non-destructive record mutation) | `CS8849` | C# 9.0 |
| Index/range operators (`^`, `..`) | `CS8790`/`CS8791`/`CS8792` | C# 8.0 |
| `ref struct` values (e.g. `Span<T>`) | `CS8640` | C# 7.2 |
| Static abstract/virtual interface member access | `CS8927` | C# 11.0 |
| Interpolated string handler conversions | `CS8952` | C# 10.0 |
| Attributes on the lambda itself or its parameters | `CS8972` | C# 10.0 |
| Inline array access | `CS9170` | C# 12.0 |
| Collection expressions (`[1, 2, 3]`) | `CS9175` | C# 12.0 |
| `&` on a method group | `CS8810` | C# 11.0 |
| Extension property access as an extension | `CS9296` | C# 14.0 |
| Expanded (non-array) `params` collection forms | `CS9226` | C# 13.0 |
| Dictionary initializers / extension `Add` collection initializers | `CS8074`/`CS8075` | C# 6.0 |
| Unsafe pointer operations | `CS1944` | C# 3.0 |

Every "since" column entry above is the C# version that *introduced the syntax being restricted*,
not a version where the restriction itself changed — none of these have ever been lifted once
added; each simply grew the list as the language grew. Treat any blog post claiming a specific one
of these was later allowed with suspicion and re-check the live compiler-messages reference before
trusting it — this list is exactly the kind of claim that goes stale.

## `LambdaExpression.CompileToMethod` doesn't exist on .NET Core/.NET 5+

`CompileToMethod(MethodBuilder)` — which emitted a tree's compiled IL directly into a
`Reflection.Emit`-built method on disk, rather than an in-memory delegate — was part of .NET
Framework but was never ported to .NET Core; it throws `PlatformNotSupportedException` there
(the member exists at the API-surface level for binary compatibility but isn't implemented) and is
entirely absent depending on target framework moniker. This matters when porting old .NET
Framework code that used `CompileToMethod` to persist a compiled expression tree into an assembly
on disk — there is no direct equivalent on .NET Core/.NET 5+; `Compile()` (an in-memory delegate)
is the only supported path.

## `Compile()` cost — cache, don't rebuild

`Expression<TDelegate>.Compile()` does real work (walking the tree, emitting IL, JIT-ing it) —
calling it inside a hot loop or per-request, rather than once and caching the resulting delegate,
is a routine performance mistake with expression-tree-based code (predicate builders, dynamic
query helpers). Build the tree once, `Compile()` once, and reuse the resulting delegate — the same
caching discipline any other expensive one-time setup warrants.

## `IQueryable<T>` translation failures happen at query execution, not compile time

A tree that compiles fine as C# can still fail when an `IQueryable<T>` provider tries to translate
it — calling an arbitrary local method, for instance, compiles (the tree just contains a
`MethodCallExpression` node), but most providers throw at the point the query actually executes
(`ToList()`, `First()`, enumeration) because they don't recognize that method and have no SQL (or
other target) to translate it to. This is a runtime surprise, not a compile error — see
[csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md)
(the "why this exists" section) for why only provider-recognized calls are safe inside an
`IQueryable<T>` lambda.

## Fallback

This entire file is a reference table and set of gotchas, not tier-specific syntax — it applies
identically at whichever tier
([csharp3-expression-trees-fundamentals.md](../references/csharp3-expression-trees-fundamentals.md)
or
[csharp4-dlr-expression-node-types.md](../references/csharp4-dlr-expression-node-types.md)) a
given project targets; a restriction against syntax from a C# version newer than the target simply
doesn't arise because that syntax isn't available to write in the first place.
