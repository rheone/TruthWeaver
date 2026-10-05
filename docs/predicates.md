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

- `StringPredicates`, `CollectionPredicates` and `RegexPredicates` cover string comparison, null, empty and white-space checks, set equality and regex matching.
- `NumericPredicates` covers `Int64` and `Decimal` selections. `ScalarPredicates` covers `Boolean`, `Guid` and `DateTimeOffset` selections. See [Scalar and numeric predicates](#scalar-and-numeric-predicates).
- `TypePredicates` covers the type tests `IsGuid`, `IsNumeric`, `IsUrl`, `IsString` and `IsDateTimeOffset`. Each has an `IsNot...` twin that is its Strong Kleene complement. See [Type tests](#type-tests).
- `SelectedValuePredicates` covers the externally selected value pattern (see [below](#n-arguments-class-based-externally-selected-value)) for a lookup client that is safe to share.

Every method on `StringPredicates` except one is ordinal-only and has a fixed behavior. A case-insensitive variant is a separate predicate (`EqualsIgnoreCase`), never a rule-text flag on `Equals`. The exception is `StringPredicates.EqualsConfigurable`. It is one predicate whose `ignoreCase` and `trim` arguments the rule sets. It is case-insensitive by default. The comparison is always ordinal, so the predicate has no `culture` argument. A rule that passes a `culture` argument (even `culture: ""`) fails to compile with an `UnknownArgument` diagnostic that tells the author to remove it. Use `EqualsConfigurable` when a rule author needs this flexibility. Otherwise register one fixed-behavior predicate for each name.

### Null selected values

A null selected value never faults. A member cannot evaluate a missing value, so every member with an optional `nullBehavior` parameter answers a null selected value with `Unknown` by default (`NullBehavior.Unknown`). `NOT hasCrust(crust: "thin")` stays `Unknown` for an order with no crust, and `Decision.IsSatisfied` stays fail-closed.

The host can choose `NullBehavior.False` for each predicate at registration. A positive member then answers `False` for a null selected value. The exception is `CollectionPredicates.SetEquals`. Under `NullBehavior.False` it reads a null collection as an empty collection, so it answers `True` when the argument array is empty.

A `NotX` twin is the Strong Kleene complement of its positive member for every selected value, a null one included: `True` becomes `False`, `False` becomes `True` and `Unknown` stays `Unknown`. A twin has the same default as its positive member, so both members of a pair registered with the defaults answer `Unknown` for a null selected value. Under `NullBehavior.False` a twin answers `True` for a null selected value, and `NotSetEquals` answers the complement of `SetEquals`. Register both members of a pair with the same setting.

The `StringPredicates` and `RegexPredicates` twins are:

| Member | Class | Meaning |
| --- | --- | --- |
| `NotEqual` | `StringPredicates` | Twin of `Equals` (ordinal, case-sensitive) |
| `NotEqualsIgnoreCase` | `StringPredicates` | Twin of `EqualsIgnoreCase` |
| `NotStartsWith` | `StringPredicates` | Twin of `StartsWith` |
| `NotEndsWith` | `StringPredicates` | Twin of `EndsWith` |
| `NotEqualsConfigurable` | `StringPredicates` | Twin of `EqualsConfigurable`. It takes the same `ignoreCase` and `trim` arguments. |
| `NotContains` | `StringPredicates` | Twin of `Contains` |
| `NotMatches` | `RegexPredicates` | Twin of `Matches`. An invalid pattern faults to `Unknown` with a `Fault`. |
| `IsEmpty` | `StringPredicates` | A non-null empty string. A null selected value is missing, not empty. |
| `IsNotEmpty` | `StringPredicates` | Twin of `IsEmpty` |

`IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace` and `IsNotNullOrWhiteSpace` (all in `StringPredicates`) have no option. They are null tests and always return a definite answer: a null selected value is `True` for `IsNullOrEmpty` and `IsNullOrWhiteSpace`, and `False` for their complements. White space follows `char.IsWhiteSpace`.

```csharp
StringPredicates.Equals<PizzaOrder>(
    "hasCrust", order => order.Crust, "Has Crust", argumentName: "crust",
    nullBehavior: NullBehavior.False);
```

### Scalar and numeric predicates

`NumericPredicates` and `ScalarPredicates` take a nullable selector (`Func<TContext, long?>`, `Func<TContext, decimal?>`, `Func<TContext, bool?>`, `Func<TContext, Guid?>` or `Func<TContext, DateTimeOffset?>`). The selector type picks the overload and the argument kind. Each member has a `NotX` twin that is the Strong Kleene complement.

| Member | Twin | `NumericPredicates` (`Int64`, `Decimal`) | `ScalarPredicates` (`Boolean`, `Guid`, `DateTimeOffset`) |
| --- | --- | --- | --- |
| `Equal` | `NotEqual` | yes | yes |
| `LessThan` | `GreaterThanOrEqual` | yes | not defined |
| `GreaterThan` | `LessThanOrEqual` | yes | not defined |
| `Between` | `Outside` | yes | not defined |
| `In` | `NotIn` | yes | yes |
| `IsNull` | `IsNotNull` | yes | yes |
| `IsDefault` | `IsNotDefault` | yes | yes |

- Ordering and ranges have no meaning for `Boolean` and `Guid`, so they are not defined. For `DateTimeOffset`, ordering and ranges belong to the date-time predicates.
- `Between` is inclusive on both bounds and `Outside` is its exact complement. Reversed bounds (`lower` greater than `upper`) are an authoring error. The predicate throws an `ArgumentException` at evaluation time, even for a null selection. The evaluator records a `Fault` and answers `Unknown`. The bounds are never swapped.
- `In` and `NotIn` test one scalar value against a literal candidate array. A candidate array has the same kind as the selector.
- No value is promoted between kinds. A `long?` selector takes `Int64` literals. A `decimal?` selector takes `Decimal` literals, and a whole number such as `5` is a valid `Decimal` literal. To compare an integer value with a decimal literal, widen it in the selector: `c => (decimal?)c.Count`. Decimal values compare by value, so `1.0` equals `1.00`.
- `DateTimeOffset` values compare by instant, so the same instant in two offsets is equal.
- A null selected value answers as [Null selected values](#null-selected-values) describes. `IsNull` and `IsNotNull` are definite and have no option. `IsDefault` tests `default(T)`: `0`, `false`, `Guid.Empty` or the default `DateTimeOffset`. A null selection is a missing value, not a default.

```csharp
NumericPredicates.Between<Order>("quantityInRange", order => order.Quantity, "Quantity In Range");
```

### Collection predicates

`CollectionPredicates` selects an `IReadOnlyCollection<string>?`. Comparison is ordinal and case-sensitive. Every predicate has a `NotX` twin that is its Strong Kleene complement: `True` becomes `False`, `False` becomes `True`, and `Unknown` stays `Unknown`.

| Predicate | Twin | Argument | `True` when |
| --- | --- | --- | --- |
| `IsEmpty` | `IsNotEmpty` | none | The collection has no elements. |
| `SetEquals` | `NotSetEquals` | `values` (`StringArray`) | The collection and the array hold the same set of strings. |
| `Contains` | `NotContains` | `value` (`String`) | The collection contains the value. |
| `ContainsAny` | `NotContainsAny` | `values` (`StringArray`) | At least one element is in the array. |
| `ContainsAll` | `NotContainsAll` | `values` (`StringArray`) | Every string in the array is an element. |
| `IsSubsetOf` | `IsNotSubsetOf` | `values` (`StringArray`) | Every element is in the array. |
| `In` | `NotIn` | `values` (`StringArray`) | The selected scalar string is in the array. |
| `CountEqual` | `NotCountEqual` | `count` (`Int64`) | The element count equals `count`. |
| `CountLessThan` | `NotCountLessThan` | `count` (`Int64`) | The element count is less than `count`. |
| `CountGreaterThan` | `NotCountGreaterThan` | `count` (`Int64`) | The element count is greater than `count`. |
| `CountLessThanOrEqual` | `NotCountLessThanOrEqual` | `count` (`Int64`) | The element count is at most `count`. |
| `CountGreaterThanOrEqual` | `NotCountGreaterThanOrEqual` | `count` (`Int64`) | The element count is at least `count`. |

`In` and `NotIn` test scalar membership. Their selector returns one `string?`, so a collection selector does not compile. Use `ContainsAny`, `ContainsAll` or `IsSubsetOf` for a collection.

A null collection counts as empty for `IsEmpty` and `IsNotEmpty`. These two predicates always return a definite answer and have no `nullBehavior` option. The other predicates in this table answer a null selected value as [Null selected values](#null-selected-values) describes.

### Date and time predicates

`DateTimePredicates` selects a `DateTimeOffset?` and takes `DateTimeOffset` literal arguments. Values compare by instant, so the same moment with a different offset is equal.

| Predicate | Twin | Arguments | `True` when |
| --- | --- | --- | --- |
| `After` | `NotAfter` | `value` | The selected instant is later than `value`. An equal instant is `False`. |
| `Before` | `NotBefore` | `value` | The selected instant is earlier than `value`. An equal instant is `False`. |
| `Between` | `Outside` | `lower`, `upper` | `lower <= value <= upper`. Both bounds are inclusive. `Outside` is the exact complement. |

`AfterNow` and `BeforeNow` compare the selected instant with the current time. They take a required `TimeProvider` at registration and no rule-text arguments. There is no ambient default clock.

| Predicate | Twin | `True` when |
| --- | --- | --- |
| `AfterNow` | `NotAfterNow` | The selected instant is later than now. An instant equal to now is `False`. |
| `BeforeNow` | `NotBeforeNow` | The selected instant is earlier than now. An instant equal to now is `False`. |

An instant equal to now makes `AfterNow` and `BeforeNow` both `False`. Neither is the complement of the other, so each has its own twin.

```csharp
DateTimePredicates.AfterNow<Order>("expiresAfterNow", order => order.ExpiresAt, TimeProvider.System);
```

The predicate reads the clock each time the engine evaluates it, never at registration. The engine evaluates one term once per `Evaluate` call, so a repeated term sees one instant. Two different terms each read the clock and can see different instants if the clock advances between them. To give every term one instant, register a `TimeProvider` that returns a fixed instant for each evaluation.

Reversed bounds (`lower` later than `upper`) are an authoring error. The predicate throws `ArgumentException`, the evaluation records a fault and the result is `Unknown`. The bounds are never swapped.

A null selected value answers as [Null selected values](#null-selected-values) describes.

There is no `DateTime` literal kind and no `DateTime` overload. A `DateTime` can have an unspecified `Kind`, so its meaning depends on the host time zone. Convert it to a `DateTimeOffset` in the selector:

```csharp
DateTimePredicates.After<Order>("placedAfter", order => new DateTimeOffset(order.PlacedUtc, TimeSpan.Zero));
```

### Type tests

Each `TypePredicates` test has two overloads under one name. The `string?` overload tests text. The `object?` overload tests the runtime type and also reads text. A null selected value answers `Unknown`. All parsing uses the invariant culture.

| Test | True when |
| --- | --- |
| `IsGuid` | `Guid.TryParse` accepts the text (`N`, `D`, `B`, `P` and `X` forms). An `object?` selector also accepts a `Guid`. |
| `IsNumeric` | The text parses as a finite `double` with `NumberStyles.Float`: a sign, a decimal point and an exponent. A thousands separator, a currency symbol, `NaN` and `Infinity` are rejected. An `object?` selector also accepts a numeric-typed value. |
| `IsUrl` | The text is an absolute URI with the `http` or `https` scheme. An `object?` selector also accepts such a `Uri`. |
| `IsString` | The value is a string. With a `string?` selector every non-null value is a string. |
| `IsDateTimeOffset` | The text is ISO 8601 with seconds and an explicit offset or `Z`, such as `2026-10-04T12:00:00Z`. Text without an offset is rejected. An `object?` selector also accepts a `DateTimeOffset`. |

## 0 arguments, stateless lambda

```csharp
.Add(
    PredicateSchema.NoArguments("isBanned", "Is Banned", "Is the current customer's account banned?"),
    (customer, args, ct) => ValueTask.FromResult(customer.IsBanned ? TruthValue.True : TruthValue.False))
```

## 1 argument, stateless lambda

[Example 3](examples.md#3-named-arguments) shows `hasTopping(topping: "greenOlives")`. It has a single named `string` argument and no injected dependency.

## 0 arguments, class-based (DI)

[Example 1](examples.md#1-a-single-predicate) shows `LovesPineapple`. It is a class that implements `IPredicate<TContext>`. DI resolves it again from `IServiceProvider` on every evaluation. Use this shape when a scoped dependency, for example a `DbContext`, is involved, even when the predicate has no rule-authored parameter.

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
