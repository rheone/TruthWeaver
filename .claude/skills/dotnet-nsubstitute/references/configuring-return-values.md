# Configuring Return Values

## `Returns` for a specific call

You configure a return value by calling the member with the exact arguments (or matchers) you want
to key off, then chaining `.Returns(...)`:

```csharp
var repository = Substitute.For<IOrderRepository>();
repository.FindById(42).Returns(new Order { Id = 42 });

repository.FindById(42);  // returns the configured Order
repository.FindById(99);  // returns null (the type's default) — not configured
```

Only calls whose arguments match the configuration return the configured value; every other call to
that member returns the member's default. This is why a test that calls a substitute with an
argument it never configured is usually a bug in the test, not in the substitute.

## Multiple return values across successive calls

`.Returns(...)` accepts multiple values, which NSubstitute returns in order across successive calls
with matching arguments, repeating the last one for any call beyond the count you supplied:

```csharp
queue.Dequeue().Returns(1, 2, 3);

queue.Dequeue(); // 1
queue.Dequeue(); // 2
queue.Dequeue(); // 3
queue.Dequeue(); // 3 (repeats the last configured value)
```

For a computed sequence, or a value that depends on call state, pass a `Func<CallInfo, T>` instead
of a literal:

```csharp
var counter = 0;
generator.Next().Returns(_ => counter++);
```

`CallInfo` also exposes the arguments the call was made with (`callInfo.Arg<T>()`,
`callInfo[index]`), which lets a configured return value depend on what was actually passed.

## `ReturnsForAnyArgs`

`Returns` matches the specific arguments the call was configured with. `ReturnsForAnyArgs` ignores
the arguments entirely and returns the configured value for **any** call to that member, regardless
of what's passed:

```csharp
repository.FindById(default).ReturnsForAnyArgs(new Order { Id = 1 });

repository.FindById(1);   // returns the configured Order
repository.FindById(999); // also returns the configured Order
```

Reach for `ReturnsForAnyArgs` when the test genuinely doesn't care which arguments are passed and
asserting on them would only add noise; prefer `Returns` with an explicit value or `Arg.Is` matcher
whenever the specific arguments matter to the scenario, since `ReturnsForAnyArgs` silently
succeeds even if the code under test passes something unexpected.

## Throwing from a substitute

`.Returns` and `.ReturnsForAnyArgs` both have `Throws`-prefixed counterparts (`.Throws(exception)`,
`.ThrowsForAnyArgs(exception)`) for configuring a member to raise an exception when called, which is
the way you exercise a caller's error-handling path against a dependency failure:

```csharp
repository.FindById(42).Throws(new TimeoutException("database unavailable"));
```

For a member returning `void`, use `.When(x => x.Save(Arg.Any<Order>())).Do(_ => throw new
TimeoutException())` instead — `void` members have no return value to hang `.Returns`/`.Throws` off
of; see [async-support.md](async-support.md) for the equivalent pattern on `Task`-returning members
that must fault rather than throw synchronously.
