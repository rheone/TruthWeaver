# Object Initializers, Extension Methods, and Lambda Configuration (C# 3.0, .NET Framework 3.5, GA November 19, 2007)

C# 3.0 changes the builder story in two directions at once. Object and collection initializer
syntax gives simple, all-public-settable-property types a *builder-free* way to look almost like a
builder call at the use site — which is exactly the case where a real builder pattern is now
overkill. At the same time, extension methods and lambda expressions give genuine builders new
tools: a fluent configuration API can be extended by third-party code without modifying the
builder's own type, and a builder can accept a configuration callback instead of a flat list of
calls.

## Syntax

```csharp
// Object initializer: an alternative to a builder for a type with public settable properties.
var invoice = new Invoice
{
    Title = "March",
    Amount = 199.99m
};

// Extension method adding a fluent call to a builder this file doesn't own.
public static class InvoiceBuilderExtensions
{
    public static InvoiceBuilder WithRoundedAmount(this InvoiceBuilder builder, decimal amount)
    {
        return builder.WithAmount(Math.Round(amount, 2));
    }
}
```

## Basic use case: object initializers as the simple-case alternative to a builder

```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
}

var shippingAddress = new Address
{
    Street = "221B Baker St",
    City = "London",
    PostalCode = "NW1 6XE"
};
```

When every field is optional, mutable, and independent — no cross-field validation, no required
subset, no multi-step assembly — an object initializer *is* the builder: it reads just as
declaratively as a chain of `With*` calls, without a builder class to maintain. Reach for an actual
builder pattern when at least one of those conditions stops holding (see
[specialized/builder-vs-modern-alternatives.md](../specialized/builder-vs-modern-alternatives.md)
for the fuller decision list, which also covers the C# 9.0+ and C# 11.0+ alternatives).

## Advanced use case: a generic builder accepting a configuration lambda

```csharp
public class Builder<TSelf, TProduct> where TSelf : Builder<TSelf, TProduct>
{
    public abstract TProduct Build();
}

public sealed class RequestBuilder : Builder<RequestBuilder, HttpRequestMessage>
{
    private readonly List<Action<HttpRequestMessage>> _configurators = new List<Action<HttpRequestMessage>>();

    public RequestBuilder Configure(Action<HttpRequestMessage> configure)
    {
        _configurators.Add(configure);
        return this;
    }

    public override HttpRequestMessage Build()
    {
        var request = new HttpRequestMessage();
        foreach (Action<HttpRequestMessage> configure in _configurators)
        {
            configure(request);
        }
        return request;
    }
}
```

```csharp
HttpRequestMessage request = new RequestBuilder()
    .Configure(r => r.Method = HttpMethod.Post)
    .Configure(r => r.Headers.Add("X-Trace-Id", traceId))
    .Build();
```

Accepting `Action<TProduct>` turns the builder into an open-ended extension point: a caller can
inject arbitrary configuration logic without the builder exposing a dedicated method for every
possible tweak. `List<Action<HttpRequestMessage>>` is itself a generic collection of a generic
delegate type — both only became first-class C# constructs together, generics in C# 2.0 and the
lambda syntax populating them in C# 3.0.

## Requirements and restrictions

- Extension methods only add methods, never fields — an extension method on a builder can't add
  new state to accumulate, only compose existing public members (as `WithRoundedAmount` does by
  calling the builder's own `WithAmount`). A builder that wants to be safely extended this way
  needs its accumulation methods to already be public.
- Object initializers require a public parameterless constructor (or one whose arguments are
  supplied positionally alongside the initializer) and public settable properties or fields —
  nothing enforces that a required subset actually gets set, which is the gap
  [csharp11-required-members.md](csharp11-required-members.md) later closes for this exact
  alternative.

## Fallback

On a target without generics or extension methods (.NET Framework 1.0/1.1, C# 1.0), skip the
generic base and configuration-lambda shape and write a concrete, non-generic builder per product
type with `Set*` methods instead — see
[pre-csharp2-classic-builder.md](pre-csharp2-classic-builder.md). Without generics specifically
(available from C# 2.0), skip the object-initializer alternative's competitor comparison but keep
the generic self-typed base from [csharp2-generic-builders.md](csharp2-generic-builders.md);
replace a configuration lambda with a dedicated `Set*`-style method per configurable aspect, since
there is no `Action<T>` delegate type convenience to lean on before C# 2.0 either (delegates exist
from C# 1.0, but the generic `Action<T>`/`Func<T>` families do not).
