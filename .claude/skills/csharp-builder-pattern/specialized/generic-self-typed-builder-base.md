# Generic Self-Typed Builder Base (CRTP)

The curiously recurring template pattern (CRTP) applied to a builder: a generic base class
constrained to be parameterized by its own eventual subtype, so base-declared fluent methods
return the *derived* builder's type rather than the base's. This pattern was introduced in
[references/csharp2-generic-builders.md](../references/csharp2-generic-builders.md) — generics
and the `where TSelf : Builder<TSelf, TProduct>` constraint are both C# 2.0 features — and every
worked example below builds on that baseline. This file goes further than the reference tier: a
multi-level hierarchy, a shared-state base with derived-only fields, and the failure mode that
happens when the pattern is applied incorrectly.

## Basic: a two-level builder hierarchy sharing common fluent methods

```csharp
public abstract class EntityBuilder<TSelf, TEntity> where TSelf : EntityBuilder<TSelf, TEntity>
{
    protected string? Id;
    protected DateTimeOffset CreatedAt = DateTimeOffset.UtcNow;

    public TSelf WithId(string id)
    {
        Id = id;
        return (TSelf)this;
    }

    public TSelf CreatedOn(DateTimeOffset createdAt)
    {
        CreatedAt = createdAt;
        return (TSelf)this;
    }

    public abstract TEntity Build();
}

public sealed class CustomerBuilder : EntityBuilder<CustomerBuilder, Customer>
{
    private string _name = "";

    public CustomerBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public override Customer Build() => new(Id ?? throw new InvalidOperationException("Id required"), _name, CreatedAt);
}
```

```csharp
Customer customer = new CustomerBuilder()
    .WithId("CUST-1")   // declared on EntityBuilder<TSelf,TEntity> — returns CustomerBuilder
    .Named("Ada")        // declared on CustomerBuilder itself
    .CreatedOn(DateTimeOffset.UtcNow)
    .Build();
```

Every subtype of `EntityBuilder<TSelf, TEntity>` gets `WithId` and `CreatedOn` for free, fully
chainable with its own fluent methods, because each one closes `TSelf` over itself
(`EntityBuilder<CustomerBuilder, Customer>`). Writing `WithId`/`CreatedOn` once here, instead of on
every entity's own builder, is the entire payoff of the pattern.

## Advanced: a generic builder base shared across sibling product hierarchies

```csharp
public interface IValidator<in T>
{
    void Validate(T product);
}

public abstract class ValidatingBuilder<TSelf, TProduct> where TSelf : ValidatingBuilder<TSelf, TProduct>
{
    private readonly List<IValidator<TProduct>> _validators = new();

    public TSelf WithValidator(IValidator<TProduct> validator)
    {
        _validators.Add(validator);
        return (TSelf)this;
    }

    protected abstract TProduct CreateProduct();

    public TProduct Build()
    {
        TProduct product = CreateProduct();
        foreach (IValidator<TProduct> validator in _validators)
        {
            validator.Validate(product); // throws if invalid
        }
        return product;
    }
}

public sealed class ShipmentBuilder : ValidatingBuilder<ShipmentBuilder, Shipment>
{
    private string _destination = "";

    public ShipmentBuilder To(string destination)
    {
        _destination = destination;
        return this;
    }

    protected override Shipment CreateProduct() => new(_destination);
}
```

```csharp
Shipment shipment = new ShipmentBuilder()
    .To("Warehouse B")
    .WithValidator(new NonEmptyDestinationValidator())
    .Build(); // Build() itself is declared once on the base and is not overridden per subtype
```

Here `Build()` is declared *non-abstract* on the base and is never overridden — only the narrower
`CreateProduct()` step varies per subtype, while the validation loop around it is shared. This
splits the CRTP base into two roles: fluent configuration methods that return `TSelf` for chaining,
and a fixed algorithm (`Build()`) that calls a subtype-supplied hook — the Template Method pattern
layered on top of the builder base.

## Advanced: the failure mode — mismatching `TSelf` against the actual runtime type

```csharp
// Compiles, but is a latent bug: TSelf is declared as CustomerBuilder while this class is Rogue.
public sealed class Rogue : EntityBuilder<CustomerBuilder, Customer>
{
    public override Customer Build() => new("id", "name", CreatedAt);
}
```

```csharp
var rogue = new Rogue();
// rogue.WithId("x") throws InvalidCastException at run time: (TSelf)this casts a Rogue to CustomerBuilder and fails
```

Nothing in the CRTP constraint (`where TSelf : EntityBuilder<TSelf, TEntity>`) stops a class from
closing `TSelf` over a *different* sibling type instead of itself — the constraint only requires
`TSelf` to be some subtype of the base, not specifically the declaring type. The convention that
`TSelf` is always "yourself" is enforced by discipline, not the compiler; a code-review checklist
item ("does every `class Foo : Builder<TSelf, ...>` declaration have `TSelf == Foo`?") is the
practical mitigation, since there is no `where TSelf : Builder<Self, ...>`-style self-referential
enforcement C# offers beyond the recursive constraint shown here.

## Fallback

Every example on this page needs generics and the `where TSelf : ...` constraint — both from
C# 2.0 — but nothing newer; see
[references/csharp2-generic-builders.md](../references/csharp2-generic-builders.md) for the
baseline this file assumes. There is no earlier-version fallback for the CRTP structure itself:
without generics, each builder in a hierarchy has to duplicate the shared fluent methods
(`WithId`, `CreatedOn`) by hand on every concrete builder class instead of inheriting them once.
