# Async code as a test-authoring concern

This file is about *writing tests for async code correctly* — xUnit-style async test methods,
avoiding `async void` in tests, testing cancellation and async streams, and asserting on faulted
tasks — not about testing this skill's own syntax examples. See
[csharp5-async-await.md](../references/csharp5-async-await.md) and
[csharp8-async-streams.md](../references/csharp8-async-streams.md) for the underlying syntax rules
these patterns build on.

## Basic: an async test method returning `Task`

```csharp
[Fact]
public async Task GetOrderAsync_Test_ReturnsOrder()
{
    Order order = await _repository.GetOrderAsync(orderId: 42);

    Assert.Equal(42, order.Id);
}
```

xUnit (and NUnit, MSTest) all support `async Task`-returning test methods directly — the test
runner awaits the returned `Task` itself. Never declare a test method `async void`: an exception
thrown from an `async void` method cannot be observed by the test runner's `await`, so a failing
assertion inside one either crashes the test process or, worse, is silently lost and the test
reports as passed. See [csharp5-async-await.md](../references/csharp5-async-await.md)'s own
`async void` restriction — the exact same rule, just with a testing-specific consequence.

## Basic: asserting an async method throws

```csharp
[Fact]
public async Task GetOrderAsync_Test_ThrowsWhenNotFound()
{
    await Assert.ThrowsAsync<OrderNotFoundException>(
        () => _repository.GetOrderAsync(orderId: 999));
}
```

`Assert.ThrowsAsync<T>` awaits the delegate internally and asserts on the unwrapped exception —
prefer it over wrapping a manual `try`/`await`/`catch` in the test body, and never assert against
`AggregateException` here; see
[exception-handling-in-async-code.md](exception-handling-in-async-code.md) for why `await`-based
code never surfaces that wrapper.

## Advanced: testing a cancellable method actually respects cancellation

```csharp
[Fact]
public async Task GetOrdersAsync_Test_RespectsCancellation()
{
    using CancellationTokenSource cts = new();
    cts.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(
        () => _repository.GetOrdersAsync(cts.Token));
}
```

A pre-cancelled token (`cts.Cancel()` before the call) is the standard way to verify a method
actually checks/forwards its `CancellationToken` rather than ignoring it — see
[cancellation-with-cancellationtoken.md](cancellation-with-cancellationtoken.md).

## Advanced: collecting an `IAsyncEnumerable<T>` for assertion

```csharp
[Fact]
public async Task GetOrdersAsync_Test_StreamsExpectedOrders()
{
    List<Order> orders = await _service.GetOrdersAsync(customerId: 1).ToListAsync();

    Assert.Equal(3, orders.Count);
    Assert.All(orders, o => Assert.Equal(1, o.CustomerId));
}
```

`System.Linq.Async`'s `ToListAsync()` (or a hand-rolled `await foreach` loop building a `List<T>`)
materializes an `IAsyncEnumerable<T>` for ordinary synchronous assertions — needs C# 8.0 for
`IAsyncEnumerable<T>` itself; see
[csharp8-async-streams.md](../references/csharp8-async-streams.md).

## Advanced: a generic async assertion helper (test-authoring tool, not a skill self-test)

```csharp
public static class AsyncAssertions
{
    public static async Task ShouldEventuallyAsync<T>(
        Func<Task<T>> poll, Func<T, bool> predicate, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        while (!cts.IsCancellationRequested)
        {
            if (predicate(await poll()))
            {
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(50), cts.Token);
        }
        throw new TimeoutException($"Condition not met within {timeout}.");
    }
}
```

```csharp
[Fact]
public async Task ProcessOrderAsync_Test_EventuallyMarksOrderComplete()
{
    await _service.ProcessOrderAsync(orderId: 42);

    await AsyncAssertions.ShouldEventuallyAsync(
        () => _repository.GetOrderAsync(42),
        order => order.Status == OrderStatus.Complete,
        TimeSpan.FromSeconds(5));
}
```

A generic (`ShouldEventuallyAsync<T>`) polling assertion is a common need when testing eventually-
consistent async workflows (background processing, message handlers) — the generic type parameter
here is the polled value's type, inferred from `poll`'s return type, exactly as in any other
generic method call.

## Fallback

Every example above needs only C# 5.0 `async`/`await`; the `IAsyncEnumerable<T>` example
additionally needs C# 8.0. Below C# 8.0, replace `GetOrdersAsync(...).ToListAsync()` with a
`Task<List<Order>>`-returning method under test instead, per
[csharp8-async-streams.md#fallback](../references/csharp8-async-streams.md#fallback); the
cancellation and exception-assertion patterns are unaffected by that tier gap.
