# Async Support

## Configuring a `Task<T>`-returning member

Recent NSubstitute versions unwrap the awaitable automatically: you pass the plain value to
`.Returns(...)` and NSubstitute wraps it in a completed `Task<T>` (or `ValueTask<T>`) for you.

```csharp
public interface IOrderRepository
{
    Task<Order?> FindByIdAsync(int id, CancellationToken cancellationToken);
}

repository.FindByIdAsync(42, Arg.Any<CancellationToken>()).Returns(new Order { Id = 42 });

var order = await repository.FindByIdAsync(42, CancellationToken.None); // the configured Order
```

You can still be explicit and pass `Task.FromResult(...)` yourself — both forms produce the same
completed-task behavior; the plain-value form is the more common style since it reads closer to
what the test is actually asserting.

## Faulting an async member

To make an `await` against the substitute throw, configure a faulted task rather than calling
`.Throws(...)` (which is for synchronous exceptions):

```csharp
repository.FindByIdAsync(42, Arg.Any<CancellationToken>())
    .Returns(Task.FromException<Order?>(new TimeoutException("database unavailable")));
```

`.ThrowsAsync(exception)` is the equivalent shorthand where available, producing the same faulted
task without constructing it by hand.

## Verifying an async call

Verification works exactly like the synchronous case — you `await` the call under test first, then
verify against the substitute; `Received()` does not itself need to be awaited, since it inspects
the already-recorded call history rather than making a new async call:

```csharp
await service.RefreshOrderAsync(orderId: 42);

await repository.Received(1).FindByIdAsync(42, Arg.Any<CancellationToken>());
```

Awaiting the `Received()` expression above is not required by NSubstitute itself, but many codebases
await it anyway for consistency with the surrounding async call syntax; either form performs the
verification synchronously the moment it executes.

## The `Task`-returning `void`-equivalent case

For a member that returns a bare `Task` (no result value, the async equivalent of `void`), configure
completion or failure the same way as `Task<T>`, just without a result value to pass:

```csharp
repository.SaveAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
```

Because `Task.CompletedTask` is the type's own default-adjacent value, some codebases skip
configuring this explicitly — an unconfigured `Task`-returning member on a substitute already
returns a completed task by default, so you only need to configure it when the test specifically
needs a delay, fault, or cancellation to occur.
