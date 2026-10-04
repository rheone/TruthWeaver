# Read-Only Auto-Properties and Expression-Bodied Members (C# 6.0, GA July 20, 2015)

C# 6.0 doesn't touch the builder's own call-chaining shape, but it changes what the *product*
a builder constructs, and the builder class itself, can look like. Read-only auto-properties let a
product type declare `{ get; }` properties set only from the constructor, without hand-writing a
backing field — the shape a builder's `Build()` method needs to target for an immutable product.
Expression-bodied members let simple fluent setters and a `Build()` method collapse to one line
each, cutting the boilerplate a builder class otherwise repeats once per property.

## Syntax

```csharp
public sealed class Invoice
{
    public string Title { get; } // read-only auto-property: settable only in this constructor
    public decimal Amount { get; }

    public Invoice(string title, decimal amount)
    {
        Title = title;
        Amount = amount;
    }
}

public sealed class InvoiceBuilder
{
    private string _title = "Untitled";
    private decimal _amount;

    public InvoiceBuilder WithTitle(string title) { _title = title; return this; } // still fine as a block
    public InvoiceBuilder WithAmount(decimal amount) => AssignAmount(amount); // expression-bodied delegate

    private InvoiceBuilder AssignAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public Invoice Build() => new Invoice(_title, _amount); // expression-bodied Build()
}
```

## Basic use case: an immutable product built entirely through a builder

```csharp
Invoice invoice = new InvoiceBuilder()
    .WithTitle("March")
    .WithAmount(199.99m)
    .Build();

// invoice.Title = "April"; // compile error: Title has no setter, not even init-only yet
```

Before this tier, an "immutable" product built by a builder still needed a hand-written backing
field per property (`private readonly string _title;` plus `public string Title => _title;`).
Read-only auto-properties remove that duplication — the property declaration *is* the storage —
while still requiring the value to arrive through the constructor, which is exactly the shape a
`Build()` method's final call already produces.

## Advanced use case: a generic builder over an expression-bodied fluent surface

```csharp
public abstract class Builder<TSelf, TProduct> where TSelf : Builder<TSelf, TProduct>
{
    public abstract TProduct Build();
}

public sealed class PointBuilder : Builder<PointBuilder, Point>
{
    private int _x;
    private int _y;

    public PointBuilder AtX(int x) => Assign(ref _x, x);
    public PointBuilder AtY(int y) => Assign(ref _y, y);

    public override Point Build() => new Point(_x, _y);

    private PointBuilder Assign(ref int field, int value)
    {
        field = value;
        return this;
    }
}
```

`Assign(ref int field, int value)` is a small generic-in-spirit helper (parameterized over *which*
field, via `ref`, not over a type parameter) that both `AtX` and `AtY` delegate to in one
expression each — the kind of one-line fluent setter expression-bodied members make routine to
write across a whole builder class without each property's setter needing its own multi-line
block body.

## Requirements and restrictions

- A read-only auto-property (`{ get; }`) can only be assigned inside the declaring type's
  constructor (or as a field-style initializer) — not from a builder's own methods after
  construction, and not via an object initializer once the object exists. The builder still has to
  do all its accumulation in its *own* mutable fields, then hand everything to the product's
  constructor in one call, exactly as in earlier tiers.
- An expression-bodied member is restricted to a single expression — a fluent method that needs
  validation or branching before returning `this` still needs a block body.

## Fallback

On a target before C# 6.0 (down to C# 2.0, once generics are available — see
[csharp2-generic-builders.md](csharp2-generic-builders.md)), write the product's properties as
`{ get; private set; }` with a hand-assigned backing pattern, or a fully manual
`private readonly` field plus a `get`-only property block, and write every fluent method and
`Build()` as an ordinary block-bodied method — functionally identical, just more lines.
