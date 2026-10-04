# Runtime Async and other allocation/performance concerns

Performance-tuning knobs for async code that aren't gated by C# language version the way the
`references/` tiers are — either because they're runtime/BCL features (Runtime Async, pooling
builders) rather than new syntax, or because they're usage guidance that applies across every tier
in this skill.

## RC caveat: Runtime Async

**Runtime Async is a .NET 11 preview/RC-stage runtime feature, not yet GA as of this session
(.NET 11 RC1 shipped September 2026; .NET 11 GA is expected November 2026).** It ships alongside
C# 15 (also RC-stage as of this session — see the version table in
[SKILL.md](../SKILL.md)), but it is a compiler-codegen and runtime change, not new C# syntax: the
`async`/`await` keywords themselves are unchanged, so nothing in the `references/` tiers of this
skill needs a Runtime Async–specific tier. Details below may still shift before GA.

## Basic: what Runtime Async changes

Historically, the C# compiler lowers every `async` method into a heap-allocated state machine
object; `await`ing an incomplete operation suspends by storing captured locals into that object and
registering a continuation. Runtime Async moves that responsibility into the CoreCLR runtime
itself: the compiler emits a much smaller IL contract, and the JIT/runtime decide suspension and
resumption directly — producing cleaner stack traces (no more `MoveNext`/state-machine frames
obscuring the real call stack in a debugger or exception) and typically fewer allocations per
suspended `await`.

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <Features>runtime-async=on</Features>
</PropertyGroup>
```

No source code changes are required to opt in — every existing `async`/`await` method in the
project benefits (or is affected) once the feature is enabled. It's a project-wide, opt-in switch,
not a per-method attribute.

## Advanced: earlier targets need an additional flag

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <EnablePreviewFeatures>true</EnablePreviewFeatures>
  <Features>$(Features);runtime-async=on</Features>
</PropertyGroup>
```

Targeting `net11.0` directly (once GA) is expected to make `<EnablePreviewFeatures>` unnecessary;
targeting an earlier preview framework moniker, or an earlier .NET 11 preview build, needs both
properties together. Opting back out on a `net11.0` project uses `<UseRuntimeAsync>false</UseRuntimeAsync>`.

## Advanced: allocation-conscious async without Runtime Async

Independent of Runtime Async, the same allocation-avoidance techniques that predate it remain
relevant on any GA target:

- Prefer `ValueTask`/`ValueTask<T>` over `Task`/`Task<T>` for methods that frequently complete
  synchronously (a cache hit, a buffered read) — see
  [csharp7-task-like-types.md](../references/csharp7-task-like-types.md).
- Opt a verified hot-path method into a pooling builder with
  `[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]` — see
  [csharp10-async-method-builder-on-methods.md](../references/csharp10-async-method-builder-on-methods.md).
  Once a `ValueTask<T>` produced by a pooling builder has been awaited, its underlying state may be
  reused for a different call — never retain a reference derived from an already-awaited
  `ValueTask<T>`.
- Avoid `async` on a method that never actually needs to suspend — an `async` method with no real
  `await` inside it (see [csharp5-async-await.md](../references/csharp5-async-await.md)'s CS1998
  note) still pays the state-machine cost for nothing.

## Fallback

Below .NET 11, Runtime Async is unavailable — every `async` method compiles to the traditional
compiler-generated state machine, exactly as described in every `references/` tier of this skill.
None of the allocation-conscious guidance above (`ValueTask<T>`, pooling builders) depends on
Runtime Async; both remain the right techniques on any earlier GA target.
