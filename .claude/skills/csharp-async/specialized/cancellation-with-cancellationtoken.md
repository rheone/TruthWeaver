# Cancellation with `CancellationToken`

`CancellationToken` is the cooperative cancellation mechanism threaded through `Task`-, `ValueTask`-,
and `IAsyncEnumerable<T>`-based async code alike. It works the same way at every tier from
[csharp5-async-await.md](../references/csharp5-async-await.md) onward; async streams add one extra
wiring detail (`[EnumeratorCancellation]`), covered below, that only exists from
[csharp8-async-streams.md](../references/csharp8-async-streams.md).

## Basic: accepting and forwarding a token

```csharp
public async Task<Order> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
{
    return await _repository.GetOrderAsync(orderId, cancellationToken);
}
```

Every async method that calls another cancellable async method should accept a
`CancellationToken` parameter (default `= default`, i.e. `CancellationToken.None`) and pass it
straight through — cancellation only works end-to-end if every link in the chain forwards the same
token.

## Basic: producing a token with a timeout

```csharp
using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
try
{
    Order order = await GetOrderAsync(orderId, cts.Token);
}
catch (OperationCanceledException) when (cts.IsCancellationRequested)
{
    Console.WriteLine("Timed out.");
}
```

`OperationCanceledException` (the base type `TaskCanceledException` derives from) is what a
correctly-written cancellable operation throws when its token is signaled — catch it specifically
rather than a broad `Exception` so a genuine failure isn't mistaken for a cancellation.

## Advanced: linking a caller's token with a local timeout

```csharp
public async Task<Order> GetOrderWithTimeoutAsync(int orderId, CancellationToken cancellationToken)
{
    using CancellationTokenSource linkedCts =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    linkedCts.CancelAfter(TimeSpan.FromSeconds(5));

    return await _repository.GetOrderAsync(orderId, linkedCts.Token);
}
```

`CreateLinkedTokenSource` produces a token that fires when *either* the caller cancels or the local
timeout elapses — the operation can be cancelled from outside (the caller's token) without losing
its own internal deadline.

## Advanced: `[EnumeratorCancellation]` for async streams

```csharp
public async IAsyncEnumerable<Order> GetOrdersAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (Order order in _database.QueryOrdersAsync(cancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return order;
    }
}
```

```csharp
await foreach (Order order in ordersService.GetOrdersAsync().WithCancellation(cancellationToken))
{
    Process(order);
}
```

Without `[EnumeratorCancellation]` on the iterator method's own `CancellationToken` parameter, a
token supplied by the caller via `.WithCancellation(token)` is silently disconnected from the token
the method body actually observes — the compiler needs the attribute to know which parameter to
wire the caller's token into. This only applies to `IAsyncEnumerable<T>` iterator methods (C# 8.0+);
plain `Task`/`ValueTask`-returning methods just take the token as an ordinary parameter, as in the
basic examples above.

## Fallback

`CancellationToken`/`CancellationTokenSource` have existed since .NET Framework 4.0 (predating
`async`/`await` itself) and work identically at every C# tier this skill covers — nothing here needs
a fallback beyond whichever tier the surrounding `async`/`await` or `IAsyncEnumerable<T>` code
already requires.
