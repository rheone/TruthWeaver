# C# Generics

Helps you write, review, or port generic types and methods in C# (constraints, variance, value
tuples, generic math, ref struct type arguments, and generic extension blocks), across every
version since generics were introduced.

## When to reach for it

- Writing or reviewing a generic type or method and its constraints
- Combining multiple constraints (`class`, `struct`, `notnull`, `unmanaged`, an interface) on one type parameter
- Adding `out`/`in` variance to a generic interface or delegate
- Writing an algorithm generic over numeric types
- Gating generic syntax by C# language version when porting older code

## Using it

This skill is model-invoked: it fires automatically when you're writing, reviewing, or porting a
generic type or method, or choosing and combining constraints.

## What it covers

| Topic | Reference |
| --- | --- |
| Generic types, methods, and constraints (the universal baseline) | [references/csharp2-generics-fundamentals.md](references/csharp2-generics-fundamentals.md) |
| `out`/`in` variance | [references/csharp4-variance.md](references/csharp4-variance.md) |
| Value tuples, `unmanaged`/`enum`/`delegate` constraints | [references/csharp7-tuples-and-constraints.md](references/csharp7-tuples-and-constraints.md) |
| `notnull` constraint, nullable type parameters | [references/csharp8-nullable-generics.md](references/csharp8-nullable-generics.md) |
| Generic attributes, generic math | [references/csharp11-generic-math-and-attributes.md](references/csharp11-generic-math-and-attributes.md) |
| `allows ref struct` anti-constraint | [references/csharp13-ref-struct-generics.md](references/csharp13-ref-struct-generics.md) |
| Generic extension blocks | [references/csharp14-generic-extension-blocks.md](references/csharp14-generic-extension-blocks.md) |

## Example prompts

- "Constrain this generic method so `T` must be a reference type with a parameterless constructor."
- "Can I make this generic repository interface covariant in `TEntity`?"
- "Why won't my generic method accept a `Span<T>` as the type argument?"
