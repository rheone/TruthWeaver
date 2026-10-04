# When a Factory Is Warranted vs. `new` or DI Alone

A factory — Factory Method, Abstract Factory, or a generic `IFactory<T>` — is optional structure
around object creation. Reach for it only when creation needs to vary, be deferred, or be
substituted; otherwise it adds an interface, a class, and a registration for something a direct
constructor call or plain constructor injection already does correctly.

## When a direct `new` call is enough

```csharp
var receipt = new OrderReceipt(order, shippingCost);
```

No factory is warranted when: there's exactly one way to build the type, its constructor arguments
are all available at the call site, no caller ever needs a different concrete type or a different
construction strategy, and nothing about the construction needs to be deferred to a later point than
"right here." `OrderReceipt` above is unlikely to ever need more than this — it's a plain data
holder assembled from values the caller already has in hand.

## When constructor injection alone is enough

```csharp
public sealed class OrderProcessor
{
    private readonly IShippingCostStrategy _shippingStrategy;

    public OrderProcessor(IShippingCostStrategy shippingStrategy) =>
        _shippingStrategy = shippingStrategy;
}
```

No factory is warranted when a dependency's *entire* lifetime matches its consumer's — the container
constructs it once (or once per scope) and hands it to the consumer's constructor, and the consumer
never needs to create additional instances of it later, with different runtime arguments, at a time
of the consumer's own choosing. This is the ordinary DI case, and injecting a factory here instead of
the dependency itself is unneeded indirection — the consumer doesn't need "a way to create an
`IShippingCostStrategy`," it needs the one it was configured with.

## When a factory is warranted

- **The concrete type to construct depends on a runtime value the container can't know at
  registration time** — a request payload's discriminator, a user's selected option — see
  [factory-method.md](factory-method.md)'s `switch`-based `Create` method.
- **A family of related objects must be constructed consistently together**, and mixing objects from
  different families would be a bug — see [abstract-factory.md](abstract-factory.md).
- **A long-lived consumer needs a fresh instance of a short-lived dependency on demand**, not once at
  construction — see the transient-from-singleton case in
  [factory-registration-via-di.md](factory-registration-via-di.md).
- **Construction needs arguments that aren't known until well after the consumer itself is
  constructed** — a factory method or delegate accepting those arguments lets creation be deferred to
  the moment they become available, where a constructor-injected instance would have had to be built
  too early.
- **Tests need to substitute a fake creation strategy** without controlling every argument the real
  constructor needs by hand — injecting a factory (or a `Func<>`) gives a test a single seam to
  replace, rather than needing to construct a fully real instance through its real constructor in
  every test.

## The decision in one pass

Ask, in order: does this type have more than one way to be built, or more than one family it needs
to stay consistent with? If no, use `new` or constructor injection directly. If yes, does the choice
need to happen at runtime, per call, rather than once at startup? If no, a static factory method is
enough. If yes, an interface-based factory (or a generic `IFactory<T>`/`Func<>`) resolved through DI
is warranted — registered and consumed as covered in
[factory-registration-via-di.md](factory-registration-via-di.md).
