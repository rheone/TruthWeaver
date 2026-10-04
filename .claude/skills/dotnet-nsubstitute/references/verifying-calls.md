# Verifying Calls

## `Received()` and `Received(n)`

`Received()` asserts a member was called at least once with the given arguments (or matchers);
`Received(n)` asserts it was called exactly `n` times:

```csharp
repository.Received().Save(Arg.Any<Order>());       // at least once
repository.Received(1).Save(Arg.Any<Order>());      // exactly once
repository.Received(0).Delete(Arg.Any<int>());      // exactly zero — equivalent to DidNotReceive()
```

A `Received()` call that finds no matching call throws, reporting the calls that *were* made to
that member so you can see why the match failed — a mismatched argument value or matcher is the
most common cause, not the member never being called at all.

## `DidNotReceive()`

Asserts a member was never called with the given arguments (or matchers):

```csharp
repository.DidNotReceive().Delete(Arg.Any<int>());
```

Prefer the specific arguments or matcher your scenario actually cares about over `Arg.Any<T>()`
here too — `DidNotReceive().Delete(Arg.Any<int>())` only proves `Delete` was never called with *any*
int; it says nothing about whether it was called with a `null` or a different overload.

## `ReceivedWithAnyArgs()` / `DidNotReceiveWithAnyArgs()`

Verifies the member was (or wasn't) called at all, ignoring what it was called with — the
verification equivalent of `ReturnsForAnyArgs`:

```csharp
repository.ReceivedWithAnyArgs().Save(default!);
```

Reach for this only when the arguments genuinely don't matter to what the test is proving;
otherwise the specific-argument form documents the scenario better and catches more regressions.

## Verifying order across multiple calls

`Received()` on its own does not assert ordering between different members or different calls.
When call order matters, use `Received.InOrder(() => { ... })`, which asserts every call inside the
block happened in that relative sequence:

```csharp
Received.InOrder(() =>
{
    repository.FindById(42);
    repository.Save(Arg.Any<Order>());
});
```

## `ClearReceivedCalls()`

Resets a substitute's recorded call history (but not its configured return values) — useful inside
a test that performs an action, asserts on the calls made so far, then performs a second action and
wants to verify the *second* action's calls in isolation without the first action's calls still
counting:

```csharp
service.Checkout(cart);
repository.Received(1).Save(Arg.Any<Order>());

repository.ClearReceivedCalls();

service.Refund(orderId: 42);
repository.Received(1).Save(Arg.Any<Order>());  // only counts calls made after the clear
```

## Verifying no unexpected calls at all

`repository.ReceivedCalls()` returns every recorded call as an `IEnumerable<ICall>`, which you can
inspect directly when a scenario needs to assert on the *total* set of interactions rather than one
member at a time — reach for this sparingly, since asserting against the full call list tends to
make a test brittle against unrelated implementation changes.
