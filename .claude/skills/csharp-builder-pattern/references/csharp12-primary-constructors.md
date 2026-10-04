# Primary Constructors on Classes and Structs (C# 12.0, GA November 14, 2023)

Primary constructors were record-only from C# 9.0; C# 12.0 extends the same syntax to ordinary
classes and structs. This is a genuine "changed how you write it" tier for two distinct parts of a
builder setup: the **product** type a builder constructs often becomes a one-line declaration
instead of a hand-written constructor plus field assignments, and the **builder class itself** can
take fixed dependencies (a validator, a default-value provider) through a primary constructor
rather than a conventional constructor body. Collection expressions, shipping in the same release,
are a smaller but related convenience for a builder that accumulates into an array or list.

## Syntax

```csharp
// Primary constructor on an ordinary class — not a record, no value equality, no ToString override.
public sealed class Invoice(string title, decimal amount)
{
    public string Title { get; } = title;
    public decimal Amount { get; } = amount;
}

// Primary constructor on the builder itself, taking a fixed collaborator.
public sealed class InvoiceBuilder(IClock clock)
{
    private string _title = "Untitled";
    private decimal _amount;

    public InvoiceBuilder WithTitle(string title) { _title = title; return this; }
    public InvoiceBuilder WithAmount(decimal amount) { _amount = amount; return this; }

    public Invoice Build() => new(_title, _amount) { IssuedAt = clock.UtcNow }; // 'clock' captured from the primary constructor
}
```

## Basic use case: a product type whose constructor collapses to its declaration

```csharp
public sealed class Point(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
}
```

Before this tier, the same type needed an explicit constructor body (`public Point(int x, int y) { X = x; Y = y; }`)
purely to copy each parameter into a property — boilerplate a builder's `Build()` method has always
had to look past to find the type it's actually assembling. The primary constructor doesn't change
what a builder does, only how tedious the product type it targets is to declare.

## Advanced use case: generic builder with a primary-constructor-injected dependency, using collection expressions to finish assembly

```csharp
public interface IIdGenerator
{
    string NextId();
}

public abstract class Builder<TSelf, TProduct>(IIdGenerator idGenerator) where TSelf : Builder<TSelf, TProduct>
{
    protected readonly IIdGenerator IdGenerator = idGenerator;

    public abstract TProduct Build();
}

public sealed class OrderBuilder(IIdGenerator idGenerator) : Builder<OrderBuilder, Order>(idGenerator)
{
    private readonly List<string> _lineItems = [];

    public OrderBuilder AddLine(string sku)
    {
        _lineItems.Add(sku);
        return this;
    }

    public override Order Build() => new(IdGenerator.NextId(), [.. _lineItems]);
}
```

```csharp
Order order = new OrderBuilder(new SequentialIdGenerator())
    .AddLine("SKU-1")
    .AddLine("SKU-2")
    .Build();
```

Two C# 12.0 features stack here: the generic base's own primary constructor
(`Builder<TSelf, TProduct>(IIdGenerator idGenerator)`) takes the shared collaborator once, and the
derived `OrderBuilder` forwards it via `: Builder<OrderBuilder, Order>(idGenerator)`; separately,
`[.. _lineItems]` is a **collection expression** spread, producing an immutable snapshot array for
the product without a `.ToArray()` call. Neither is required by the pattern — everything here still
compiles as a conventional constructor body and `_lineItems.ToArray()` on C# 11.0 — but both remove
lines that added nothing beyond "get this value from A to B."

## Requirements and restrictions

- A primary constructor's parameters are in scope for the whole class body but are **not**
  automatically fields or properties — `public string Title { get; } = title;` above still needs
  an explicit property declaration if the value should be readable after construction; a parameter
  used only inside a method body (like `clock` in `InvoiceBuilder`, referenced only from `Build()`)
  is captured as a compiler-generated private field without a matching declaration needed.
- Declaring a primary constructor removes the implicit parameterless constructor a class would
  otherwise get; if a builder needs both a dependency-injected primary constructor and a
  zero-argument default path, an explicit secondary constructor chaining to the primary one
  (`public OrderBuilder() : this(new SequentialIdGenerator()) { }`) is required.
- Collection expressions (`[...]`, `[.. spread]`) target a specific collection type from context —
  the same target-typing rule target-typed `new` relies on — so `[.. _lineItems]` above resolves to
  `string[]` only because `Build()`'s constructor call expects that type there.

## Fallback

On a target before C# 12.0 (down to C# 2.0 for the generic self-typed base pattern itself — see
[csharp2-generic-builders.md](csharp2-generic-builders.md)), write the product's and the builder's
constructors as explicit bodies assigning each parameter to a field or property by hand, and
replace `[.. _lineItems]` with `_lineItems.ToArray()` (or `new List<T>(_lineItems).AsReadOnly()`
for a read-only list view) — functionally identical, one call instead of spread syntax.
