# Performance attributes

Attributes that hint or force the JIT's compilation strategy for a specific method, or change
what happens to local variables on method entry. Narrow, targeted tools — benchmark before and
after applying any of these to confirm they actually help; they're easy to add on intuition and
get wrong.

## `[MethodImpl(MethodImplOptions...)]`

`System.Runtime.CompilerServices.MethodImplAttribute` — since .NET Framework 1.0;
`AggressiveInlining` since .NET Framework 4.5; `AggressiveOptimization` since .NET Core 3.0.

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public int Add(int a, int b) => a + b;
```

- `AggressiveInlining` — hints the JIT to inline the call even past its normal size/complexity
  heuristics. Only worth it for genuinely tiny, extremely hot methods (a few IL instructions,
  called in a tight loop) — the JIT already inlines small methods on its own most of the time,
  so this is a hint for the cases it's on the fence about, not a guaranteed override, and
  over-applying it can bloat code size and hurt instruction-cache behavior.
- `AggressiveOptimization` — tells the JIT to fully tier-up-optimize the method immediately
  instead of starting with a fast-but-unoptimized tier-0 JIT (the default tiered-compilation
  behavior). Useful for a method known to be hot from the very first call (e.g. a startup-path
  hot loop) where paying tier-0's slower codegen even briefly is undesirable.
- `NoInlining` / `NoOptimization` — the opposite hints, mainly useful for isolating a method in a
  profiler/debugger or working around a JIT bug in a specific method.

**When to add it proactively**: essentially never speculatively — this category is a response to
a measured hot path (a profiler trace, a benchmark regression), not a default applied while
writing new code. Flag it in review if seen without an accompanying benchmark.

## `[SkipLocalsInit]`

`System.Runtime.CompilerServices.SkipLocalsInitAttribute` — since C# 9 / .NET 5. Requires
`AllowUnsafeBlocks` (compiler restriction — the attribute itself doesn't require `unsafe` code,
but the compiler currently only recognizes it in assemblies where unsafe code is permitted).

Suppresses the CLR's default zero-initialization of local variables (the C# language guarantees
definite assignment independently, so the zero-init the runtime does underneath is normally pure
overhead once the compiler's own definite-assignment checking already covers correctness).

```csharp
[SkipLocalsInit]
public unsafe void FillBuffer(byte* buffer, int length)
{
    Span<byte> local = stackalloc byte[256]; // skips the zero-fill for this stack allocation
    // ...
}
```

**When to add it proactively**: hot paths using `stackalloc` or large local structs, where the
zero-init cost is measurable — again, a measured-hotpath tool, not a default. Never apply it to
code where an uninitialized read is actually reachable (e.g. via unsafe pointer arithmetic that
outruns what the C# definite-assignment analysis covers) — that turns a correctness guarantee
into reading genuinely arbitrary stack garbage.

## Fallback / no-op behavior

`MethodImplOptions.AggressiveInlining`/`NoInlining`/`NoOptimization`: available since .NET
Framework 1.0/4.5 — no fallback needed on any supported target. `AggressiveOptimization`: .NET
Core 3.0+; on older targets simply omit it (tiered compilation itself may not exist there either).
`[SkipLocalsInit]`: C# 9/.NET 5+; on an older target or without `AllowUnsafeBlocks`, omit it — the
JIT falls back to its default zero-init behavior, which is a correctness-safe no-op cost, never a
functional gap.
