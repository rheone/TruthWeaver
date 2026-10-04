---
name: csharp-system-attributes
description: Proactive guidance on System.* / BCL attributes to add while writing or reviewing C# code — DebuggerDisplay/DebuggerBrowsable/DebuggerTypeProxy/DebuggerStepThrough/StackTraceHidden for debugging, Obsolete/Conditional/EditorBrowsable for API lifecycle, nullable-flow attributes (NotNullWhen, MemberNotNull, DoesNotReturn, ...) for TryXxx and guard patterns, CallerMemberName/CallerArgumentExpression for caller info, MethodImpl/SkipLocalsInit for performance, AttributeUsage/Flags for custom attribute and enum authoring, and SuppressMessage/UnconditionalSuppressMessage for analyzer/trim suppression. Use when writing a throw-helper, a TryParse-style method, a custom attribute class, a flags enum, INotifyPropertyChanged boilerplate, or when deciding whether/which attribute belongs on a type or member. Does not cover P/Invoke or marshaling attributes (DllImport, MarshalAs, StructLayout), or framework-specific attributes (ASP.NET Core, EF Core, System.Text.Json, test frameworks).
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# System Attributes

Guidance on *when and why* to add a BCL attribute while writing or reviewing C# code — not just
what each attribute does. Organized by the situation you're in, not by C# version: these
attributes are mostly stable, purpose-driven tools rather than version-gated syntax, so each
reference file below notes an attribute's version-introduced fact inline rather than splitting
files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| A type is noisy or unhelpful to inspect in a debugger | `DebuggerDisplay`, `DebuggerBrowsable`, `DebuggerTypeProxy`, `DebuggerStepThrough`, `StackTraceHidden` | [references/debugging-diagnostics.md](references/debugging-diagnostics.md) |
| Deprecating a member, gating debug-only code, or tidying IntelliSense | `Obsolete`, `Conditional`, `EditorBrowsable` | [references/api-lifecycle-contracts.md](references/api-lifecycle-contracts.md) |
| Writing a `TryXxx` method, a guard/throw-helper, or an `EnsureXxx` initializer | `NotNullWhen`, `MaybeNullWhen`, `MemberNotNull`, `MemberNotNullWhen`, `DoesNotReturn`, `DoesNotReturnIf`, `NotNull`, `MaybeNull`, `AllowNull`, `DisallowNull` | [references/nullable-flow-analysis.md](references/nullable-flow-analysis.md) |
| Writing `OnPropertyChanged`, a logging helper, or a validating guard method | `CallerMemberName`, `CallerLineNumber`, `CallerFilePath`, `CallerArgumentExpression` | [references/caller-info.md](references/caller-info.md) |
| Responding to a measured hot path (profiler/benchmark in hand) | `MethodImpl(AggressiveInlining/AggressiveOptimization)`, `SkipLocalsInit` | [references/performance.md](references/performance.md) |
| Defining a custom attribute class or a bitwise-combinable enum | `AttributeUsage`, `Flags` | [references/attribute-authoring-meta.md](references/attribute-authoring-meta.md) |
| An analyzer or trimmer/AOT diagnostic is a known false positive | `SuppressMessage`, `UnconditionalSuppressMessage` | [references/analyzer-suppression.md](references/analyzer-suppression.md) |
| Verifying an attribute is applied correctly, or testing `[Conditional]`/nullable-flow/caller-info behavior | Reflection-based assertions, compile-time vs. runtime distinctions | [references/testing.md](references/testing.md) |

## Quick start

The single most common miss: a `TryXxx` method without a nullable-flow attribute on its `out`
parameter, forcing every caller into `result!` even right after checking the returned `bool`.

```csharp
public static bool TryParse(string? input, [NotNullWhen(true)] out Config? result)
{
    if (input is null) { result = null; return false; }
    result = new Config(input);
    return true;
}
```

## Out of scope

- P/Invoke and marshaling attributes (`DllImport`, `LibraryImport`, `MarshalAs`, `StructLayout`)
  — a narrow, deep domain of its own with different failure modes than everyday type/member
  annotation.
- Framework-specific attributes (ASP.NET Core, EF Core, `System.Text.Json`) and test-framework
  attributes — out of scope regardless of how central they are to a given codebase.
