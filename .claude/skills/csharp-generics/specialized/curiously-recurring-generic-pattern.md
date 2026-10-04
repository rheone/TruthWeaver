# Curiously Recurring Generic Pattern (CRTP)

A self-referencing constraint — `where T : Base<T>` — where a base type is parameterized by its
own derived type. Works from C# 2.0 (no special version requirement); it's a usage pattern of
ordinary constraints, not a distinct language feature.

## The shape

```csharp
public abstract class Entity<TSelf> where TSelf : Entity<TSelf>
{
    public int Id { get; init; }

    public bool SameAs(TSelf other) => other is not null && Id == other.Id;
}

public class Order : Entity<Order>
{
    public decimal Total { get; init; }
}
```

`Order` derives from `Entity<Order>` — the derived type is its own base's type argument. This
gets `SameAs` typed as `SameAs(Order other)` on `Order`, not `SameAs(Entity<Order> other)`,
without `Order` having to override or restate the method.

## Why: strongly-typed fluent returns from a base class

The most common reason to reach for CRTP is a fluent base class whose chained methods need to
return the *derived* type, not the base type, so the derived type's own members stay chainable:

```csharp
public abstract class QueryBuilder<TSelf> where TSelf : QueryBuilder<TSelf>
{
    protected readonly List<string> Clauses = new();

    public TSelf Where(string clause)
    {
        Clauses.Add(clause);
        return (TSelf)this;
    }
}

public class OrderQueryBuilder : QueryBuilder<OrderQueryBuilder>
{
    public OrderQueryBuilder ForCustomer(int customerId) =>
        Where($"CustomerId = {customerId}");
}

var query = new OrderQueryBuilder()
    .Where("Status = 'Pending'")
    .ForCustomer(42); // chains onto OrderQueryBuilder's own method — impossible if Where returned QueryBuilder<TSelf>
```

Without CRTP, `Where` would have to return `QueryBuilder<TSelf>`, and the chain would lose access
to `ForCustomer` after the first `.Where(...)` call.

## The unchecked cast

`(TSelf)this` is the pattern's one weak point: nothing in the language stops a second class from
deriving from `QueryBuilder<OrderQueryBuilder>` instead of correctly closing the loop over itself,
which makes the cast throw an `InvalidCastException` at runtime rather than fail at compile time.
Mitigate with a `protected` (never `public`) constructor plus a comment, or accept the cast as a
documented, narrow trust boundary — there is no fully compile-time-safe version of this pattern in
C# today.

## Combining with generic math (C# 11+)

CRTP is also how `static abstract` interface members express "an operation returning the
implementing type," since an ordinary interface can't say "return the type that implements me":

```csharp
public interface IAddable<TSelf> where TSelf : IAddable<TSelf>
{
    static abstract TSelf operator +(TSelf left, TSelf right);
}
```

This is the same self-referencing shape as the class-based examples above, applied to an
interface constraint instead of a base class — see
[generic-math-numeric-abstractions.md](generic-math-numeric-abstractions.md) for the full
treatment.

## Fallback

None needed — CRTP is plain C# 2.0 generics used in a particular shape, not a version-gated
feature. The generic-math combination specifically needs C# 11 (`static abstract`); the
class-based fluent-builder form works on every version this skill covers.
