---
name: dotnet-nsubstitute
description: Guidance on NSubstitute, a third-party mocking/test-double library for C#/.NET (current stable release 6.2.0). Covers Substitute.For<T>() creation, configuring return values with Returns/ReturnsForAnyArgs (including async Task<T>/ValueTask<T> members), argument matchers (Arg.Any<T>, Arg.Is, Arg.Do), verifying calls with Received()/DidNotReceive()/ReceivedWithAnyArgs(), partial substitutes via Substitute.ForPartsOf<T>(), and common pitfalls (argument matcher misuse outside a configuration/verification call, substituting non-virtual members, over-verifying). Use when writing, reviewing, or debugging test code that creates, configures, or verifies calls against an NSubstitute substitute.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert H. Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# NSubstitute

Guidance on NSubstitute, a third-party mocking/test-double library for .NET. Current stable release
as of this writing: **6.2.0**.
Organized by concern/topic, not by NSubstitute version — its core `Substitute.For<T>()`/`Returns`/
`Received()` API has been stable across its 4.x–6.x lines, so each reference file notes a
version-introduced fact inline rather than splitting files by version tier.

## Pick your reference file by situation

| You're doing this... | Reach for | Reference file |
| --- | --- | --- |
| Creating a test double for an interface, class, or delegate | `Substitute.For<T>()` | [references/core-concepts.md](references/core-concepts.md) |
| Making a substitute member return a specific value | `Returns`, `ReturnsForAnyArgs`, multi-call sequences, throwing | [references/configuring-return-values.md](references/configuring-return-values.md) |
| Matching which call a configuration or verification applies to | `Arg.Any<T>`, `Arg.Is<T>`, `Arg.Do<T>`, compound matchers | [references/argument-matchers.md](references/argument-matchers.md) |
| Asserting a member was (or wasn't) called | `Received()`, `Received(n)`, `DidNotReceive()`, `ReceivedWithAnyArgs()`, `ClearReceivedCalls()` | [references/verifying-calls.md](references/verifying-calls.md) |
| Configuring or verifying an `async`/`Task`-returning member | `Returns` with `Task<T>`/`ValueTask<T>`, `Received()` on async calls | [references/async-support.md](references/async-support.md) |
| Substituting most of a class but keeping some real logic | `Substitute.ForPartsOf<T>()`, `.When(...).CallBase()` | [references/partial-substitutes.md](references/partial-substitutes.md) |
| A substitute isn't matching, isn't returning what you configured, or a verification always fails | Argument matcher scope, non-virtual members, over-specified verifications | [references/common-pitfalls.md](references/common-pitfalls.md) |
| Verifying a shared substitute-factory helper or a custom argument matcher actually works | Testing test-double helpers, boundary-case matcher tests | [references/testing-your-test-doubles.md](references/testing-your-test-doubles.md) |

## Quick start

```csharp
public interface IOrderRepository
{
    Order? FindById(int id);
    void Save(Order order);
}

[Fact]
public void Checkout_SavesOrderWithUpdatedStatus()
{
    var repository = Substitute.For<IOrderRepository>();
    repository.FindById(42).Returns(new Order { Id = 42, Status = OrderStatus.Pending });

    var service = new CheckoutService(repository);
    service.Complete(orderId: 42);

    repository.Received(1).Save(Arg.Is<Order>(o => o.Status == OrderStatus.Completed));
}
```

`Substitute.For<T>()` builds a dynamic implementation of `IOrderRepository` with every member
returning a default value until you configure it with `Returns`; `Received()` then asserts what the
code under test actually called on it.

## Out of scope

- Other mocking libraries' APIs or migration paths from them.
- General unit-testing framework mechanics (test discovery, lifecycle, data-driven tests,
  assertion syntax) — orthogonal to what a mocking library itself does.
- Integration testing against real infrastructure (databases, HTTP servers) — NSubstitute's job is
  replacing a dependency with a configurable double, not managing real external state.
