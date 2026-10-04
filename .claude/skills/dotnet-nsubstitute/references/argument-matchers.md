# Argument Matchers

Argument matchers let a `Returns` configuration or a `Received()` verification apply to a class of
calls instead of one literal argument value. They live in the static `Arg` class and are only valid
**inside a member call that is itself inside a configuration or verification statement** — see the
warning at the end of this file.

## `Arg.Any<T>()`

Matches any value of type `T`, including `null` for reference types:

```csharp
repository.FindById(Arg.Any<int>()).Returns(new Order());

repository.Received().Save(Arg.Any<Order>());
```

Use it when the specific value genuinely doesn't matter to the scenario being tested — the call
happened with *some* argument of that type, and that's all this configuration or assertion needs to
establish.

## `Arg.Is<T>(...)`

Matches a value against either a literal or a predicate:

```csharp
// Literal — equivalent to just passing the value directly, but useful for clarity
// when mixed with other Arg.* calls in the same argument list.
repository.FindById(Arg.Is(42));

// Predicate — matches any Order whose Status is Completed.
repository.Received().Save(Arg.Is<Order>(o => o.Status == OrderStatus.Completed));
```

The predicate form is the one you reach for most: it lets a verification assert on a *shape*
(specific properties of a complex argument) without needing an `Equals` override on that type or a
full object comparison.

## `Arg.Do<T>()` — capturing an argument for later inspection

`Arg.Do<T>(callback)` matches any value of type `T` (like `Arg.Any<T>()`) and additionally invokes
the given callback with the actual argument every time the call is made — useful for capturing a
value passed to a `void` member so you can assert on it after the fact:

```csharp
Order? savedOrder = null;
repository.Save(Arg.Do<Order>(o => savedOrder = o));

service.Checkout(cart);

Assert.Equal(OrderStatus.Completed, savedOrder?.Status);
```

## Compound matchers in one call

When a member takes multiple parameters, mix matchers and literals freely, but **once you use one
`Arg.*` matcher in a call's argument list, every argument in that same call must be an `Arg.*`
matcher** — you cannot mix a literal and a matcher in the same call:

```csharp
// Invalid — mixes a literal (42) with a matcher (Arg.Any<Order>()) in one call.
repository.Update(42, Arg.Any<Order>());

// Valid — wrap the literal in Arg.Is.
repository.Update(Arg.Is(42), Arg.Any<Order>());
```

NSubstitute detects this specific mistake and throws at configuration time with a message pointing
at it, which is the one argument-matcher misuse that fails loudly rather than silently.

## The scope trap

`Arg.Any<T>()`, `Arg.Is<T>()`, and `Arg.Do<T>()` work by registering a matcher against the *next*
member call NSubstitute intercepts — they do not evaluate to a real `T` value on their own. Calling
one outside of a substitute member-call argument list (assigning it to a variable, passing it to
your own helper method, using it in an `if`) either returns a meaningless default or corrupts the
matcher queue for the next real call, and is one of the most common sources of a mysterious
NSubstitute failure. Always call `Arg.*` directly as an argument to the substitute member you're
configuring or verifying, in the same statement — never store the result or forward it through
another method call first.
