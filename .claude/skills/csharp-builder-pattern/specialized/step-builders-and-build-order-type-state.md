# Step Builders and Build-Order Enforcement (Type-State Pattern)

A **step builder** (also called a type-state builder) uses a sequence of narrow generic interfaces
— one per required step — so that only the *next* legal method is visible at each point in a
chain, and `Build()` itself only appears once every required step has been completed. This turns a
"forgot to call `WithAmount`" bug from a run-time `Build()` failure (the plain fluent builder's best
option, shown in every `references/` tier) into a *compile error*: the method simply doesn't exist
on the type the previous step returned. This relies only on generics and interfaces (C# 2.0) and
applies at any tier from there forward.

## Basic: a three-step builder where each step returns the interface for the next step only

```csharp
public interface IRecipientStep
{
    ISubjectStep To(string recipient);
}

public interface ISubjectStep
{
    IBodyStep WithSubject(string subject);
}

public interface IBodyStep
{
    EmailBuilder WithBody(string body); // last step returns the concrete builder, which exposes Build()
}

public sealed class EmailBuilder : IRecipientStep, ISubjectStep, IBodyStep
{
    private string _recipient = "";
    private string _subject = "";
    private string _body = "";

    private EmailBuilder() { } // construction only through Start()

    public static IRecipientStep Start() => new EmailBuilder();

    public ISubjectStep To(string recipient) { _recipient = recipient; return this; }
    public IBodyStep WithSubject(string subject) { _subject = subject; return this; }
    public EmailBuilder WithBody(string body) { _body = body; return this; }

    public Email Build() => new(_recipient, _subject, _body);
}
```

```csharp
Email email = EmailBuilder.Start()
    .To("ops@example.com")   // only To(...) is visible here — the IRecipientStep interface exposes nothing else
    .WithSubject("Deploy")   // only WithSubject(...) is visible here
    .WithBody("Shipped v2")  // only WithBody(...), which returns the concrete EmailBuilder with Build()
    .Build();

// EmailBuilder.Start().WithSubject("x"); // compile error: IRecipientStep has no WithSubject method
```

The private constructor plus a static `Start()` factory returning the *first* step's interface
(not the concrete type) closes off every entry point except the one that begins the required
sequence — a caller can never get a reference typed as `EmailBuilder` itself until the last step
has already returned it.

## Advanced: a generic step-builder interface reused across multiple product hierarchies

```csharp
public interface IStep<out TNext>
{
    TNext Advance();
}

public interface IHasDestination<TProduct>
{
    IHasCarrier<TProduct> To(string destination);
}

public interface IHasCarrier<TProduct>
{
    IBuildable<TProduct> Via(string carrier);
}

public interface IBuildable<TProduct>
{
    TProduct Build();
}

public sealed class ShipmentBuilder : IHasDestination<Shipment>, IHasCarrier<Shipment>, IBuildable<Shipment>
{
    private string _destination = "";
    private string _carrier = "";

    private ShipmentBuilder() { }

    public static IHasDestination<Shipment> Start() => new ShipmentBuilder();

    public IHasCarrier<Shipment> To(string destination) { _destination = destination; return this; }
    public IBuildable<Shipment> Via(string carrier) { _carrier = carrier; return this; }
    public Shipment Build() => new(_destination, _carrier);
}
```

```csharp
Shipment shipment = ShipmentBuilder.Start()
    .To("Warehouse B")
    .Via("Express")
    .Build();
```

`IHasDestination<TProduct>`, `IHasCarrier<TProduct>`, and `IBuildable<TProduct>` are generic over
the *product*, not the builder — a second step builder for a different product (say, an
`OrderBuilder`) can implement the same three interfaces and get the identical compile-time-ordered
call sequence without redeclaring the step contracts. This is the type-state pattern generalized:
the interfaces describe legal states of construction, and interface implementation (not
inheritance) is what makes a single class walk through all of them via `this`.

## Requirements and restrictions

- Every step interface's method must return the *next* step's interface type, not the concrete
  builder — returning the concrete type early re-exposes every method on it (including ones from
  later steps, if the class implements those interfaces too, which it typically does since one
  class backs the whole chain), silently defeating the ordering guarantee.
- Optional steps don't fit this shape cleanly: a step-builder chain enforces one fixed order, so a
  product with several truly optional fields (no required order, none mandatory) is better served
  by the plain fluent builder pattern in `references/`, reserving the step-builder shape for
  products with a small number of genuinely mandatory, meaningfully ordered inputs.

## Fallback

Nothing here needs anything past interfaces and generics, both available from C# 2.0 — see
[references/csharp2-generic-builders.md](../references/csharp2-generic-builders.md). There is no
older fallback for the compile-time ordering guarantee itself: without interfaces exposing only the
next legal step, the best available substitute is a plain fluent builder (see
[references/pre-csharp2-classic-builder.md](../references/pre-csharp2-classic-builder.md)) with a
run-time check inside `Build()` that throws if a required step was skipped.
