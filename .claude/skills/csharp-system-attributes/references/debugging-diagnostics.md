# Debugging & diagnostics attributes

Attributes that shape how a debugger presents or steps through your types — they change nothing
at runtime for normal execution; they only affect the debugging experience.

## `[DebuggerDisplay]`

`System.Diagnostics.DebuggerDisplay(string)` — since .NET Framework 2.0.

Controls the summary shown in the Watch/Locals/Autos windows and inline hover, instead of the
default `ToString()`-or-type-name display. Use it whenever a type's default display (its full
`ToString()`, or worse, just the type name) is too noisy or unhelpful to eyeball while stepping
through code — collections, value objects, and anything with a "the interesting field" pattern.

```csharp
[DebuggerDisplay("{Name,nq} ({Count} items)")]
public sealed class Basket
{
    public string Name { get; }
    public int Count => _items.Count;
    private readonly List<Item> _items = new();
}
```

- `{Expression}` is evaluated in the debugger; `,nq` ("no quotes") strips the quotes a `string`
  result would otherwise get wrapped in.
- Prefer a private `DebuggerDisplay` property/method over inlining complex expressions in the
  attribute string — the string isn't compiled, so mistakes only surface at debug time, and a
  real member is refactor-safe (rename-tracked) where a string literal isn't.

**When to add it proactively**: any type with more than 2–3 fields where the "obviously
interesting" one isn't already what `ToString()` shows, especially collection wrapper types and
DTOs that will be inspected a lot during debugging sessions.

## `[DebuggerBrowsable]`

`System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState)` — since .NET Framework 2.0.

Controls whether a member appears in the debugger's variable list at all:

- `Never` — hide it entirely (e.g. a backing field already shown via its property, or a huge
  internal cache that clutters the view).
- `Collapsed` (default) — shown normally.
- `RootHidden` — hide the member itself but flatten its children up one level (used by BCL
  collection wrappers so you see the elements, not `_items → [0], [1], ...`).

```csharp
[DebuggerBrowsable(DebuggerBrowsableState.Never)]
private readonly object _syncRoot = new();
```

**When to add it proactively**: sync/lock objects, lazily-initialized backing fields already
exposed by a property, or any field whose presence in the debugger view is pure noise.

## `[DebuggerTypeProxy]`

`System.Diagnostics.DebuggerTypeProxy(Type)` — since .NET Framework 2.0.

Substitutes a different type's shape for what the debugger displays when expanding an instance —
used when the real internal representation (a hash table, a tree) is a poor debugging view of
what the type conceptually holds (a set, a sorted sequence). The proxy type takes the target type
in its constructor and exposes `[DebuggerBrowsable(RootHidden)]` members shaped for display.

```csharp
[DebuggerTypeProxy(typeof(RingBufferDebugView<>))]
public sealed class RingBuffer<T> { /* ... */ }

internal sealed class RingBufferDebugView<T>
{
    private readonly RingBuffer<T> _buffer;
    public RingBufferDebugView(RingBuffer<T> buffer) => _buffer = buffer;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _buffer.ToArray();
}
```

**When to add it proactively**: custom collection or graph types where the storage
representation and the logical contents differ enough that expanding the instance in a debugger
is misleading without it. Don't reach for this before `[DebuggerDisplay]` — it's a heavier tool
for a narrower problem (reshaping *expansion*, not the one-line summary).

## `[DebuggerStepThrough]`

`System.Diagnostics.DebuggerStepThrough` — since .NET Framework 2.0.

Tells the debugger to step over the attributed method/constructor/class entirely (as if it were
a single opaque call), including not stopping at breakpoints set inside it. Use on trivial
forwarding code — property-changed boilerplate, generated proxies, simple guard/validation
helpers — where stepping into it during a debug session is never useful and just interrupts flow.

```csharp
[DebuggerStepThrough]
private static void ThrowIfNull(object? value, string paramName)
{
    if (value is null) throw new ArgumentNullException(paramName);
}
```

Don't apply it broadly to real logic — a breakpoint set *inside* a `[DebuggerStepThrough]` method
won't be hit, which surprises whoever's debugging later and has no way to know why without
checking the source.

## `[StackTraceHidden]`

`System.Diagnostics.StackTraceHidden` — since .NET 6 (C# language-version-independent; it's a
runtime/BCL attribute, not a language feature).

Removes the attributed method from stack traces rendered by `Exception.StackTrace` and (as of
.NET 8) `Environment.StackTrace` — the *runtime* semantics are unaffected, only the printed
trace. This is the attribute-based counterpart to `[DebuggerStepThrough]`: that one affects the
interactive debugger's stepping behavior, this one affects the trace text a thrown exception
carries into logs.

```csharp
[StackTraceHidden]
internal static class ThrowHelpers
{
    public static void ThrowIfNegative(int value, string paramName)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(paramName);
    }
}
```

**When to add it proactively**: dedicated throw-helper classes/methods (the pattern the BCL
itself uses for `ArgumentNullException.ThrowIfNull` et al.) — without it, every exception thrown
via the helper shows the helper's own frame at the top of the trace instead of the caller that
actually violated the contract, which is the frame a reader of the log actually wants.

## Fallback / no-op behavior

None of these attributes are version-gated in the "unavailable below C# N" sense that would force
a fallback pattern — they're ordinary BCL types usable from any C# version once the target
framework includes them ((`StackTraceHidden`: .NET 6+; the rest: .NET Framework 2.0+). On an
older target framework, the attribute type simply isn't present — there's no lesser-featured
substitute to fall back to; omit it.
