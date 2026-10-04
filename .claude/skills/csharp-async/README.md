# C# Async and Await

Helps you write, review, or port asynchronous C# code (`async`/`await`, `Task`/`ValueTask`, async
streams, cancellation, and `ConfigureAwait`) across the full history of .NET's async patterns,
from the pre-`async` APM/EAP era through the latest language and runtime changes.

## When to reach for it

- Deciding whether a method should return `Task<T>` or `ValueTask<T>`
- Threading a `CancellationToken` correctly through a chain of async calls
- Producing or consuming an `IAsyncEnumerable<T>` async stream with `await foreach`
- Porting old `BeginX`/`EndX` or event-based async code to `async`/`await`
- Deciding where `ConfigureAwait(false)` belongs in a library vs. an application

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing,
reviewing, or porting async code, or choosing between async return types.

## What it covers

| Topic | Reference |
| --- | --- |
| Pre-`async` APM/EAP/bare-Task patterns | [references/pre-csharp5-apm-eap-tap.md](references/pre-csharp5-apm-eap-tap.md) |
| `async`/`await` fundamentals (the universal baseline) | [references/csharp5-async-await.md](references/csharp5-async-await.md) |
| `await` inside `catch`/`finally` | [references/csharp6-await-in-catch-finally.md](references/csharp6-await-in-catch-finally.md) |
| Task-like return types and `ValueTask<T>` | [references/csharp7-task-like-types.md](references/csharp7-task-like-types.md) |
| `async Main` entry points | [references/csharp7.1-async-main.md](references/csharp7.1-async-main.md) |
| Async streams, `await foreach`, `await using` | [references/csharp8-async-streams.md](references/csharp8-async-streams.md) |
| Per-method `AsyncMethodBuilder` | [references/csharp10-async-method-builder-on-methods.md](references/csharp10-async-method-builder-on-methods.md) |
| `ref`/`unsafe` inside async methods | [references/csharp13-ref-unsafe-in-async.md](references/csharp13-ref-unsafe-in-async.md) |

## Example prompts

- "Should this repository method return `Task<Order>` or `ValueTask<Order>`?"
- "Convert this old `BeginRead`/`EndRead` pair to `async`/`await`."
- "How do I thread a `CancellationToken` through this async pipeline properly?"
