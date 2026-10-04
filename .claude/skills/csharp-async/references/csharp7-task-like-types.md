# Generalized async return types and `ValueTask<T>` (C# 7.0 / .NET Core 1.0+, .NET Framework 4.6.2+)

C# 7.0 shipped March 7, 2017 with Visual Studio 2017. Before this release, an `async` method could
only return `void`, `Task`, or `Task<TResult>` — the compiler recognized exactly those three
shapes. C# 7.0 generalizes this: any type with a `GetAwaiter()` method returning a valid awaiter
(one implementing `INotifyCompletion`/`ICriticalNotifyCompletion` with `IsCompleted` and
`GetResult()`), or any type annotated with `[AsyncMethodBuilder(...)]`, can be an async method's
return type. This is the language feature that makes `ValueTask`/`ValueTask<TResult>` usable as an
`async` method return type. `ValueTask`/`ValueTask<TResult>` themselves are BCL types (not part of
the C# 7.0 language release) that arrived shortly after, in .NET Core 2.0 (August 2017), and are
available on earlier targets via the `System.Threading.Tasks.Extensions` NuGet package.

## Syntax

```csharp
public async ValueTask<int> GetCachedCountAsync(int key)
{
    if (_cache.TryGetValue(key, out int cached))
    {
        return cached; // no Task allocation for the synchronous-completion path
    }
    int count = await _repository.GetCountAsync(key);
    _cache[key] = count;
    return count;
}
```

## Basic use case

`ValueTask<T>` pays off exactly where a result is frequently available synchronously (a cache hit,
a buffered read) and the method is called often enough that `Task<T>`'s per-call heap allocation
would matter:

```csharp
public ValueTask<Order> GetOrderAsync(int orderId)
{
    if (_recentOrders.TryGetValue(orderId, out Order? cached))
    {
        return new ValueTask<Order>(cached); // synchronous path, no Task allocated
    }
    return new ValueTask<Order>(LoadOrderFromDatabaseAsync(orderId));
}

private async Task<Order> LoadOrderFromDatabaseAsync(int orderId) =>
    await _database.QuerySingleAsync<Order>("...", orderId);
```

## Advanced use case: a custom task-like type via `AsyncMethodBuilder`

Most code should stick to `Task`/`Task<T>`/`ValueTask`/`ValueTask<T>` — the BCL builders behind
them are heavily tuned. Understanding the mechanism matters mainly for reading library code (game
engines, UI frameworks) that ships its own awaitable to avoid `SynchronizationContext` overhead or
to integrate with a bespoke scheduler:

```csharp
[AsyncMethodBuilder(typeof(CustomTaskMethodBuilder<>))]
public readonly struct CustomTask<T>
{
    // A minimal task-like type needs GetAwaiter() returning a type with
    // IsCompleted, GetResult(), and OnCompleted() — the same shape ValueTask<T> implements.
}
```

```csharp
public async CustomTask<int> ComputeAsync() => await Task.FromResult(42);
```

## Requirements and restrictions

- `ValueTask<T>` can be awaited **at most once**. Awaiting it twice, calling `.Result`/`.GetAwaiter().GetResult()`
  after already awaiting it, or storing it to await later from two places is undefined behavior —
  unlike `Task<T>`, which is safe to await repeatedly from multiple places. Convert to `Task<T>`
  via `.AsTask()` first if the value needs to be observed more than once.
- Don't call `.Result` or `.GetAwaiter().GetResult()` on an incomplete `ValueTask<T>` from
  synchronous code expecting `Task<T>`-like blocking semantics without understanding the underlying
  `IValueTaskSource<T>` may be pooled and reused once observed — see
  [specialized/runtime-async-performance.md](../specialized/runtime-async-performance.md) for the
  pooling mechanics.
- Prefer `Task`/`Task<T>` for any public API surface that callers might store, pass around, or
  await multiple times; reserve `ValueTask`/`ValueTask<T>` for hot-path, single-await call sites.

## Fallback

Below C# 7.0, an `async` method may only return `void`, `Task`, or `Task<TResult>` — write the
cache-check-then-fall-through shape from the basic example as a plain `Task<T>`-returning method
instead, accepting the allocation on every call:

```csharp
public Task<Order> GetOrderAsync(int orderId) =>
    _recentOrders.TryGetValue(orderId, out Order? cached)
        ? Task.FromResult(cached)
        : LoadOrderFromDatabaseAsync(orderId);
```

`Task.FromResult(cached)` is the pre-`ValueTask` idiom for "already have the result, still need a
`Task<T>` to return" — see [csharp5-async-await.md](csharp5-async-await.md).
