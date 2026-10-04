# Testing Your Test Doubles

NSubstitute configures test doubles *for* other tests, but a shared substitute-creation helper or
a custom argument matcher is itself code that can have bugs — a helper that silently misconfigures
a return value, or a matcher that always matches (or never matches), passes every test that uses it
right up until it hides a real regression. Verify these directly rather than trusting them by
inspection.

## Testing a shared substitute-factory helper

A helper that centralizes common substitute setup (e.g. `TestDoubles.OrderRepositoryReturning(order)`)
should be tested the same way as any other production code: assert it configures exactly the
behavior it claims to.

```csharp
public static class TestDoubles
{
    public static IOrderRepository OrderRepositoryReturning(Order order)
    {
        var repository = Substitute.For<IOrderRepository>();
        repository.FindById(order.Id).Returns(order);
        return repository;
    }
}

[Fact]
public void OrderRepositoryReturning_ConfiguresFindByIdForTheGivenOrder()
{
    var order = new Order { Id = 42 };

    var repository = TestDoubles.OrderRepositoryReturning(order);

    repository.FindById(42).Should().BeSameAs(order);
    repository.FindById(99).Should().BeNull(); // unconfigured id returns the default, not the order
}
```

The second assertion matters as much as the first — a helper that accidentally uses `ReturnsForAnyArgs`
instead of `Returns` would make every call return the same order regardless of id, silently
weakening every test that uses the helper.

## Testing a custom argument matcher

A custom matcher built on `Arg.Is<T>(predicate)` or a compound matcher combining several
conditions should be tested against both a value it's meant to match and one it's meant to reject:

```csharp
[Fact]
public void IsValidOrderMatcher_MatchesOnlyPositiveTotals()
{
    Func<Order, bool> matcher = o => o.Total > 0;

    matcher(new Order { Total = 10 }).Should().BeTrue();
    matcher(new Order { Total = -5 }).Should().BeFalse();
    matcher(new Order { Total = 0 }).Should().BeFalse();
}
```

Testing the boundary (`Total = 0` here) is what catches an off-by-one in the matcher's predicate —
a matcher tested only against an obviously-valid and obviously-invalid case can still hide a wrong
boundary condition.

## Common pitfall: a matcher or helper that always passes

The most dangerous failure mode for test infrastructure is one that looks like it's verifying
something but actually can't fail — a matcher predicate accidentally returning `true`
unconditionally, or a helper method that configures a substitute member NSubstitute silently
ignores (e.g. configuring a non-virtual member, which NSubstitute cannot intercept and which
produces no error, just a substitute that behaves as if unconfigured). Write the negative-case
assertion for any shared test-double helper specifically to catch this: confirm the helper's
configuration actually has an observable effect, not just that calling it doesn't throw.
