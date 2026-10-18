# TruthWeaver.Generators

A Roslyn source generator that writes predicate registration code at compile time.

## Purpose

Mark a static method with `[Predicate]`, and the generator adds a `Register` method to the type. `Register` adds each
marked method to a `PredicateRegistryBuilder<TContext>`, with a schema built from the method signature. The generated
code calls the same public `Add` method as hand-written registration. It uses no reflection and no assembly scanning, so
it is trim and Native AOT safe. The package holds only an analyzer and adds no assembly to the build output.

```csharp
public static partial class OrderPredicates
{
    [Predicate("hasMinimumOrders", "Has minimum orders", "Did the customer place enough orders?")]
    public static TruthValue HasMinimumOrders(Customer customer, long minimum = 3) =>
        customer.OrderCount >= minimum ? TruthValue.True : TruthValue.False;
}

PredicateRegistry<Customer> registry = OrderPredicates.Register(PredicateRegistry<Customer>.CreateBuilder()).Build();
```

The full rules for methods, parameters and diagnostics are in [Predicate source generator](../../docs/predicate-source-generator.md).

## Depends on

No TruthWeaver packages.

## Key types

| Type | Role |
| --- | --- |
| `PredicateRegistrationGenerator` | The incremental generator. It finds the marked methods and emits `Register`. |

## Notes

The type that holds a marked method, and each type around it, must be `partial`. The generator adds `[Predicate]` to the
consuming compilation as an internal type, so you do not reference a run-time assembly for it.
