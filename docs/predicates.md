# Predicate types

How to write and register the predicates that a rule calls. The meaning of each operator is in the [Strong Kleene (K3) reference](strong-k3/README.md). Back to the [README](../README.md).

## Registration shapes

Every predicate is one of four registration shapes. The shapes mix freely in one `PredicateRegistryBuilder<TContext>.Build()`. Two independent axes describe a predicate. The first axis is how many rule-authored arguments it takes: zero, one, or several ("n"). The second axis is where its implementation comes from: a stateless lambda, or a class that DI resolves.

| Shape | Arguments | Implementation | When to use |
| --- | --- | --- | --- |
| Lambda, 0 args | none | stateless delegate | A simple stateless check with no rule-authored parameter. |
| Lambda, 1 arg | one | stateless delegate with a 1-argument schema | The common case. A stateless check that the rule text parameterizes, for example `hasTopping(topping: "greenOlives")`. |
| Class-based (DI), 0 args | none | `IPredicate<TContext>` | Needs a scoped or injected dependency but no rule-authored parameter. |
| Class-based (DI), n args | several | `IPredicate<TContext>` with a multi-argument schema | Needs rule-authored parameters and one or more injected dependencies. |

## Ready-made predicates

Before you write a predicate by hand, check [`TruthWeaver.Predicates`](../src/TruthWeaver.Predicates). It ships generic factories that take a value selector:

