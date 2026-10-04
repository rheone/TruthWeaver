# Exception handling in async code

Exceptions behave differently depending on *how* an async operation is observed — `await`ing a
task unwraps and rethrows the single originating exception, while `.Wait()`/`.Result` (and
`Task.WhenAll`, when multiple tasks fault) surface an `AggregateException`. Getting this wrong is a
frequent source of `catch` blocks that never fire because they're catching the wrong exception
type. Applies from [csharp5-async-await.md](../references/csharp5-async-await.md) onward.

## Basic: `await` unwraps automatically

```csharp
public async Task ProcessOrderAsync(int orderId)
{
    try
    {
        await _repository.SaveOrderAsync(orderId);
    }
    catch (SqlException ex)
    {
        // Fires correctly: awaiting a faulted Task rethrows its single inner exception directly.
        await _logger.LogErrorAsync(ex);
        throw;
    }
}
```

## Basic: blocking with `.Result`/`.Wait()` wraps in `AggregateException`

```csharp
try
{
    _repository.SaveOrderAsync(orderId).Wait(); // avoid this — shown to explain the pitfall
}
catch (AggregateException ex)
{
    // ex.InnerException (or ex.InnerExceptions for multiple) holds the real SqlException —
    // a `catch (SqlException)` here would never match.
    SqlException? sqlEx = ex.InnerException as SqlException;
}
```

Prefer `await` over `.Wait()`/`.Result` throughout; where a synchronous call site genuinely cannot
be made `async` (e.g. bridging into legacy code), use `.GetAwaiter().GetResult()` instead — it
rethrows the original exception unwrapped, the same way `await` does, without introducing
`AggregateException`.

## Advanced: `Task.WhenAll` aggregates every faulted task's exception

```csharp
public async Task SaveAllAsync(IEnumerable<Order> orders)
{
    Task[] tasks = orders.Select(o => _repository.SaveOrderAsync(o)).ToArray();
    try
    {
        await Task.WhenAll(tasks);
    }
    catch (Exception)
    {
        // `await Task.WhenAll(...)` rethrows only the FIRST faulted task's exception directly —
        // to see every failure, inspect the tasks themselves afterward.
        IEnumerable<Exception> allErrors = tasks
            .Where(t => t.IsFaulted)
            .SelectMany(t => t.Exception!.InnerExceptions);
        foreach (Exception error in allErrors)
        {
            await _logger.LogErrorAsync(error);
        }
        throw;
    }
}
```

`await Task.WhenAll(tasks)` only rethrows the first exception encountered — if multiple tasks
faulted and every failure matters (not just the first), inspect `tasks` (or catch
`AggregateException` and enumerate `.InnerExceptions` from the `Task.WhenAll(...)` task itself
before awaiting it) rather than relying on the exception the `await` surfaces.

## Advanced: `OperationCanceledException` should not be treated as a failure

```csharp
public async Task<Order?> TryGetOrderAsync(int orderId, CancellationToken cancellationToken)
{
    try
    {
        return await _repository.GetOrderAsync(orderId, cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        return null; // expected shutdown path, not an error
    }
    catch (SqlException ex)
    {
        await _logger.LogErrorAsync(ex);
        throw;
    }
}
```

Catching `OperationCanceledException` unconditionally (without the `when` guard checking the
*caller's* token) risks silently swallowing a cancellation that originated from an unrelated,
unexpected source (e.g. an internal timeout the caller didn't ask for) — see
[cancellation-with-cancellationtoken.md](cancellation-with-cancellationtoken.md).

## Fallback

`await`'s single-exception unwrapping behavior is intrinsic to C# 5.0's `async`/`await` — no
fallback needed at any tier this skill covers. Below C# 5.0 (bare `Task`/`ContinueWith` code, see
[pre-csharp5-apm-eap-tap.md](../references/pre-csharp5-apm-eap-tap.md)), every fault observation
point (`.Result`, `.Wait()`, a `ContinueWith` continuation inspecting `t.Exception`) surfaces
`AggregateException` — there's no unwrapped form available without `await`.
