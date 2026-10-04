---
name: csharp-extension-members
description: Reference for C# extension methods and extension members — from the classic `this`-parameter form (C# 3.0 / .NET Framework 3.5) through extension properties, static extension members, and operators (C# 14 / .NET 10), and extension indexers (C# 15 / .NET 11). Use when writing, reviewing, or porting extension methods/members, choosing between classic and new extension syntax, targeting multiple C# language versions in one library, or writing generic extension members. Covers the pre-C#3 fallback pattern (.NET Framework 1.0–2.0, no extension methods) through the latest .NET 11 release candidate.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Extension Members

Two eras of the same idea — adding members to a type you can't (or don't want to) modify directly:

- **Classic extension methods** (C# 3.0+, 2007): a `static` method with `this` on its first parameter. Methods only.
- **Extension members** (C# 14+, 2025): a block-based `extension(...)` syntax that adds properties, static members, and operators — and, from C# 15 (.NET 11), indexers.

Both forms compile into the same static class and coexist in one codebase. Full rules: [references/resolution-and-coexistence-rules.md](specialized/resolution-and-coexistence-rules.md).

## Quick start (works everywhere, C# 3.0+)

```csharp
public static class StringExtensions
{
    public static bool IsNullOrBlank(this string? value) =>
        string.IsNullOrWhiteSpace(value);
}

bool blank = someText.IsNullOrBlank();
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets, so pick the highest tier you need and it points you downward as required.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0 – 2.0 | C# 1.0 – 2.0 | [references/pre-csharp3-no-extensions.md](references/pre-csharp3-no-extensions.md) — no extension mechanism exists; static-helper fallback pattern |
| .NET Framework 3.5 – .NET 9 | C# 3.0 – 13 | [references/csharp3-extension-methods.md](references/csharp3-extension-methods.md) — classic `this` syntax; the universal baseline |
| .NET Core 3.0+ with nullable reference types | C# 8.0+ | [references/csharp8-nullable-extensions.md](references/csharp8-nullable-extensions.md) — nullable annotations on receivers/parameters |
| .NET 10 | C# 14 (default) | [references/csharp14-extension-members.md](references/csharp14-extension-members.md) — `extension(...)` blocks: properties, static members, operators |
| .NET 11 (RC1 as of Sept 2026; GA expected Nov 2026) | C# 15 (default) | [references/csharp15-extension-indexers.md](references/csharp15-extension-indexers.md) — adds extension indexers |

**.NET 9 note:** extension members were originally slated for C# 13 but were deferred to C# 14 — .NET 9 has no extension-block syntax at any `LangVersion` setting, including `preview`. On .NET 9, classic syntax is the only option.

## Specialized patterns

- [specialized/generic-extension-members.md](specialized/generic-extension-members.md) — generic type parameters and constraints, classic methods and C# 14 blocks
- [specialized/extension-properties.md](specialized/extension-properties.md) — read-only, write-only, and stateful extension properties
- [specialized/static-extension-members.md](specialized/static-extension-members.md) — `Type.Member` extension statics, generic-constraint use case
- [specialized/extension-operators.md](specialized/extension-operators.md) — operator overloads on types you don't own
- [specialized/fluent-and-linq-style-patterns.md](specialized/fluent-and-linq-style-patterns.md) — chaining and custom LINQ-style query operators
- [specialized/resolution-and-coexistence-rules.md](specialized/resolution-and-coexistence-rules.md) — overload precedence, `using` scoping, mixing both syntaxes, multi-targeting
- [specialized/testing-extension-members.md](specialized/testing-extension-members.md) — writing extension methods/members as test-authoring tools: fluent assertions, builders, mock setup helpers