- `StringPredicates`, `CollectionPredicates` and `RegexPredicates` cover string comparison, null and empty checks, set equality and regex matching.
- `SelectedValuePredicates` covers the externally selected value pattern (see [below](#n-arguments-class-based-externally-selected-value)) for a lookup client that is safe to share.

Every method on `StringPredicates` except one is ordinal-only and has a fixed behavior. A case-insensitive variant is a separate predicate (`EqualsIgnoreCase`), never a rule-text flag on `Equals`. The exception is `StringPredicates.EqualsConfigurable`. It is one predicate whose `ignoreCase` and `trim` arguments the rule sets. It is case-insensitive by default. The comparison is always ordinal, so the predicate has no `culture` argument. A rule that passes a `culture` argument (even `culture: ""`) fails to compile with an `UnknownArgument` diagnostic that tells the author to remove it. Use `EqualsConfigurable` when a rule author needs this flexibility. Otherwise register one fixed-behavior predicate for each name.

### Null selected values

A null selected value is a definite `False` by default, with no fault. These members take an optional `nullBehavior` parameter that the host sets at registration:

- every `StringPredicates` comparison (`Equals`, `EqualsIgnoreCase`, `StartsWith`, `EndsWith`, `Contains` and `EqualsConfigurable`)
- `RegexPredicates.Matches`
- `CollectionPredicates.SetEquals`

`NullBehavior.Unknown` makes a null selected value answer `Unknown` instead, still without a fault. With this setting, `NOT hasCrust(crust: "thin")` stays `Unknown` for an order with no crust and does not become `True`. `Decision.IsSatisfied` stays fail-closed. The default is `NullBehavior.False`. `StringPredicates.IsNullOrEmpty` has no option. It is a null test and always returns a definite answer.

```csharp
StringPredicates.Equals<PizzaOrder>(
    "hasCrust", order => order.Crust, "Has Crust", argumentName: "crust",
    nullBehavior: NullBehavior.Unknown);
```

## 0 arguments, stateless lambda

```csharp
.Add(
    PredicateSchema.NoArguments("isBanned", "Is Banned", "Is the current customer's account banned?"),
    (customer, args, ct) => ValueTask.FromResult(customer.IsBanned ? TruthValue.True : TruthValue.False))
```

## 1 argument, stateless lambda

[Example 3](../README.md#3-named-arguments) shows `hasTopping(topping: "greenOlives")`. It has a single named `string` argument and no injected dependency.

## 0 arguments, class-based (DI)

[Example 1](../README.md#1-a-single-predicate) shows `LovesPineapple`. It is a class that implements `IPredicate<TContext>`. DI resolves it again from `IServiceProvider` on every evaluation. Use this shape when a scoped dependency, for example a `DbContext`, is involved, even when the predicate has no rule-authored parameter.

## n arguments, class-based, multiple injected dependencies

This shape combines everything. The predicate below has two rule-authored arguments and two constructor-injected dependencies. DI resolves the dependencies on every evaluation.

```csharp
public sealed class HasEarnedEnoughLoyaltyStamps : IPredicate<PizzaOrder>
{
    private readonly ILoyaltyStampStore stamps;
    private readonly TimeProvider clock;

    public HasEarnedEnoughLoyaltyStamps(ILoyaltyStampStore stamps, TimeProvider clock)
    {
        this.stamps = stamps;
        this.clock = clock;
    }

    public static PredicateSchema Schema =>
        new(
            "hasEarnedEnoughLoyaltyStamps",
            "Has Earned Enough Loyalty Stamps",
            "Has the order's customer earned at least the given number of loyalty stamps within the given time window?",
            [
                new PredicateArgumentSchema("minCount", "The minimum number of loyalty stamps required.", LiteralKind.Int64),
                new PredicateArgumentSchema("withinDays", "The lookback window, in days.", LiteralKind.Int64),
            ]);

    public async ValueTask<TruthValue> EvaluateAsync(PizzaOrder order, PredicateArguments args, CancellationToken ct)
    {
        long minCount = args.GetInt64("minCount");
        long withinDays = args.GetInt64("withinDays");
        DateTimeOffset cutoff = this.clock.GetUtcNow().AddDays(-withinDays);

        long count = await this.stamps.CountStampsSinceAsync(order.Id, cutoff, ct);
        return count >= minCount ? TruthValue.True : TruthValue.False;
    }
}
```

A rule uses it as `hasEarnedEnoughLoyaltyStamps(minCount: 5, withinDays: 30)`. `ILoyaltyStampStore` can be scoped (for example, a store that an `IDbContextFactory` backs), and `TimeProvider` is typically a singleton. Both resolve correctly on every evaluation, because DI resolves the predicate again from `IServiceProvider` and does not construct it once at registration.

`AddTruthWeaver` registers the registry and the compiler. It does not register the predicate types. The host registers a class-based predicate and its dependencies in the container, in the same way as any other DI service:

```csharp
services.AddScoped<ILoyaltyStampStore, LoyaltyStampStore>();
services.AddSingleton(TimeProvider.System);
services.AddScoped<HasEarnedEnoughLoyaltyStamps>();  // the predicate type itself
services.AddScoped<LovesPineapple>();

services.AddTruthWeaver<PizzaOrder>(builder => builder
    .Add<LovesPineapple>()
    .Add<HasEarnedEnoughLoyaltyStamps>());
```

Lambda and class-based predicates register through the same `PredicateRegistryBuilder<TContext>.Add(...)` overloads. The difference is the dependency lifetime and the number of rule-authored arguments that the schema declares. The rule text, and the way the compiler validates a term, are the same for both.

## n arguments, class-based, externally-selected value

`HasEarnedEnoughLoyaltyStamps` injects a dependency to read a value that it already knows how to interpret. The values `minCount` and `withinDays` are used directly. A related shape is different. A rule-text literal argument, a `TContext`-supplied value, or both, is a key to look up. It is not a value that is ready to use. A constructor-injected service does that lookup live, before the predicate can answer. This pattern has no single canonical shape. It covers three cases, and none is more central than the others:

1. **Single-value, no comparison target.** The literal key resolves directly to the answer. There is no other side to compare against, and the predicate can leave `TContext` unread. A feature-flag check is the classic instance:

   ```csharp
   public sealed class IsPromoActive(IPromoService promos) : IPredicate<object?>
   {
       public static PredicateSchema Schema =>
           new(
               "isPromoActive",
               "Is Promo Active",
               "Is the given promo code currently active, selected live from the promotions service?",
               [new PredicateArgumentSchema("promoCode", "The promo code to look up.", LiteralKind.String)]);

       public async ValueTask<TruthValue> EvaluateAsync(object? context, PredicateArguments args, CancellationToken ct) =>
           await promos.IsActiveAsync(args.GetString("promoCode"), ct) ? TruthValue.True : TruthValue.False;
   }
   ```

   A rule uses it as `isPromoActive(promoCode: "SUMMER-2026")`.

2. **Single-sided value check.** The service resolves one side live: the argument or a context value. The other side is a plain value on `TContext` that needs no resolution. `TContext` here has no user field. The pattern is about a key that needs a live lookup, not about the current user:

   ```csharp
   public sealed class IsWithinZoneLimit(IZoneLimitLookupService zoneLimits) : IPredicate<DeliveryRun>
   {
       public static PredicateSchema Schema =>
           new(
               "isWithinZoneLimit",
               "Is Within Zone Limit",
               "Is the delivery run's amount within the live order limit resolved for the given delivery zone code?",
               [new PredicateArgumentSchema("zoneCode", "The delivery zone code to look up a live limit for.", LiteralKind.String)]);

       public async ValueTask<TruthValue> EvaluateAsync(DeliveryRun run, PredicateArguments args, CancellationToken ct)
       {
           string zoneCode = args.GetString("zoneCode");
           decimal limit = await zoneLimits.ResolveLimitAsync(zoneCode, ct);
           return run.Amount <= limit ? TruthValue.True : TruthValue.False;
       }
   }
   ```

   A rule uses it as `isWithinZoneLimit(zoneCode: "Z-100")`. Only `zoneCode` is resolved. The predicate reads `run.Amount` directly from `TContext`.

3. **Two-sided comparison.** The injected service resolves both a `TContext`-supplied anchor and the rule-text argument independently. The predicate compares the two resolved results. A relationship check is one instance of this family. It is not the pattern itself:

   ```csharp
   public sealed class IsAssignedToCandidateDriver(IDriverLookupService drivers) : IPredicate<PizzaOrder>
   {
       public static PredicateSchema Schema =>
           new(
               "isAssignedToCandidateDriver",
               "Is Assigned To Candidate Driver",
               "Does the order's actual assigned driver, resolved live, match the given candidate?",
               [new PredicateArgumentSchema("candidateDriverId", "The candidate driver to validate.", LiteralKind.Guid)]);

       public async ValueTask<TruthValue> EvaluateAsync(PizzaOrder order, PredicateArguments args, CancellationToken ct)
       {
           Guid candidateDriverId = args.GetGuid("candidateDriverId");
           // candidateDriverId here belongs to "Mister Moneybags," our top delivery driver.
           Guid actualDriverId = await drivers.ResolveDriverIdAsync(order.Id, ct);
           return actualDriverId == candidateDriverId ? TruthValue.True : TruthValue.False;
       }
   }
   ```

   A rule uses it as `isAssignedToCandidateDriver(candidateDriverId: "3fa85f64-5717-4562-b3fc-2c963f66afa6")`. The context, `order`, is an order id and not the current user. It needs its own resolution as much as the argument does. Neither side of a two-sided comparison is the identity side.

These rules hold for all three shapes:

- The rule-text argument is a key, not necessarily an identity. A `String` cost-center code is as valid a key as a `Guid`. What the key resolves to has no role in the rule text, JSON or YAML surface. It exists only inside `EvaluateAsync`.
- A predicate can choose to read `TContext`. Shape 1 never reads it. Shapes 2 and 3 read it, but the pattern does not require it.
- Term identity ([CONTEXT.md#term-identity](../CONTEXT.md#term-identity)) is not affected. The engine compares the literal argument as an ordinary literal for memoization. What the argument resolves to on one evaluation never enters term identity. The predicate-author contract ([CONTEXT.md#the-predicate-author-contract](../CONTEXT.md#the-predicate-author-contract)) still applies. The same argument and the same context, within one evaluation, must give the same answer. A resolution service that is consistent within a single evaluation meets the contract, even if the underlying data changes between evaluations.
- The live call happens inside `EvaluateAsync`. A lookup failure, such as a timeout or a connection error, becomes a `Fault` and `TruthValue.Unknown`, as for any other predicate fault. It is never an unhandled exception. A predicate that cannot decide (for example, when the data is not available) returns `TruthValue.Unknown` directly, with no `Fault`. The predicate needs no special handling. See [`IPredicate<TContext>`](../src/TruthWeaver.Abstractions/IPredicate.cs).

Every shape above works with no engine change. The predicate selects whatever it needs. A rule that needs a per-request value in an argument can also read it from a data source (see [Arguments read from a data source](#arguments-read-from-a-data-source)).

All three examples above are class-based. Use a class when the object that does the selecting is a scoped dependency (a `DbContext`, a per-request `HttpClient`) that DI must create again on every evaluation. Use `SelectedValuePredicates` when the lookup client is safe to capture once. Examples are a long-lived, thread-safe instance such as a cached feature-flag reader, or an `HttpClient`-backed lookup wrapper that the host already holds. `SelectedValuePredicates` is in [`TruthWeaver.Predicates`](../src/TruthWeaver.Predicates). It covers the same pattern as a lightweight lambda factory and needs no one-off class. The single-value convenience overload matches shape 1:

```csharp
(PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
    SelectedValuePredicates.Create<object?>(
        "isPromoActive",
        "Is Promo Active",
        "Is the given promo code currently active, selected live from the promotions service?",
        async (_, args, ct) => await promos.IsActiveAsync(args.GetString("promoCode"), ct) ? TruthValue.True : TruthValue.False,
        new PredicateArgumentSchema("promoCode", "The promo code to look up.", LiteralKind.String));
```

A second overload takes a separate `test` delegate for shapes 2 and 3. Use it when it is clearer to keep "select" apart from "turn the selected value into an answer". Both paths solve the same pattern, and neither replaces the other. Use `SelectedValuePredicates` when the lookup client is safe to share. Use a hand-written `IPredicate<TContext>` when it is not.

## Arguments read from a data source

A literal argument is fixed in the rule. When the value changes per request, or lives in a JSON or YAML document, write a variable reference instead: the source name and a query. The engine resolves it on every evaluation.

<!-- doctest:rule ageVariable -->
```text
ageAtLeast(min: from("user", "$.minAge"))
```

Declare the source names when you compile (`new CompilerOptions(DataSources: ...)` with a `DataSourceDeclarations`), and pass the sources when you evaluate (`rule.EvaluateAsync(context, services, dataSources)`). The [data sources guide](data-sources.md) describes the declarations, the query validators, the diagnostics, the faults and `FakeDataSource` for tests.
