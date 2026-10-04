# Custom Exception Design

When to introduce a custom exception type, the standard constructor shape, hierarchy design, and
the current (post-.NET 8) guidance on serialization — a place earlier .NET versions required
boilerplate this domain no longer needs. Builds on
[../references/csharp1-try-catch-finally.md](../references/csharp1-try-catch-finally.md); nothing
here depends on any later tier.

## Basic: when a custom exception type earns its keep

```csharp
// Worth a custom type: callers can reasonably want to catch THIS failure specifically,
// distinct from other InvalidOperationExceptions, and it carries structured data callers need.
public class InsufficientInventoryException : Exception
{
    public string Sku { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientInventoryException(string sku, int requested, int available)
        : base($"Requested {requested} of '{sku}' but only {available} available.")
    {
        Sku = sku;
        Requested = requested;
        Available = available;
    }

    public InsufficientInventoryException()
    {
    }

    public InsufficientInventoryException(string message)
        : base(message)
    {
    }

    public InsufficientInventoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

A custom exception type is worth creating when at least one of these is true: callers plausibly
need to catch it separately from sibling failures, it carries structured data (`Sku`, `Requested`,
`Available` above) a caller might act on programmatically rather than just log, or it marks a
domain-specific failure mode that a generic `InvalidOperationException`/`ArgumentException` would
blur together with unrelated failures. If none of those apply, throwing one of the BCL's own
exception types is preferable to growing an exception-type hierarchy nobody catches selectively.

The four constructors shown (default, message-only, message-plus-inner, and the domain-specific
one) follow the standard shape: the first three exist so the type behaves like any other
`Exception`-derived type in generic infrastructure (deserialization helpers, some logging
frameworks, and test tooling all assume a message-only or parameterless constructor exists), even
though most call sites will only ever use the domain-specific one.

## Advanced: exception hierarchies and when to introduce a common base

```csharp
public abstract class OrderProcessingException : Exception
{
    public string OrderId { get; }

    protected OrderProcessingException(string orderId, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        OrderId = orderId;
    }
}

public sealed class InsufficientInventoryException : OrderProcessingException
{
    public string Sku { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientInventoryException(string orderId, string sku, int requested, int available)
        : base(orderId, $"Order {orderId}: requested {requested} of '{sku}' but only {available} available.")
    {
        Sku = sku;
        Requested = requested;
        Available = available;
    }
}

public sealed class PaymentDeclinedException : OrderProcessingException
{
    public string DeclineReason { get; }

    public PaymentDeclinedException(string orderId, string declineReason)
        : base(orderId, $"Order {orderId}: payment declined ({declineReason}).")
    {
        DeclineReason = declineReason;
    }
}
```

```csharp
try
{
    ProcessOrder(order);
}
catch (OrderProcessingException ex)
{
    // one handler for "something about processing this order failed," regardless of which
    // specific failure -- while callers who need to distinguish InsufficientInventoryException
    // from PaymentDeclinedException can still add a more specific catch above this one.
    Logger.LogWarning("Order {OrderId} failed: {Message}", ex.OrderId, ex.Message);
}
```

Introduce a shared abstract base only when there's a real "catch either of these, together" use
case — an abstract base with a single concrete subclass, or a base nobody ever catches by its own
type, is pure ceremony. Keep the base `abstract` (nothing should throw the base type itself) and
leaf types `sealed` unless a further subclass is genuinely expected.

## Serialization: current guidance supersedes the old three-constructor-plus-`ISerializable` advice

Older guidance (and older generated templates) added a fourth, `protected` constructor taking
`SerializationInfo`/`StreamingContext` plus a `GetObjectData` override, to support binary
serialization across AppDomain or remoting boundaries. As of .NET 8, the legacy binary
serialization infrastructure those members plug into is obsolete (diagnostic `SYSLIB0051`), and
.NET 9 removed `BinaryFormatter` outright — remoting itself was already dropped in .NET Core 1.0,
which was the binary serialization constructor's original reason for existing on `Exception` in
the first place.

For any exception type written against a current .NET target, skip the serialization constructor,
`GetObjectData` override, and `[Serializable]` attribute entirely — the four-constructor "basic"
example above and the hierarchy example are the complete current shape. If cross-process transport
of exception data is genuinely needed (e.g. an error contract returned from a remote worker), model
that as an ordinary data type serialized with `System.Text.Json` or the transport's own contract
type, not as binary serialization of the `Exception` instance itself.

## Fallback

Everything in this file has worked since [C# 1.0](../references/csharp1-try-catch-finally.md) at
the language level — there's no version gate on defining a custom exception type or an exception
hierarchy. The one version-sensitive fact is the serialization guidance above: a codebase that must
still target .NET Framework or an older .NET Core version without `SYSLIB0051` may still need the
legacy serialization constructor and `[Serializable]` attribute if it genuinely uses binary
serialization or remoting; the modern advice to omit them applies once the minimum target no longer
requires that infrastructure.
