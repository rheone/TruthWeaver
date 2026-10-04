# Generic async methods and `Task<T>`/`ValueTask<T>` composition

Async methods and generics compose without any special-case rules — a type parameter can appear
anywhere an ordinary type could: as the `T` in `Task<T>`/`ValueTask<T>`, as an async method's own
type parameter, or inside a generic delegate an async method accepts or returns. This file collects
the shapes that come up often enough to be worth naming, all buildable from
[csharp5-async-await.md](../references/csharp5-async-await.md)'s baseline plus ordinary generics.

## Basic: a generic async method

```csharp
public async Task<T> GetFirstOrDefaultAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
{
    await foreach (T item in source.WithCancellation(cancellationToken))
    {
        return item;
    }
    return default!;
}
```

`T` is inferred from the `IAsyncEnumerable<T>` argument at the call site, exactly as it would be
for a non-async generic method — `async` adds nothing to how type inference works here.

## Basic: `Task<T>` where `T` is itself a closed generic type

```csharp
public async Task<Dictionary<string, List<Order>>> GetOrdersByRegionAsync()
{
    List<Order> allOrders = await _repository.GetAllOrdersAsync();
    return allOrders
        .GroupBy(o => o.Region)
        .ToDictionary(g => g.Key, g => g.ToList());
}
```

Nothing about `async` restricts what `T` in `Task<T>` can be — `Dictionary<string, List<Order>>`
nests generic types the same way it would outside an async method.

## Advanced: a generic delegate parameter returning a generic task

```csharp
public async Task<TResult> WithRetryAsync<TResult>(
    Func<CancellationToken, Task<TResult>> operation,
    int maxAttempts,
    CancellationToken cancellationToken = default)
{
    for (int attempt = 1; attempt < maxAttempts; attempt++)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
        }
    }
    return await operation(cancellationToken);
}
```

```csharp
Order order = await WithRetryAsync(
    ct => _repository.GetOrderAsync(orderId, ct),
    maxAttempts: 3,
    cancellationToken);
```

`Func<CancellationToken, Task<TResult>>` is an ordinary generic delegate; `WithRetryAsync` is itself
generic over `TResult`, inferred here from the lambda's return type. This is the standard shape for
resilience/retry helpers, timeout wrappers, and similar cross-cutting async utilities.

## Advanced: a generic `IAsyncEnumerable<T>` transform

```csharp
public static async IAsyncEnumerable<TResult> SelectAwaitAsync<TSource, TResult>(
    this IAsyncEnumerable<TSource> source,
    Func<TSource, CancellationToken, ValueTask<TResult>> selector,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (TSource item in source.WithCancellation(cancellationToken))
    {
        yield return await selector(item, cancellationToken);
    }
}
```

Two independent type parameters (`TSource`, `TResult`) on a generic async-stream extension method —
needs C# 8.0 for `IAsyncEnumerable<T>`/`await foreach`/`yield return` inside an `async` iterator;
see [csharp8-async-streams.md](../references/csharp8-async-streams.md).

## Fallback

The generic-method and `Task<T>`-of-generic-`T` shapes need nothing beyond
[csharp5-async-await.md](../references/csharp5-async-await.md) (C# 5.0) plus C# 2.0 generics — no
fallback required below the skill's own baseline. The `IAsyncEnumerable<T>` example specifically
needs C# 8.0; below that, write the equivalent as a `Task<List<TResult>>`-returning generic method
that materializes eagerly, per
[csharp8-async-streams.md#fallback](../references/csharp8-async-streams.md#fallback).
