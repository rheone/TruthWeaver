---
name: csharp-async
description: Reference for C# asynchronous programming — async/await, Task/Task<T>, ValueTask/ValueTask<T>, IAsyncEnumerable<T> async streams, await foreach, await using/IAsyncDisposable, cancellation, and ConfigureAwait — from pre-async APM/EAP patterns (.NET Framework 1.0+) through async/await itself (C# 5.0 / .NET Framework 4.5), await in catch/finally (C# 6.0), generalized async return types enabling ValueTask (C# 7.0), async Main (C# 7.1), async streams and async disposal (C# 8.0 / .NET Core 3.0), per-method AsyncMethodBuilder (C# 10), and ref/unsafe in async methods (C# 13 / .NET 9). Use when writing, reviewing, or porting async/await code, choosing between Task and ValueTask, producing or consuming an async stream, wiring cancellation through async calls, deciding where ConfigureAwait belongs, writing async tests, or gating async syntax by C# language version.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# Async

Asynchronous programming in C# has gone through three eras: the pre-language-support APM/EAP
patterns, `async`/`await` as first-class syntax starting in C# 5.0, and steady refinement since
(what can be an async return type, what can appear inside an async method body, how async streams
are produced and consumed). The baseline in
[references/csharp5-async-await.md](references/csharp5-async-await.md) still compiles unchanged on
C# 15.

## Quick start (works everywhere, C# 5.0+)

```csharp
public async Task<Order> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
{
    Order order = await _repository.GetOrderAsync(orderId, cancellationToken);
    return order;
}
```

## Pick your reference file

Load the file matching your target; each one names its fallback for older targets.

| Target | C# language version | Reference file |
| --- | --- | --- |
| .NET Framework 1.0 – 4.0 | C# 1.0 – 4.0 | [references/pre-csharp5-apm-eap-tap.md](references/pre-csharp5-apm-eap-tap.md) — APM (`BeginX`/`EndX`), EAP (`MethodAsync` + `MethodCompleted`), and bare `Task`/`ContinueWith` before language support |
| .NET Framework 4.5+ | C# 5.0+ | [references/csharp5-async-await.md](references/csharp5-async-await.md) — `async`/`await`, `Task`/`Task<T>`; the universal baseline |
| .NET Framework 4.6+ | C# 6.0+ | [references/csharp6-await-in-catch-finally.md](references/csharp6-await-in-catch-finally.md) — `await` inside `catch`/`finally` blocks |
| .NET Core 1.0+ / .NET Framework 4.6.2+ | C# 7.0+ | [references/csharp7-task-like-types.md](references/csharp7-task-like-types.md) — generalized async return types; enables `ValueTask<T>` (BCL type, .NET Core 2.0+) |
| .NET Core 2.0+ | C# 7.1+ | [references/csharp7.1-async-main.md](references/csharp7.1-async-main.md) — `async Main` entry point |
| .NET Core 3.0+ | C# 8.0+ | [references/csharp8-async-streams.md](references/csharp8-async-streams.md) — `IAsyncEnumerable<T>`, `await foreach`, `await using`/`IAsyncDisposable` |
| .NET 6+ | C# 10+ | [references/csharp10-async-method-builder-on-methods.md](references/csharp10-async-method-builder-on-methods.md) — `[AsyncMethodBuilder]` on individual methods, per-call-site pooling builders |
| .NET 9+ | C# 13+ | [references/csharp13-ref-unsafe-in-async.md](references/csharp13-ref-unsafe-in-async.md) — `ref` locals and `unsafe` contexts in async methods |
| .NET 10 (C# 14); .NET 11 RC1+ as of Sept 2026, GA expected Nov 2026 (C# 15) | C# 14; C# 15 | no reference file — neither version adds new async-specific syntax; see [specialized/runtime-async-performance.md](specialized/runtime-async-performance.md) for .NET 11's separate, non-syntax Runtime Async runtime feature |

## Specialized patterns

- [specialized/cancellation-with-cancellationtoken.md](specialized/cancellation-with-cancellationtoken.md) — threading `CancellationToken` through async calls, linked tokens, `[EnumeratorCancellation]`
- [specialized/configureawait-and-synchronization-context.md](specialized/configureawait-and-synchronization-context.md) — `ConfigureAwait(false)`, `ConfigureAwaitOptions` (.NET 8+), library vs. application guidance
- [specialized/generic-async-methods-and-task-of-t.md](specialized/generic-async-methods-and-task-of-t.md) — generic async methods, `Task<T>` over generic `T`, generic async delegates and streams
- [specialized/async-disposal-patterns.md](specialized/async-disposal-patterns.md) — `IAsyncDisposable` alongside `IDisposable`, `DisposeAsyncCore`, disposing collections of async-disposables
- [specialized/exception-handling-in-async-code.md](specialized/exception-handling-in-async-code.md) — `await`'s unwrapping vs. `AggregateException` from `.Wait()`/`.Result`/`Task.WhenAll`, cancellation vs. failure
- [specialized/testing-async-code.md](specialized/testing-async-code.md) — async test methods, avoiding `async void` in tests, asserting on async streams and cancellation
- [specialized/runtime-async-performance.md](specialized/runtime-async-performance.md) — .NET 11's Runtime Async runtime feature (RC caveat), `ValueTask<T>` and pooling-builder allocation guidance
