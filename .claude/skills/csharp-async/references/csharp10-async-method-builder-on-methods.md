# `[AsyncMethodBuilder]` on individual methods (C# 10 / .NET 6)

C# 10 shipped November 2021 alongside .NET 6. Since C# 7.0 (see
[csharp7-task-like-types.md](csharp7-task-like-types.md)), a custom task-like type could declare
its builder once via `[AsyncMethodBuilder(typeof(TBuilder))]` on the *type* — every `async` method
returning that type used the same builder. C# 10 allows the same attribute on an individual
*method*, overriding the builder for just that call site regardless of what its return type's
default builder is. This exists primarily to let hot-path methods opt into a pooling builder
(e.g. `PoolingAsyncValueTaskMethodBuilder<T>`) without changing every other method that returns the
same type.

## Syntax

```csharp
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
public async ValueTask<int> GetCachedValueAsync(int key)
{
    if (_cache.TryGetValue(key, out int value))
    {
        return value;
    }
    return await LoadValueAsync(key);
}
```

## Basic use case: opting a single hot-path method into a pooling builder

```csharp
public sealed class RequestHandler
{
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public async ValueTask<Response> HandleAsync(Request request)
    {
        // Called on every request; pooling the state machine avoids a per-call allocation
        // when the operation doesn't complete synchronously.
        Response response = await _pipeline.ExecuteAsync(request);
        return response;
    }
}
```

## Advanced use case: leaving the type's default builder alone for everything else

```csharp
// Regular ValueTask<T> builder — allocates a state machine box per call when it suspends,
// same as before C# 10.
public async ValueTask<Order> GetOrderAsync(int orderId) =>
    await _repository.GetOrderAsync(orderId);

// Only this method opts into pooling, because profiling showed it's the hot path.
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
public async ValueTask<int> GetOrderCountAsync(int customerId) =>
    await _repository.GetOrderCountAsync(customerId);
```

Method-level override means the decision to pool is a per-method performance tuning knob, not an
all-or-nothing choice for every `ValueTask<T>`-returning method in the type — appropriate for the
small number of methods actually shown by profiling to benefit, not applied speculatively.

## Requirements and restrictions

- The overriding builder must still satisfy the task-like type shape from C# 7.0 (a `Create()`
  method, `Task`/`ValueTask`-shaped result-producing members, etc.) — this attribute changes *which*
  builder compiles the method's state machine, not the rules a builder must follow.
- Pooling builders (`PoolingAsyncValueTaskMethodBuilder<T>`) reuse the underlying state object once
  the `ValueTask<T>` has been awaited — this sharpens the existing "await a `ValueTask<T>` at most
  once" rule from C# 7.0 into "and don't retain any reference derived from it afterward either."
  Reserve this for verified hot paths; the default builder is correct and safer for everything else.

## Fallback

Below C# 10, apply `[AsyncMethodBuilder(typeof(TBuilder))]` at the *type* level instead — meaning
every `async` method returning that particular task-like type shares one builder choice, with no
per-method override available. For built-in `Task`/`Task<T>`/`ValueTask`/`ValueTask<T>`, this
restriction rarely matters in practice, since pooling builders are an opt-in performance technique
most code doesn't need — see [csharp7-task-like-types.md](csharp7-task-like-types.md) for the
baseline task-like type mechanics this attribute builds on.
