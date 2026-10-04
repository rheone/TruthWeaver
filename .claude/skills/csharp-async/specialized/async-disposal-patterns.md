# Async disposal patterns (`IAsyncDisposable`, `await using`)

`IAsyncDisposable`/`await using` (C# 8.0 — see
[csharp8-async-streams.md](../references/csharp8-async-streams.md)) exist for resources whose
cleanup genuinely needs to be asynchronous: closing a network connection with a graceful shutdown
handshake, flushing a buffered async writer. This file goes deeper than that reference tier's own
introduction into the patterns for combining sync and async disposal on one type, and the
`DisposeAsyncCore` idiom for a type with its own subclasses.

## Basic: a type that is only asynchronously disposable

```csharp
public sealed class RemoteSession : IAsyncDisposable
{
    private readonly Connection _connection;

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseGracefullyAsync();
        _connection.Dispose();
    }
}
```

```csharp
await using RemoteSession session = await RemoteSession.OpenAsync(endpoint);
await session.RunAsync();
```

## Basic: implementing both `IDisposable` and `IAsyncDisposable`

Do this when the type has legitimate synchronous callers too — `await using` is used when one is
available, `using` still works as a (blocking) fallback:

```csharp
public sealed class BufferedWriter : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _stream.Flush();
        _stream.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _stream.FlushAsync();
        await _stream.DisposeAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

## Advanced: `DisposeAsyncCore` for a base class with async-disposable subclasses

Mirrors the classic `Dispose(bool disposing)` pattern, so a derived class can add its own async
cleanup without re-implementing the public `DisposeAsync()` contract:

```csharp
public class ManagedResource : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await _handle.CloseAsync();
    }
}

public sealed class PooledResource : ManagedResource
{
    protected override async ValueTask DisposeAsyncCore()
    {
        await ReturnToPoolAsync();
        await base.DisposeAsyncCore();
    }
}
```

## Advanced: disposing a collection of async-disposable items concurrently

```csharp
public static async ValueTask DisposeAllAsync(IEnumerable<IAsyncDisposable> resources)
{
    List<Exception> exceptions = [];
    foreach (IAsyncDisposable resource in resources)
    {
        try
        {
            await resource.DisposeAsync();
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }
    }
    if (exceptions.Count > 0)
    {
        throw new AggregateException(exceptions);
    }
}
```

Disposal failures on one resource shouldn't prevent the rest from being cleaned up — collect and
aggregate rather than letting the first exception abort the loop (see
[exception-handling-in-async-code.md](exception-handling-in-async-code.md) for `AggregateException`
handling on the caller's side).

## Fallback

Below C# 8.0, there is no `IAsyncDisposable`/`await using`. Expose a plain `Task CloseAsync()` (or
`ValueTask CloseAsync()`, from .NET Core 2.0+ — see
[csharp7-task-like-types.md](../references/csharp7-task-like-types.md)) method instead, and call it
explicitly inside a `try`/`finally`:

```csharp
RemoteSession session = await RemoteSession.OpenAsync(endpoint);
try
{
    await session.RunAsync();
}
finally
{
    await session.CloseAsync();
}
```

(`await` inside `finally` itself needs C# 6.0 — see
[csharp6-await-in-catch-finally.md](../references/csharp6-await-in-catch-finally.md); below that,
see that file's own fallback for restructuring around the restriction.)
