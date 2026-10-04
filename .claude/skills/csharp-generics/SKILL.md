---
name: csharp-generics
description: Reference for C# generics — generic types, methods, and constraints (C# 2.0 / .NET Framework 2.0) through variance, value-tuple and `unmanaged`/`notnull` constraints, generic math and generic attributes (C# 11 / .NET 7), ref struct type arguments (C# 13 / .NET 9), and generic extension blocks (C# 14 / .NET 10, C# 15 / .NET 11 RC). Use when writing, reviewing, or porting a generic type or method, choosing or combining constraints, adding variance to an interface or delegate, writing algorithms generic over numeric types, or gating generic syntax by C# language version.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Generics

Generics have grown by accretion since C# 2.0 (2005) — each later version adds a capability
(variance, new constraint kinds, generic math, ref struct type arguments) without retiring
anything earlier. The baseline in [references/csharp2-generics-fundamentals.md](references/csharp2-generics-fundamentals.md)
still compiles unchanged on C# 15.

## Quick start (works everywhere, C# 2.0+)

```csharp
public class Cache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _items = new();

    public bool TryGet(TKey key, out TValue? value) => _items.TryGetValue(key, out value);

    public void Set(TKey key, TValue value) => _items[key] = value;
}
```

(`notnull` needs C# 8 — see the table below; drop it and the shape above is valid from C# 2.0.)

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 2.0+ | C# 2.0+ | [references/csharp2-generics-fundamentals.md](references/csharp2-generics-fundamentals.md) — generic types/methods, `where` constraints, `default(T)`; the universal baseline |
| .NET Framework 4.0+ | C# 4.0+ | [references/csharp4-variance.md](references/csharp4-variance.md) — `out`/`in` variance on interfaces and delegates |
| .NET Core 1.0 – .NET Framework 4.7.2 | C# 7.0 – 7.3 | [references/csharp7-tuples-and-constraints.md](references/csharp7-tuples-and-constraints.md) — value tuples as generic arguments, `unmanaged`/`enum`/`delegate` constraints |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-nullable-generics.md](references/csharp8-nullable-generics.md) — `notnull` constraint, nullable reference type parameters |
| .NET 7 | C# 11 | [references/csharp11-generic-math-and-attributes.md](references/csharp11-generic-math-and-attributes.md) — generic attributes, static abstract interface members (generic math) |
| .NET 9 | C# 13 | [references/csharp13-ref-struct-generics.md](references/csharp13-ref-struct-generics.md) — `allows ref struct`, `Span<T>` as a type argument |
| .NET 10; .NET 11 (RC1 as of Sept 2026, GA expected Nov 2026) | C# 14; C# 15 (default) | [references/csharp14-generic-extension-blocks.md](references/csharp14-generic-extension-blocks.md) — generic `extension<T>(...)` blocks; C# 15 adds nothing new to generics proper |

## Specialized patterns

- [specialized/generic-constraints-reference.md](specialized/generic-constraints-reference.md) — every constraint kind in one table, oldest to newest, with combination rules
- [specialized/variance-in-depth.md](specialized/variance-in-depth.md) — designing your own covariant/contravariant interface or delegate
- [specialized/generic-type-inference-and-overload-resolution.md](specialized/generic-type-inference-and-overload-resolution.md) — how the compiler infers type arguments, and how generic overloads resolve against each other
- [specialized/generic-collections-and-linq.md](specialized/generic-collections-and-linq.md) — from `List<T>`/`Dictionary<TKey,TValue>` to a hand-rolled `IEnumerable<T>` implementation
- [specialized/generic-delegates-and-attributes.md](specialized/generic-delegates-and-attributes.md) — `Func<>`/`Action<>`/custom generic delegates, and C# 11 generic attributes
- [specialized/generic-math-numeric-abstractions.md](specialized/generic-math-numeric-abstractions.md) — algorithms generic over `INumber<T>` and friends
- [specialized/curiously-recurring-generic-pattern.md](specialized/curiously-recurring-generic-pattern.md) — self-referencing constraints (`where T : Base<T>`)
- [specialized/generics-for-testing.md](specialized/generics-for-testing.md) — generic assertion helpers, `TheoryData<T>`, and object-mother builders
