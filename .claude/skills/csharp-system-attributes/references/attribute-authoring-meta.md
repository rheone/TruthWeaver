# Attribute-authoring meta attributes

Attributes you apply *to your own attribute classes*, and one closely related enum-shaping
attribute — relevant whenever you're defining a custom attribute type, not just consuming BCL
ones.

## `[AttributeUsage]`

`System.AttributeUsageAttribute(AttributeTargets validOn, ...)` — since .NET Framework 1.0.

Every custom attribute class should carry this — without it, the default is "usable on
absolutely anything" (`AttributeTargets.All`), which is almost never actually correct and gives
callers no compiler-enforced guardrail against putting it somewhere meaningless.

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AuditableAttribute : Attribute
{
    public string Reason { get; }
    public AuditableAttribute(string reason) => Reason = reason;
}
```

- `validOn` — restrict to exactly the targets that make semantic sense (`Class`, `Method`,
  `Property`, `Field`, `Parameter`, `All`, or a bitwise-OR combination). Getting this narrow is
  the main value: it turns "this attribute misapplied to a constructor" into a compiler error
  instead of a runtime surprise when whatever reads the attribute doesn't find it.
- `AllowMultiple` (default `false`) — whether the same attribute type can be applied more than
  once to the same target. Set `true` only when stacking genuinely makes sense (e.g. multiple
  `[Route]`-style attributes); leaving the default `false` catches accidental duplication.
- `Inherited` (default `true`) — whether the attribute applies to overriding members/derived
  types automatically, or only to the exact member/type it's placed on. Set `false` explicitly
  when the attribute encodes something that shouldn't silently propagate through inheritance
  (e.g. a "this specific override was audited" marker).

**When to add it proactively**: on every custom attribute class, as part of writing it — not as
an afterthought. Always specify `validOn` explicitly; don't rely on the `AttributeTargets.All`
default.

Attribute classes should also normally be `sealed` (a non-obvious but standard convention — the
BCL follows it universally) since attribute inheritance/polymorphism is rarely useful and sealing
lets consumers pattern-match on the concrete type with confidence.

## `[Flags]`

`System.FlagsAttribute` — since .NET Framework 1.0. Not attribute-authoring-specific, but grouped
here as the other "shapes how a custom type is treated" declaration-site attribute developers
reach for alongside `[AttributeUsage]`.

Applied to an `enum`, signals that its values are meant to be combined with bitwise OR, and
changes `ToString()`/formatting to render the combination as a comma-separated list of flags
instead of falling back to the raw numeric value.

```csharp
[Flags]
public enum FilePermissions
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Execute = 1 << 2,
    ReadWrite = Read | Write,
}
```

**When to add it proactively**: any enum whose members are designed to be OR-combined (explicit
power-of-two values, or combination members like `ReadWrite` above) — without `[Flags]`, the
enum still *works* for bitwise combination, but `ToString()` on a combined value prints an
unhelpful raw number instead of the flag names, and analyzers/IDEs won't recognize the
OR-combination intent. Conversely, don't add it to an enum whose members aren't actually meant to
be combined — it's a signal, and a false one misleads callers into trying combinations that make
no domain sense.

## Fallback / no-op behavior

Both attributes are .NET Framework 1.0-era BCL types with no version-gating concerns on any
currently supported target — always available, no fallback needed.
