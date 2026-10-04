# Analyzer suppression attributes

Attributes that suppress a specific static-analysis or trimming/AOT diagnostic at a specific
scope — narrower and more auditable than a blanket `#pragma warning disable` because they carry
a required justification and are scoped to exactly the member they're placed on.

## `[SuppressMessage]`

`System.Diagnostics.CodeAnalysis.SuppressMessageAttribute` — since .NET Framework 1.1
(originally for FxCop; now used by Roslyn analyzers, including built-in code-quality analyzers
and third-party ones like StyleCop).

```csharp
[SuppressMessage("Design", "CA1062:Validate arguments of public methods",
    Justification = "Guaranteed non-null by DI container registration.")]
public void Configure(IOptions<Settings> options) => _settings = options.Value;
```

- `Justification` isn't required by the compiler but should be treated as mandatory in practice
  — a suppression with no stated reason is indistinguishable from "someone silenced this without
  understanding it," and is exactly what a future reviewer needs to judge whether the suppression
  is still valid.
- Prefer this attribute, scoped to the single offending member, over a file-level or
  project-wide `<NoWarn>`/`.editorconfig` severity downgrade — the attribute-scoped form means
  the *next* violation of the same rule elsewhere in the file still gets flagged, where a broader
  suppression silences all of them.
- Compare to `[Obsolete]`'s `DiagnosticId`/`UrlFormat` pattern
  ([api-lifecycle-contracts.md](api-lifecycle-contracts.md)) — different mechanism, same
  underlying goal of narrowly-scoped, self-documenting diagnostic control.

**When to add it proactively**: when an analyzer flags something that's a genuine, understood
false positive for this specific case — never as a reflexive way to make a warning go away
without reading what it's telling you. If the analyzer is right and the code can reasonably be
changed to satisfy it, fix the code instead of suppressing the warning.

## `[UnconditionalSuppressMessage]`

`System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute` — since .NET 5.

Same shape and purpose as `[SuppressMessage]`, but specifically for trimming/AOT-analysis
diagnostics (`IL2026`, `IL3050`, etc. — reflection-based patterns the trimmer/AOT compiler can't
statically verify are safe). It exists as a distinct type because trimming diagnostics are
suppressed based on a *runtime-independent* (unconditional) analysis pass, separate from the
regular Roslyn analyzer pipeline `[SuppressMessage]` targets — using the wrong one of the two
doesn't suppress the diagnostic it's aimed at.

```csharp
[UnconditionalSuppressMessage("Trimming", "IL2075",
    Justification = "Type is preserved via [DynamicallyAccessedMembers] on the caller.")]
private static PropertyInfo[] GetProperties(Type t) => t.GetProperties();
```

**When to add it proactively**: only when publishing a library or app with trimming/NativeAOT
enabled and a specific, understood reflection pattern trips the trimmer's analysis despite being
safe (usually because the safety depends on a guarantee the trimmer can't see, like a
`[DynamicallyAccessedMembers]` annotation on a caller). Prefer fixing the reflection pattern to be
trim-safe (e.g. via `[DynamicallyAccessedMembers]`) over suppressing when that's feasible — the
suppression documents an exception, not a preferred first response.

## Fallback / no-op behavior

`[SuppressMessage]`: .NET Framework 1.1+, always available. `[UnconditionalSuppressMessage]`:
.NET 5+; on an older target, trimming/AOT analysis doesn't apply in the first place, so there's
nothing to suppress — omit it rather than substitute anything.
