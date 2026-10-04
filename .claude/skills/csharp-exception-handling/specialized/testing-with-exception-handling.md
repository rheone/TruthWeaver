# Exception Handling as a Concern When Writing Tests

This file is about how exception behavior shows up *while authoring tests* — asserting that a
specific exception type (and specific properties/message on it) was thrown, asserting the
opposite (that nothing was thrown when a given input is valid), and building reusable custom
assertion helpers across test frameworks. It is not a tutorial on testing this skill's own
`try`/`catch`/`when`/`throw` syntax examples. Builds on
[../references/csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md).

## Basic: asserting an exception is thrown, across frameworks

```csharp
// xUnit
[Fact]
public void Withdraw_Test_Throws_WhenInsufficientFunds()
{
    var account = new Account(balance: 50m);

    InsufficientFundsException ex = Assert.Throws<InsufficientFundsException>(
        () => account.Withdraw(100m));

    Assert.Equal(50m, ex.AvailableBalance);
}
```

```csharp
// NUnit
[Test]
public void Withdraw_Test_Throws_WhenInsufficientFunds()
{
    var account = new Account(balance: 50m);

    var ex = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100m));

    Assert.That(ex.AvailableBalance, Is.EqualTo(50m));
}
```

```csharp
// MSTest
[TestMethod]
public void Withdraw_Test_Throws_WhenInsufficientFunds()
{
    var account = new Account(balance: 50m);

    var ex = Assert.ThrowsExactly<InsufficientFundsException>(() => account.Withdraw(100m));

    Assert.AreEqual(50m, ex.AvailableBalance);
}
```

All three return the caught exception instance from the assertion call, which is what makes
asserting on its *properties* (not just its type) possible in the same test — asserting only the
type and ignoring the returned instance verifies far less than most test authors assume, since a
type-only assertion would pass even if the exception carried the wrong data. Note MSTest's naming:
`Assert.ThrowsExactly<T>` requires the exact type (a subtype does not satisfy it), while xUnit's and
NUnit's `Assert.Throws<T>` accept the declared type or anything derived from it — check which
behavior a given framework version provides before relying on exact-vs-derived matching.

## Basic: asserting on the message or other properties, not just the type

```csharp
[Fact]
public void Withdraw_Test_MessageIncludesShortfall()
{
    var account = new Account(balance: 50m);

    InsufficientFundsException ex = Assert.Throws<InsufficientFundsException>(
        () => account.Withdraw(100m));

    Assert.Equal(50m, ex.AvailableBalance);
    Assert.Equal(100m, ex.Requested);
    Assert.Contains("50", ex.Message); // brittle: prefer asserting structured properties over message text
}
```

Prefer asserting structured properties (`AvailableBalance`, `Requested`) the exception type exposes
over asserting substrings of `Message` — message text is the part of an exception most likely to
be reworded without anyone treating that as a breaking change, while a public property is part of
the type's actual contract. Reach for a message assertion only when the exception genuinely has no
structured data to check (e.g., wrapping a third-party library's exception whose message is the
only available detail).

## Basic: asserting an exception is *not* thrown

```csharp
[Fact]
public void Withdraw_Test_DoesNotThrow_WhenFundsSufficient()
{
    var account = new Account(balance: 100m);

    Exception? ex = Record.Exception(() => account.Withdraw(50m)); // xUnit: captures any exception, or null

    Assert.Null(ex);
}
```

```csharp
[Fact]
public void Withdraw_Test_DoesNotThrow_WhenFundsSufficient_Plain()
{
    var account = new Account(balance: 100m);

    account.Withdraw(50m); // if this throws, the test fails with the exception itself as the failure --
                            // no explicit assertion needed for the "did not throw" case
}
```

For the "must not throw" case, letting an unexpected exception simply propagate out of the test
method is usually sufficient and produces a clearer failure (the actual exception and its stack
trace) than wrapping the call in a `try`/`catch` that converts it into `Assert.Fail`. Reach for
`Record.Exception` (xUnit) or an equivalent capture-and-inspect helper specifically when the test
also needs to assert something about the *absence* case beyond "no exception happened" — e.g.,
asserting no exception was thrown AND that a specific side effect did or didn't occur, where the
capture-then-assert form reads more clearly than a bare call followed by unrelated assertions.

## Advanced: an async variant and a custom cross-framework assertion helper

```csharp
[Fact]
public async Task WithdrawAsync_Test_Throws_WhenInsufficientFunds()
{
    var account = new Account(balance: 50m);

    InsufficientFundsException ex = await Assert.ThrowsAsync<InsufficientFundsException>(
        () => account.WithdrawAsync(100m));

    Assert.Equal(50m, ex.AvailableBalance);
}
```

```csharp
public static class ExceptionAssert
{
    public static TException ThrowsWithProperty<TException, TProperty>(
        Action action,
        Func<TException, TProperty> propertySelector,
        TProperty expected)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex) when (Equals(propertySelector(ex), expected))
        {
            return ex; // matched: return it so the caller can assert further if needed
        }
        catch (TException ex)
        {
            throw new InvalidOperationException(
                $"{typeof(TException).Name} was thrown, but with an unexpected property value.", ex);
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but nothing was thrown.");
    }
}
```

```csharp
ExceptionAssert.ThrowsWithProperty<InsufficientFundsException, decimal>(
    () => account.Withdraw(100m),
    ex => ex.AvailableBalance,
    expected: 50m);
```

A custom cross-framework helper like this is worth writing once a codebase's tests repeatedly
combine "assert this type was thrown" with "assert this specific property matches" across many
test files — it standardizes the pattern instead of each test hand-rolling its own `try`/`catch`
around the assertion, and the `when` filter (see
[exception-filters-in-depth.md](exception-filters-in-depth.md)) lets it distinguish "wrong
property value" from "right type, but with unexpected details" as two different failure messages.

## Fallback

`Assert.Throws<T>`/`Assert.ThrowsAsync<T>` are ordinary generic library methods, not compiler
features — they've worked since whichever xUnit/NUnit/MSTest version first shipped them, well
within [C# 1.0](../references/csharp1-try-catch-finally.md)'s generic-method-calling rules once
generics themselves exist (C# 2.0 onward for the caller side; the assertion libraries themselves
all target C# 2.0+ runtimes). On a test framework or C# target with no typed `Throws<T>` helper at
all, use a plain `try`/`catch` around the call, fail explicitly if no exception was thrown, and
assert on the caught exception's type and properties manually — functionally equivalent, just more
verbose per test.
