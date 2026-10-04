# Generic Builders and Self-Returning Chains (C# 2.0, .NET Framework 2.0, GA November 7, 2005)

C# 2.0 added generics to the language and CLR. For the builder pattern this is a real capability
change, not just convenience: before generics, a builder's "accumulate state, then construct"
shape had to be re-typed by hand for every product type, because there was no way to parameterize
a class over the type it builds. C# 2.0 makes a reusable, generic builder base possible for the
first time, and it also makes fluent method chaining worth writing deliberately — each `Set*` call
can now return `this` typed generically, so a chain of calls type-checks and reads as one
expression instead of a run of separate statements.

## Syntax

```csharp
public abstract class Builder<TProduct>
{
    public abstract TProduct Build();
}

public class ListBuilder<T>
{
    private List<T> _items = new List<T>();

    public ListBuilder<T> Add(T item)
    {
        _items.Add(item);
        return this; // chaining: only needs a return type, but a *generic* one is new here
    }

    public List<T> Build()
    {
        return new List<T>(_items);
    }
}
```

## Basic use case

```csharp
List<string> names = new ListBuilder<string>()
    .Add("Ada")
    .Add("Grace")
    .Add("Katherine")
    .Build();
```

`ListBuilder<T>` is written once and reused for any element type — `ListBuilder<int>`,
`ListBuilder<Order>` — which is exactly the reuse C# 1.0 couldn't offer. Each `Add` call chains
because it returns `ListBuilder<T>`, resolved to the same closed generic type as the receiver.

## Advanced use case: the self-returning generic base (CRTP) for fluent chains that survive inheritance

A plain `Builder<TProduct>` base runs into a problem the moment a derived builder wants to add its
own fluent methods: if the base's methods return the base type, chaining through a derived method
loses the derived type.

```csharp
public abstract class Builder<TSelf, TProduct> where TSelf : Builder<TSelf, TProduct>
{
    protected string Title = "Untitled";

    public TSelf WithTitle(string title)
    {
        Title = title;
        return (TSelf)this; // 'this' is Builder<TSelf,TProduct>; the constraint guarantees TSelf is really the runtime type
    }

    public abstract TProduct Build();
}

public sealed class InvoiceBuilder : Builder<InvoiceBuilder, Invoice>
{
    private decimal _amount;

    public InvoiceBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this; // no cast needed here: this already has compile-time type InvoiceBuilder
    }

    public override Invoice Build()
    {
        return new Invoice(Title, _amount);
    }
}
```

```csharp
Invoice invoice = new InvoiceBuilder()
    .WithTitle("March") // declared on the base — returns InvoiceBuilder, not Builder<InvoiceBuilder,Invoice>
    .WithAmount(199.99m) // declared on the derived type
    .Build();
```

`where TSelf : Builder<TSelf, TProduct>` is the **curiously recurring template pattern (CRTP)**
applied to a builder base: `TSelf` is constrained to be (a subtype of) the base class parameterized
by itself. This is the mechanism that lets a base-class fluent method (`WithTitle`) return the
*derived* builder type (`InvoiceBuilder`), so a chain can freely interleave base-declared and
derived-declared fluent calls without ever downcasting at the call site — only the base's own
`(TSelf)this` cast, written once, is needed. This pattern is the foundation
[specialized/generic-self-typed-builder-base.md](../specialized/generic-self-typed-builder-base.md)
builds on with more variations.

## Requirements and restrictions

- The `(TSelf)this` cast inside the base class is not statically verified by the compiler — nothing
  stops a caller from writing `class Rogue : Builder<InvoiceBuilder, Invoice>`, mismatching `TSelf`
  against its own type and turning the cast into a runtime `InvalidCastException`. The convention
  works because every derived builder is expected to close `TSelf` over itself; document that
  expectation since the compiler won't enforce it directly.
- Generic constraints (`where TSelf : ...`) and generic methods both date to C# 2.0 — nothing about
  this pattern needs a later version, though later tiers change what a derived product type or a
  derived builder's own fields look like.

## Fallback

On a target with no generics (.NET Framework 1.0/1.1, C# 1.0), write one non-generic builder class
per product type by hand — see
[pre-csharp2-classic-builder.md](pre-csharp2-classic-builder.md). There is no substitute for a
shared generic base; the reuse this tier provides simply isn't available, so each builder
duplicates its own "accumulate state, then construct" boilerplate.
