# `async`/`await`, Task, and Task&lt;T&gt; (C# 5.0 / .NET Framework 4.5)

C# 5.0 shipped August 15, 2012 with Visual Studio 2012 and .NET Framework 4.5, and nearly the
entire release was this one feature: the `async` and `await` keywords, which let the compiler
generate the state machine that used to be hand-written as a chain of `ContinueWith` calls (see
[pre-csharp5-apm-eap-tap.md](pre-csharp5-apm-eap-tap.md)). `Task` and `Task<TResult>` themselves
already existed from .NET Framework 4.0 — C# 5.0's contribution is the syntax that makes writing
and composing them read like ordinary sequential code.

## Syntax

```csharp
public async Task<string> DownloadContentAsync(string url)
{
    using var client = new HttpClient();
    string content = await client.GetStringAsync(url);
    return content;
}
```

`async` marks a method as containing `await` expressions; `await` suspends execution until the
awaited operation completes, without blocking the calling thread. The method's return type is one
of three shapes: `Task` (no result), `Task<TResult>` (a result), or `void` (fire-and-forget —
restricted to event handlers; see Requirements below).

## Basic use case

```csharp
public async Task<int> GetOrderCountAsync(int customerId)
{
    List<Order> orders = await _orderRepository.GetOrdersAsync(customerId);
    return orders.Count;
}
```

```csharp
int count = await GetOrderCountAsync(customerId: 42);
```

Callers of an `async Task<T>` method `await` it the same way they'd `await` any other task-returning
call — the asynchrony composes without the caller needing special handling.

## Advanced use case: composing several awaits, including generic ones

```csharp
public async Task<Dictionary<int, Order>> GetOrdersByIdAsync(IEnumerable<int> orderIds)
{
    Task<Order>[] tasks = orderIds.Select(id => _orderRepository.GetOrderAsync(id)).ToArray();
    Order[] orders = await Task.WhenAll(tasks);
    return orders.ToDictionary(o => o.Id);
}
```

`Task<Order>[]`, `Task.WhenAll<TResult>`, and the returned `Dictionary<int, Order>` are all
ordinary generic types — an async method's return type being `Task<T>` composes with generics
exactly the way any other method's return type does; there's no special interaction to learn
beyond "`T` in `Task<T>` can itself be a generic type," as here with `Dictionary<int, Order>`:

```csharp
public async Task<TResult> RetryAsync<TResult>(Func<Task<TResult>> operation, int maxAttempts)
{
    for (int attempt = 1; attempt < maxAttempts; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(attempt));
        }
    }
    return await operation();
}
```

A generic async method (`RetryAsync<TResult>`) taking a generic delegate (`Func<Task<TResult>>`)
that itself returns a generic task — this shape is common for retry/resilience helpers and needs
nothing beyond what C# 2.0 generics and C# 5.0 `async`/`await` each already provide independently.

## Requirements and restrictions

- `async void` is valid only for event handlers. It has no `Task` to observe, so exceptions thrown
  from it crash the process (or, for UI event handlers, the synchronization context) instead of
  propagating to a caller's `await` — never use it for anything callable, including test methods
  (see [specialized/testing-async-code.md](../specialized/testing-async-code.md)).
- An `async` method without any `await` inside it compiles but emits a warning (CS4014-adjacent —
  specifically CS1998) and runs synchronously; it's usually a sign the `async` modifier is
  unnecessary or an `await` was forgotten.
- Awaiting a `Task` captures the current `SynchronizationContext` (if any) and resumes on it by
  default — the behavior `ConfigureAwait(false)` opts out of; see
  [specialized/configureawait-and-synchronization-context.md](../specialized/configureawait-and-synchronization-context.md).
- `await` is only valid inside a method marked `async`; C# 5.0 does not allow `await` inside
  `catch`/`finally` blocks (see [csharp6-await-in-catch-finally.md](csharp6-await-in-catch-finally.md)).

## Fallback

Below .NET Framework 4.5 / C# 5.0, there is no `async`/`await` syntax. Write the operation as a
bare `Task`-returning method composed with `ContinueWith`, or bridge legacy APM/EAP members with
`TaskCompletionSource<TResult>` or `Task.Factory.FromAsync` — see
[pre-csharp5-apm-eap-tap.md](pre-csharp5-apm-eap-tap.md).
