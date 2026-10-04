# Async streams: `IAsyncEnumerable<T>`, `await foreach`, `await using` (C# 8.0 / .NET Core 3.0)

C# 8.0 shipped September 2019 alongside .NET Core 3.0 — the first C# release built specifically
for .NET Core, relying on new CLR and library capabilities unavailable on .NET Framework. For
async programming, it adds three related pieces: **asynchronous streams** (`IAsyncEnumerable<T>`
plus `await foreach` to consume them, using `yield return` inside an `async` iterator method to
produce them), and **asynchronous disposal** (`IAsyncDisposable` plus `await using`) for resources
whose cleanup is itself asynchronous.

## Syntax

```csharp
public async IAsyncEnumerable<Order> GetOrdersAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (Order order in _database.QueryOrdersAsync(cancellationToken))
    {
        yield return order;
    }
}
```

```csharp
await foreach (Order order in ordersService.GetOrdersAsync(cancellationToken))
{
    Process(order);
}
```

```csharp
await using DbConnection connection = await _factory.OpenConnectionAsync();
// connection.DisposeAsync() runs automatically at the end of scope.
```

## Basic use case: streaming query results without buffering them all in memory

```csharp
public async IAsyncEnumerable<LogEntry> ReadLogEntriesAsync(string path)
{
    await using FileStream stream = File.OpenRead(path);
    using StreamReader reader = new(stream);
    string? line;
    while ((line = await reader.ReadLineAsync()) is not null)
    {
        if (TryParse(line, out LogEntry entry))
        {
            yield return entry;
        }
    }
}
```

```csharp
await foreach (LogEntry entry in ReadLogEntriesAsync("app.log"))
{
    if (entry.Level == LogLevel.Error)
    {
        Console.WriteLine(entry);
    }
}
```

Only one log entry is materialized at a time — a plain `Task<List<LogEntry>>` would have to load
the entire file into memory first.

## Advanced use case: a generic async stream with cancellation and `ConfigureAwait`

```csharp
public static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
    this IAsyncEnumerable<TSource> source,
    Func<TSource, Task<TResult>> selector,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
        yield return await selector(item).ConfigureAwait(false);
    }
}
```

```csharp
await foreach (OrderSummary summary in
    ordersService.GetOrdersAsync(cancellationToken).SelectAsync(SummarizeAsync, cancellationToken))
{
    Console.WriteLine(summary);
}
```

`SelectAsync<TSource, TResult>` is a generic extension method over `IAsyncEnumerable<TSource>` —
the same generic-method shape as LINQ's `Select`, adapted to the async-stream form; `TResult` here
is exactly as ordinary a type parameter as in any non-async generic method.

## Requirements and restrictions

- `[EnumeratorCancellation]` (on a `CancellationToken` parameter of the iterator method itself) is
  what lets a caller's `.WithCancellation(token)` reach the token actually used inside the
  `await foreach`/`yield return` body — without it, a token passed via `WithCancellation` is
  silently ignored by the iterator. See
  [specialized/cancellation-with-cancellationtoken.md](../specialized/cancellation-with-cancellationtoken.md).
- An `async IAsyncEnumerable<T>` iterator method cannot also take `ref`, `out`, or (before C# 13)
  `ref struct` parameters — see
  [csharp13-ref-unsafe-in-async.md](csharp13-ref-unsafe-in-async.md) for what C# 13 relaxes here.
- `IAsyncEnumerator<T>.DisposeAsync()` (invoked automatically at the end of `await foreach`, or
  explicitly via `await using` if you obtain the enumerator directly) must be awaited — don't
  synchronously block on it, since the same synchronous-blocking deadlock risk that applies to any
  other `await` applies here. See
  [specialized/async-disposal-patterns.md](../specialized/async-disposal-patterns.md).

## Fallback

Below C# 8.0 / .NET Core 3.0, there is no `IAsyncEnumerable<T>`. The closest equivalent is
returning `Task<List<T>>` (materializing everything before returning — loses the streaming/backpressure
benefit) or, for genuinely large sequences, hand-rolling a pull-based async cursor:

```csharp
public interface IAsyncCursor<T>
{
    Task<bool> MoveNextAsync();
    T Current { get; }
}
```

```csharp
IAsyncCursor<LogEntry> cursor = ReadLogEntries("app.log");
while (await cursor.MoveNextAsync())
{
    Process(cursor.Current);
}
```

For `await using`/`IAsyncDisposable`, fall back to a synchronous `try`/`finally` calling a
`Task CloseAsync()`-style method directly and awaiting it inside `finally` (valid from C# 6.0
onward — see [csharp6-await-in-catch-finally.md](csharp6-await-in-catch-finally.md)):

```csharp
DbConnection connection = await _factory.OpenConnectionAsync();
try
{
    await UseConnectionAsync(connection);
}
finally
{
    await connection.CloseAsync();
    connection.Dispose();
}
```
