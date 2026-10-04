# Pre-history: APM, EAP, and bare TAP (before C# 5.0 / .NET Framework 4.5)

Before `async`/`await` existed, .NET went through three successive asynchronous patterns, each
layered on the last rather than replacing it outright: the **Asynchronous Programming Model**
(APM, .NET Framework 1.0), the **Event-based Asynchronous Pattern** (EAP, .NET Framework 2.0),
and the **Task-based Asynchronous Pattern** (TAP) using the bare `Task`/`Task<T>` types introduced
in .NET Framework 4.0 — three years before C# 5.0 added the `async`/`await` keywords that made TAP
the default way to *write* asynchronous code rather than just a type you could return.

## APM: `Begin`/`End` and `IAsyncResult` (.NET Framework 1.0+)

An asynchronous operation exposes a `BeginX`/`EndX` method pair. `BeginX` starts the operation and
returns an `IAsyncResult`; `EndX` blocks (or returns immediately if already complete) and produces
the result or rethrows the exception that occurred during the operation:

```csharp
public class Downloader
{
    public IAsyncResult BeginDownload(string url, AsyncCallback callback, object state) =>
        // WebRequest-era API; illustrative shape.
        new SlowOperation(url).BeginInvoke(callback, state);

    public string EndDownload(IAsyncResult result) =>
        (string)((SlowOperation.SlowOperationDelegate)((AsyncResult)result).AsyncDelegate)
            .EndInvoke(result);
}
```

The callback-driven form:

```csharp
downloader.BeginDownload(url, ar =>
{
    string content = downloader.EndDownload(ar);
    Console.WriteLine(content);
}, state: null);
```

Delegates themselves gained `BeginInvoke`/`EndInvoke` for free — any delegate type could be called
asynchronously this way, which made APM the universal fallback even for types with no purpose-built
async API.

## EAP: `MethodNameAsync` plus a completion event (.NET Framework 2.0+)

EAP wraps the same idea in an object-oriented, event-driven shape: a method named `MethodAsync`
starts the operation, and a `MethodCompleted` event (carrying an `EventArgs`-derived payload with
`Error`, `Cancelled`, and `Result`/output properties) reports completion:

```csharp
public class Downloader
{
    public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

    public void DownloadAsync(string url)
    {
        // Kicks off work on a background thread, then raises DownloadCompleted.
    }
}
```

```csharp
downloader.DownloadCompleted += (sender, e) =>
{
    if (e.Error is not null)
    {
        Console.WriteLine($"Failed: {e.Error.Message}");
        return;
    }
    Console.WriteLine(e.Result);
};
downloader.DownloadAsync(url);
```

`WebClient` and the `BackgroundWorker` component are the canonical EAP examples — both predate
`async`/`await` by years and both are still visible in older codebases.

## Bare TAP: `Task`/`Task<T>` without language support (.NET Framework 4.0)

.NET Framework 4.0 introduced `System.Threading.Tasks.Task` and `Task<TResult>` as first-class
representations of "an operation that will complete in the future" — but C# 4.0 had no `async`/
`await` keywords yet, so composing tasks meant chaining `ContinueWith` by hand:

```csharp
Task<string> downloadTask = Task.Factory.StartNew(() => DownloadContent(url));

downloadTask.ContinueWith(t =>
{
    if (t.IsFaulted)
    {
        Console.WriteLine($"Failed: {t.Exception!.InnerException!.Message}");
        return;
    }
    Console.WriteLine(t.Result);
}, TaskContinuationOptions.ExecuteSynchronously);
```

Nesting several dependent steps this way produces the "pyramid of `ContinueWith`" that `async`/
`await` was built specifically to replace — the `Task` object itself didn't change meaning when
C# 5.0 shipped; what changed was that the compiler could now generate the continuation chain for
you from linear-looking code.

## Requirements and restrictions

- APM requires a matching `BeginX`/`EndX` pair per operation (or a delegate to piggyback on); there
  is no generic APM wrapper without one.
- EAP's completion event fires on whatever thread context the component chooses to marshal to
  (frequently the UI thread via `SynchronizationContext`, which is *why* the pattern exists — it
  predates a general-purpose way to hop back to the UI thread).
- Bare TAP (`Task` without `async`/`await`) has no cancellation, exception-unwrapping, or
  continuation-context conveniences beyond what `ContinueWith`'s options provide by hand; the
  `AggregateException` wrapping that `Task.Wait()`/`.Result` produce on failure is why later
  `await`-based code special-cases single-exception unwrapping (see
  [specialized/exception-handling-in-async-code.md](../specialized/exception-handling-in-async-code.md)).

## Fallback

None — this is the floor. On a codebase that cannot move past .NET Framework 4.0 without
`async`/`await`, this file *is* the target: wrap legacy APM/EAP members behind `TaskCompletionSource`
if you need a `Task`-shaped façade without waiting for language support, or use `Task.Factory.FromAsync`
(also .NET Framework 4.0+) to bridge an existing `BeginX`/`EndX` pair into a plain `Task<TResult>`.
Once `async`/`await` becomes available, port forward to
[csharp5-async-await.md](csharp5-async-await.md).
