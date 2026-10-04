# `await` in `catch` and `finally` blocks (C# 6.0 / .NET Framework 4.6)

C# 6.0 shipped July 20, 2015 with Visual Studio 2015 and .NET Framework 4.6. Among its many small
productivity features, one closes a real gap in C# 5.0's `async`/`await`: C# 5.0's compiler could
not generate a valid state machine for an `await` expression inside a `catch` or `finally` block,
so cleanup and error-logging code that needed to run an asynchronous operation had no way to do so
directly. C# 6.0 removes that restriction.

## Syntax

```csharp
public async Task SubmitOrderAsync(Order order)
{
    try
    {
        await _gateway.SubmitAsync(order);
    }
    catch (GatewayException ex)
    {
        await _logger.LogErrorAsync(ex);
        throw;
    }
    finally
    {
        await _connection.CloseAsync();
    }
}
```

## Basic use case

Logging a failure asynchronously without losing the original exception:

```csharp
public async Task<Order> LoadOrderAsync(int orderId)
{
    try
    {
        return await _orderRepository.GetOrderAsync(orderId);
    }
    catch (SqlException ex)
    {
        await _telemetry.TrackExceptionAsync(ex);
        throw;
    }
}
```

## Advanced use case: async cleanup that must run regardless of outcome

```csharp
public async Task<ReportResult> GenerateReportAsync(ReportRequest request)
{
    IReportSession session = await _reportEngine.OpenSessionAsync(request);
    try
    {
        return await session.RunAsync();
    }
    finally
    {
        // Cleanup itself is asynchronous (e.g. releasing a remote resource);
        // C# 5.0 could not express this directly inside `finally`.
        await session.CloseAsync();
    }
}
```

Before C# 6.0, code with this shape had to move the `await session.CloseAsync()` call outside the
`try`/`finally` entirely (losing the "runs even on exception" guarantee) or restructure into nested
`try`/catch blocks that captured the exception, ran the async cleanup outside `finally`, then
rethrew — noticeably more error-prone than the direct form above.

## Requirements and restrictions

None beyond ordinary `async`/`await` rules — the language restriction that's lifted here is purely
about where `await` may appear, not a new capability with its own constraints.

## Fallback

Below C# 6.0 / .NET Framework 4.6, restructure the `finally` cleanup to run outside the
`try`/`finally` block, tracking whether it must still execute:

```csharp
public async Task<ReportResult> GenerateReportAsync(ReportRequest request)
{
    IReportSession session = await _reportEngine.OpenSessionAsync(request);
    ExceptionDispatchInfo? pending = null;
    ReportResult? result = null;
    try
    {
        result = await session.RunAsync();
    }
    catch (Exception ex)
    {
        pending = ExceptionDispatchInfo.Capture(ex);
    }
    await session.CloseAsync();
    pending?.Throw();
    return result!;
}
```

Otherwise, everything in [csharp5-async-await.md](csharp5-async-await.md) still applies unchanged.
