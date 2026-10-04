# API lifecycle & contract attributes

Attributes that communicate a member's status to callers and tooling — deprecation, build-time
inclusion, and IntelliSense visibility.

## `[Obsolete]`

`System.ObsoleteAttribute` — since .NET Framework 1.0; the three-argument form
(`Obsolete(message, error)`) since .NET Framework 1.1; `DiagnosticId`/`UrlFormat` properties
added in .NET 5 / C# 9.

Marks a member as deprecated. Three escalating forms:

```csharp
[Obsolete] // compiler warning, generic message
[Obsolete("Use Parse(ReadOnlySpan<char>) instead.")] // warning, custom message
[Obsolete("Removed in v3; use NewApi().", error: true)] // compile ERROR — breaks callers
```

- Always include a message that says *what to use instead*, not just that the member is old —
  the message is what shows up in the build warning and in IDE tooltips; "this is obsolete" with
  no replacement forces every caller to go read source or docs.
- `error: true` is a hard break — only use it once you actually want migration to be
  build-blocking (e.g. the final step of a multi-release deprecation cycle), not as the first
  announcement of a deprecation.
- Since C# 9 / .NET 5, pair it with a `DiagnosticId` (e.g. `"MYLIB0001"`) when you want callers to
  be able to suppress that *specific* obsoletion via `#pragma warning disable MYLIB0001` or
  `<NoWarn>` without blanket-suppressing `CS0618`/`CS0619` for everything else obsolete in their
  dependency graph. Pair `DiagnosticId` with `UrlFormat` to point the warning at migration docs.

**When to add it proactively**: any member being replaced or removed, in the same commit that
introduces the replacement — not as a follow-up. Never remove a public member without an
`[Obsolete]` period first unless the whole library is pre-1.0 / explicitly unstable.

## `[Conditional]`

`System.Diagnostics.ConditionalAttribute(string conditionString)` — since .NET Framework 1.0.

Applied to a method (`void`-returning only), makes every *call site* to that method compiled out
entirely unless the given preprocessor symbol is defined in the caller's compilation — not the
method's own. This is different from wrapping the method body in `#if`: with `[Conditional]`,
call sites don't even need their own `#if` guards, and the method still compiles and can still be
called reflectively.

```csharp
[Conditional("DEBUG")]
public static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

Assert(index >= 0, "index must be non-negative"); // compiled out entirely in Release
```

Multiple `[Conditional]` attributes on one method OR together (compiled in if *any* symbol is
defined).

**When to add it proactively**: debug-only diagnostics, verbose tracing helpers, and
assertion-style guards meant to vanish from Release builds without every call site needing its
own `#if DEBUG` wrapper. Don't use it for anything with a return value (the compiler rejects that)
or anything whose side effects callers might depend on even when the symbol is undefined — a
compiled-out call silently does nothing, which is invisible at the call site.

## `[EditorBrowsable]`

`System.ComponentModel.EditorBrowsable(EditorBrowsableState)` — since .NET Framework 1.1.

Controls whether a public member shows up in IntelliSense/autocomplete — `Never`, `Always`
(default), or `Advanced`. It is **not** an access modifier: the member is still fully public,
callable, and discoverable via reflection or by typing its exact name; this only hides it from
the autocomplete list to reduce noise.

```csharp
[EditorBrowsable(EditorBrowsableState.Never)]
public bool Equals(SomeStruct other) => /* required by IEquatable<T> but not meant to be
    called directly — prefer the == operator */ ...;
```

**When to add it proactively**: interface-implementation members that exist for contract
compliance but have a better-surfaced alternative (operator overloads, extension methods)
you want IntelliSense to nudge callers toward instead. Don't use this as a substitute for
`internal`/`private` — if something genuinely shouldn't be called from outside the assembly, use
accessibility modifiers, not `EditorBrowsableState.Never`, which offers no real encapsulation.

## `[Experimental]`

`System.Diagnostics.CodeAnalysis.ExperimentalAttribute(string diagnosticId)` — since .NET 8 /
C# 12.

The mirror image of `[Obsolete]`: marks a member (or an entire type/assembly) as *too new* to use
without acknowledging the risk, rather than too old. Every caller gets a compiler error
(diagnostic ID of your choosing, e.g. `"MYLIB001"`) unless they explicitly suppress it — there's
no warning-only tier the way `[Obsolete]` has one.

```csharp
[Experimental("MYLIB001")]
public sealed class PreviewParser { /* shape may still change before stabilizing */ }

#pragma warning disable MYLIB001 // acknowledged: API may change before it stabilizes
var parser = new PreviewParser();
#pragma warning restore MYLIB001
```

- Unlike `[Obsolete]`, the diagnostic ID is *required*, not optional — there's no generic
  "this is experimental" warning; every experimental API needs its own ID so callers can suppress
  exactly the one they've reviewed and accepted, not every experimental feature in the library at
  once.
- Applying it to an `assembly`-level target marks everything in that assembly experimental in one
  declaration — useful for an entire preview package, rather than tagging every public type.

**When to add it proactively**: a genuinely pre-stable public API you're shipping ahead of a
final design — a preview feature, an API surface still gathering feedback. Don't use it as a
substitute for proper semantic versioning (a 0.x package's whole surface being "unstable" doesn't
need per-member `[Experimental]`); reach for it when *most* of a stable-looking library is settled
but one specific corner isn't yet.

## Fallback / no-op behavior

`[Obsolete]`, `[Conditional]`, and `[EditorBrowsable]` are old BCL attributes (.NET Framework
1.0/1.1) with no version-gating concerns for any currently supported target. `[Conditional]`'s
effect is purely compile-time and symbol-driven — there is no runtime fallback to reason about; a
caller compiling without the symbol simply never emits the call. `[Experimental]` needs .NET 8 /
C# 12; on an older target, omit it and rely on documentation/versioning alone to signal
pre-stability — there is no lesser-version equivalent.
