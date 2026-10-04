# `AggregateException` and Flattening

`AggregateException` is a BCL type (`System`, shipped in .NET Framework 4.0 with the Task Parallel
Library) — not a C# language feature, so nothing about it is gated by C# language version; it's
usable from any C# version once the target framework is .NET Framework 4.0 or later. It represents
one or more failures collected together, most commonly surfaced by blocking `Task` observation
(`.Wait()`, `.Result`) or by parallel APIs (`Parallel.For`, `Parallel.ForEach`) where multiple
independent operations can each fail. Builds on
[../references/csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md) for the
`try`/`catch` shape itself.

## Basic: catching and enumerating multiple failures

```csharp
try
{
    Parallel.ForEach(orderIds, id =>
    {
        ProcessOrder(id); // any iteration's exception is collected, not thrown immediately
    });
}
catch (AggregateException ex)
{
    foreach (Exception inner in ex.InnerExceptions)
    {
        Logger.LogError(inner, "An order failed to process.");
    }
}
```

`Parallel.ForEach` (and `Parallel.For`) run every iteration to completion regardless of earlier
failures, then wrap every iteration's exception into a single `AggregateException`'s
`InnerExceptions` collection — a single `catch (SomeSpecificException)` would never match here,
because the exception the `try` actually observes is always `AggregateException` itself, not
whichever type(s) are nested inside it.

## Advanced: flattening nested `AggregateException`s

```csharp
try
{
    Parallel.Invoke(
        () => Parallel.ForEach(batchA, ProcessOrder), // can itself throw AggregateException
        () => Parallel.ForEach(batchB, ProcessOrder));
}
catch (AggregateException ex)
{
    // Flatten() collapses any AggregateException nested inside InnerExceptions (arbitrarily
    // deep) into a single, flat AggregateException whose InnerExceptions has no
    // AggregateException instances left in it -- just the original leaf failures.
    AggregateException flat = ex.Flatten();
    foreach (Exception inner in flat.InnerExceptions)
    {
        Logger.LogError(inner, "An order failed to process.");
    }
}
```

Nesting happens naturally whenever one parallel/aggregating operation contains another — without
`Flatten()`, `ex.InnerExceptions` could itself contain `AggregateException` instances, so a caller
that doesn't flatten first has to recurse manually to reach every leaf failure. `Flatten()` returns
a new `AggregateException` rather than mutating the original.

## Advanced: filtering with `Handle`

```csharp
try
{
    Parallel.ForEach(orderIds, ProcessOrder);
}
catch (AggregateException ex)
{
    // Handle invokes the predicate for each inner exception; any inner exception for which
    // the predicate returns false is re-collected into a new AggregateException that Handle
    // re-throws -- so only genuinely unhandled failures propagate further.
    ex.Handle(inner =>
    {
        if (inner is OrderNotFoundException notFound)
        {
            Logger.LogWarning("Order {OrderId} no longer exists, skipping.", notFound.OrderId);
            return true; // handled -- don't rethrow this one
        }
        return false; // not handled -- include in the rethrown AggregateException
    });
}
```

`Handle` is the idiomatic alternative to a manual `foreach` over `InnerExceptions` when only some
of the inner failures are expected/recoverable — it rethrows automatically if any predicate call
returned `false`, preserving exactly the inner exceptions that weren't handled.

## Fallback

`AggregateException`, `Flatten()`, and `Handle()` all require the .NET Framework 4.0+ BCL — there's
no C# language version gate, only a target-framework one (this is the language-vs-BCL distinction:
`AggregateException` compiles and works identically on any C# language version once the target
framework is new enough). On a target predating .NET Framework 4.0, there's no aggregate-failure
type at all — parallel/multi-failure code on that old a target would need to collect exceptions
into an ordinary `List<Exception>` by hand and decide its own convention for surfacing them,
since `Parallel`, `Task`, and `AggregateException` itself didn't exist yet.
