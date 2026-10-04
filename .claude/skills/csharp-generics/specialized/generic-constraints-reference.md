# Generic Constraints Reference

Every `where` constraint kind, in the order they were introduced, with the combination rules
that govern using several on one type parameter. Each entry links back to its version-gated
reference file for full context.

| Constraint | C# version | Meaning |
| --- | --- | --- |
| `where T : struct` | 2.0 | non-nullable value type |
| `where T : class` | 2.0 | reference type |
| `where T : new()` | 2.0 | accessible public parameterless constructor |
| `where T : BaseType` | 2.0 | `BaseType` or a type deriving from it |
| `where T : IInterface` | 2.0 | implements `IInterface` |
| `where T : U` | 2.0 | is or derives from type parameter `U` (naked type constraint) |
| `where T : unmanaged` | 7.3 | non-nullable value type with no reference-type fields anywhere in its layout |
| `where T : Enum` | 7.3 | an enum type |
| `where T : Delegate` | 7.3 | a delegate type |
| `where T : notnull` | 8.0 | any non-nullable type — rejects an explicitly nullable reference type argument (warning) |
| `where T : allows ref struct` | 13.0 | anti-constraint — permits (does not require) a `ref struct` type argument |

Full write-ups: [csharp2-generics-fundamentals.md](../references/csharp2-generics-fundamentals.md),
[csharp7-tuples-and-constraints.md](../references/csharp7-tuples-and-constraints.md),
[csharp8-nullable-generics.md](../references/csharp8-nullable-generics.md),
[csharp13-ref-struct-generics.md](../references/csharp13-ref-struct-generics.md).

## Combination order

Multiple constraints on one type parameter are comma-separated in a fixed order the compiler
enforces:

1. `struct` / `class` / `unmanaged` / `notnull` (mutually exclusive with each other, and with
   `new()` implied incompatible with `struct` since value types always have a default
   constructor)
2. A single base class constraint, if any
3. Interface constraints, any number
4. `new()`, last, if present
5. `allows ref struct`, always last (after `new()` if both appear)

```csharp
public class Repository<T> where T : class, IEntity, IAuditable, new()
{
}

public class Buffer<T> where T : IProcessable, allows ref struct
{
}
```

## Mutually exclusive combinations

- `struct` and `class` cannot both appear — a type parameter can't be constrained to be both a
  value type and a reference type.
- `struct` and `new()` are redundant together, not illegal: every value type has an implicit
  parameterless constructor, so `new()` adds nothing when `struct` is already present. Compilers
  don't warn about this; it's just dead weight.
- `unmanaged` implies `struct` — don't write `where T : struct, unmanaged`; `unmanaged` alone is
  the stronger, sufficient constraint.
- `notnull` and `class`/`struct` can combine (`where T : class, notnull`), but `notnull` alone
  already permits both value and non-nullable reference types, so adding `class` narrows it
  further rather than being redundant.

## Constraints on multiple type parameters

Each type parameter gets its own `where` clause; order among clauses doesn't matter, and one
parameter's constraint can reference another:

```csharp
public class Graph<TNode, TEdge>
    where TNode : IEquatable<TNode>
    where TEdge : IEdge<TNode>
{
}
```

## No way to constrain "has an operator" before C# 11

Before generic math ([csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md)),
there is no constraint expressing "`T` supports `+`" or any other operator — only `static abstract`
interface members (C# 11+) let a constraint require an operator or static factory member.
