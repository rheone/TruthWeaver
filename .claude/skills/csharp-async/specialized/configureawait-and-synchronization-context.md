# `ConfigureAwait` and the synchronization context

By default, `await`ing a `Task`/`ValueTask` captures the current `SynchronizationContext` (present
in UI frameworks and older ASP.NET) or, absent one, the current `TaskScheduler`, and resumes the
rest of the method on it. `ConfigureAwait(false)` opts out, letting the continuation run on
whatever thread-pool thread completed the awaited operation. This applies identically from
[csharp5-async-await.md](../references/csharp5-async-await.md) onward; C# 8.0 async streams add
their own place to apply it (shown below), and .NET 8 added a richer options-based overload.

## Basic: library code avoiding a UI-thread hop it doesn't need

```csharp
public async Task<string> DownloadContentAsync(string url)
{
    using HttpClient client = new();
    return await client.GetStringAsync(url).ConfigureAwait(false);
}
```

Library/business-logic code generally has no reason to resume on the original context — every
`await` in the method (and any method it calls that also uses `ConfigureAwait(false)`
consistently) can run its continuation on a thread-pool thread, which avoids the classic deadlock
where a UI-thread caller blocks synchronously (`.Result`/`.Wait()`) on a task whose continuation is
waiting for that same UI thread to free up.

## Basic: application/UI code that *needs* the original context

```csharp
private async void OnDownloadButtonClicked(object sender, EventArgs e)
{
    string content = await _service.DownloadContentAsync(url); // no ConfigureAwait(false) here
    _statusLabel.Text = content; // must run back on the UI thread
}
```

Top-level UI event handlers and ASP.NET (classic, pre-Core) request-handling code are the layer
that *wants* the context captured — `ConfigureAwait(false)` belongs in the library code being
called, not necessarily at every call site indiscriminately.

## Advanced: `ConfigureAwaitOptions` (.NET 8+)

```csharp
public async Task ProcessAsync(CancellationToken cancellationToken)
{
    // SuppressThrowing: observe a faulted/cancelled task without an exception,
    // useful for fire-and-forget-with-cleanup patterns.
    await _backgroundTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}
```

`Task.ConfigureAwait(ConfigureAwaitOptions)` (a `[Flags]` enum: `ContinueOnCapturedContext`,
`SuppressThrowing`, `ForceYielding`) is a .NET 8 runtime library addition — not a C# language
version feature — available on `Task`/`Task<T>` but, as of .NET 8, not on `ValueTask`/`ValueTask<T>`.
It's a BCL API, so it's usable from any C# language version once the target framework is .NET 8+;
it isn't gated by `LangVersion` the way the reference-tier features in this skill are.

## Advanced: `ConfigureAwait` on an async stream

```csharp
public async Task ConsumeAsync(IAsyncEnumerable<Order> orders, CancellationToken cancellationToken)
{
    await foreach (Order order in orders.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        Process(order);
    }
}
```

`ConfigureAwaitAsyncEnumerable<T>.ConfigureAwait(bool)` extends the same opt-out to every `await`
implicit in the `await foreach` loop (each `MoveNextAsync()` and the final `DisposeAsync()`) — needs
C# 8.0 for `await foreach` itself; see
[csharp8-async-streams.md](../references/csharp8-async-streams.md).

## Fallback

`ConfigureAwait(bool)` on `Task`/`Task<T>`/`ValueTask`/`ValueTask<T>` has been available since
`Task` itself gained awaiter support in C# 5.0 — no fallback needed for the basic/advanced
`Task`-based examples above. The `ConfigureAwaitOptions` overload and the async-stream
`ConfigureAwait` extension each need their own prerequisite (.NET 8 runtime; C# 8.0 language
version, respectively) — below either, use the plain `ConfigureAwait(false)` boolean overload, or
(below C# 8.0 entirely) omit the async-stream example, since `await foreach` doesn't exist yet.
