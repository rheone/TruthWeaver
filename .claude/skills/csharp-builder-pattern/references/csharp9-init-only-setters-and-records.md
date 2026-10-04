# Init-Only Setters, Records, `with`-Expressions, and Target-Typed `new` (C# 9.0, GA November 10, 2020)

C# 9.0 is the single biggest inflection point for this skill, for a reason worth stating plainly:
most of what it adds doesn't change how you *write* a builder — it changes when you need one at
all. Init-only setters let a type expose object-initializer-style construction (`new Foo { X = 1 }`)
while staying immutable afterward, which is precisely the property a builder previously had to
provide by hand via its own accumulate-then-`Build()` shape. Records add a compact syntax for
immutable data types plus `with`-expressions for non-destructive copies, competing directly with a
builder used only to produce variations of an existing instance. Target-typed `new` is the one
genuine "changes how you write a builder" item in this tier: it shortens every builder
instantiation at the call site.

## Syntax

```csharp
// init-only setter: settable only during object-initializer syntax, immutable after.
public sealed class Invoice
{
    public string Title { get; init; } = "Untitled";
    public decimal Amount { get; init; }
}

// target-typed new: the constructed type is inferred from the declaration's type, not repeated.
InvoiceBuilder builder = new(); // instead of new InvoiceBuilder()

// record + with-expression: an immutable data type plus non-destructive copy syntax, built in.
public sealed record InvoiceRecord(string Title, decimal Amount);
```

## Basic use case: init-only properties replace a builder for a simple, flat, always-valid shape

```csharp
var invoice = new Invoice
{
    Title = "March",
    Amount = 199.99m
};

// invoice.Amount = 0m; // compile error: init-only, not settable after construction
```

This is the direct competitor to [csharp3's object initializer](csharp3-object-initializers-and-fluent-extensions.md)
tier, now closing the one gap object initializers had against a builder: the result is genuinely
immutable, not merely settable-until-someone-does. For a type with no cross-property validation and
no required subset, this removes the last reason to reach for a builder at all — see
[specialized/builder-vs-modern-alternatives.md](../specialized/builder-vs-modern-alternatives.md).

## Basic use case: target-typed `new` shortens every builder call site

```csharp
public sealed class InvoiceBuilder
{
    public InvoiceBuilder WithTitle(string title) => this;
    public InvoiceBuilder WithAmount(decimal amount) => this;
    public Invoice Build() => new();
}

Invoice invoice = new InvoiceBuilder()
    .WithTitle("March")
    .WithAmount(199.99m)
    .Build();

// with target-typed new, the builder's own declaration site can drop the repeated type name too:
InvoiceBuilder reusable = new();
reusable.WithTitle("April");
```

Unlike init-only setters and records, target-typed `new` is a pure syntax shortening with no effect
on builder *design* — it applies to instantiating the builder itself, and to `Build()`'s own
`return new();` when the return type is already declared on the method signature.

## Advanced use case: a builder wrapping a record product, mixing immutability with staged assembly

```csharp
public sealed record ShipmentPlan(string Carrier, IReadOnlyList<string> Stops, bool SignatureRequired);

public abstract class Builder<TSelf, TProduct> where TSelf : Builder<TSelf, TProduct>
{
    public abstract TProduct Build();
}

public sealed class ShipmentPlanBuilder : Builder<ShipmentPlanBuilder, ShipmentPlan>
{
    private string _carrier = "Standard";
    private readonly List<string> _stops = new();
    private bool _signatureRequired;

    public ShipmentPlanBuilder WithCarrier(string carrier) { _carrier = carrier; return this; }
    public ShipmentPlanBuilder AddStop(string stop) { _stops.Add(stop); return this; }
    public ShipmentPlanBuilder RequireSignature() { _signatureRequired = true; return this; }

    public override ShipmentPlan Build() => new(_carrier, _stops.AsReadOnly(), _signatureRequired);
}
```

```csharp
ShipmentPlan plan = new ShipmentPlanBuilder()
    .WithCarrier("Express")
    .AddStop("Warehouse A")
    .AddStop("Warehouse B")
    .RequireSignature()
    .Build();

// once built, a small variation doesn't need the builder again — a record's own with-expression covers it:
ShipmentPlan urgentPlan = plan with { Carrier = "Overnight" };
```

The builder still earns its place here because assembling `_stops` is a genuine multi-step process
a single object initializer or `with`-expression can't express — but *after* the plan exists,
producing a one-field variant goes through the record's `with`-expression instead of routing back
through the builder. This is the realistic coexistence: a builder for assembly, `with` for
post-construction variation of the same immutable shape.

## Requirements and restrictions

- An init-only property (`{ get; init; }`) can be set via object-initializer syntax or from within
  the declaring type's own constructor, but never afterward — including from that type's own
  instance methods once construction has finished. A builder targeting an init-only product still
  has to accumulate state in its own mutable fields first, then emit the product in one
  object-initializer or constructor call, same as every earlier tier's `Build()`.
- `with`-expressions require the source type to be a record (or, from C# 10.0, a record struct) or
  otherwise implement a compiler-recognized non-destructive-mutation pattern; a builder's own
  product type has to actually be declared `record`/`record struct` to get `with` for free — an
  ordinary class with init-only properties does not gain `with` automatically.
- Target-typed `new` needs the target's type to be unambiguous from context (a variable
  declaration, a field/property type, a `return` statement's declared return type); it doesn't
  apply where the compiler can't already determine the type another way, such as an overload set
  with multiple candidate parameter types.

## Fallback

On a target before C# 9.0 (down to C# 6.0, for the read-only product shape — see
[csharp6-readonly-autoprops-and-expression-bodied-members.md](csharp6-readonly-autoprops-and-expression-bodied-members.md)),
use `{ get; private set; }` or constructor-only assignment instead of `init`, write every builder
and product instantiation with the type name repeated (`new InvoiceBuilder()`, `new Invoice(...)`),
and replace a `with`-expression by cloning the product's fields into a new builder instance (or a
hand-written `With(Action<TSelf> mutate)`-style copy constructor) instead of relying on
compiler-generated non-destructive mutation.
