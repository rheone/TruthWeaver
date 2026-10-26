# Predicate types

How to write and register the predicates that a rule calls. The meaning of each operator is in the [Strong Kleene (K3) reference](strong-k3/README.md). Back to the [README](../README.md). The defaults that all built-in predicates share are in [Predicate conventions](predicate-conventions.md).

## Registration shapes

Every predicate is one of four registration shapes. The shapes mix freely in one `PredicateRegistryBuilder<TContext>.Build()`. Two independent axes describe a predicate. The first axis is how many rule-authored arguments it takes: zero, one, or several ("n"). The second axis is where its implementation comes from: a stateless lambda, or a class that DI resolves.

| Shape | Arguments | Implementation | When to use |
| --- | --- | --- | --- |
| Lambda, 0 args | none | stateless delegate | A simple stateless check with no rule-authored parameter. |
| Lambda, 1 arg | one | stateless delegate with a 1-argument schema | The common case. A stateless check that the rule text parameterizes, for example `hasTopping(topping: "greenOlives")`. |
| Class-based (DI), 0 args | none | `IPredicate<TContext>` | Needs a scoped or injected dependency but no rule-authored parameter. |
| Class-based (DI), n args | several | `IPredicate<TContext>` with a multi-argument schema | Needs rule-authored parameters and one or more injected dependencies. |

## Cost hint

`PredicateSchema.Cost` is an optional `double?`, set with `init` on any of the four shapes above. It is a relative number, not a unit of time: unset (the default) means "neutral", the same baseline as every other predicate that also leaves it unset. A predicate that is cheaper than typical (an in-memory check) can use a negative value; a predicate that is expensive (a network call, a database lookup, heavy computation) can use a positive one.

```csharp
builder.Add(
    PredicateSchema.NoArguments("fraudCheck", "Fraud Check", "Calls the fraud-scoring service.") with { Cost = 10.0 },
    FraudCheckPredicate.EvaluateAsync);
```

The only consumer is [`CompiledRule<TContext>.OptimizeOrder()`](rewriting-rules.md#optimize-evaluation-order): it stable-sorts the operands of `AND` and `OR`, cheapest first, so the evaluator's short-circuit reaches a deciding cheap predicate before an expensive one. Setting `Cost` on no predicate in a registry is exactly today's behavior - `OptimizeOrder()` then changes nothing, because every operand ties at the neutral baseline and a stable sort keeps ties in their written order. A host that has measured actual per-predicate cost can pass those values to `OptimizeOrder(OrderOptimizationOptions)` for one call instead of changing the registered hint.

## Ready-made predicates

Before you write a predicate by hand, check [`TruthWeaver.Predicates`](../src/TruthWeaver.Predicates). It ships generic factories that take a value selector:

- `StringPredicates`, `CollectionPredicates` and `RegexPredicates` cover string comparison, null, empty and white-space checks, set equality and regex matching. `RegexPredicates.IsMatch` and `IsNotMatch` take the optional rule argument `flags`, a string of the letters `i` (ignore case), `m`, `s`, `n` and `x`. A pattern can also start with an inline option such as `(?i)`.
- `NumericPredicates` covers `Int64` and `Decimal` selections. `ScalarPredicates` covers `Boolean`, `Guid` and `DateTimeOffset` selections. See [Scalar and numeric predicates](#scalar-and-numeric-predicates).
- `TypePredicates` covers the type tests `IsGuid`, `IsNumeric`, `IsUrl`, `IsString` and `IsDateTimeOffset`. Each has an `IsNot...` twin that is its Strong Kleene complement. See [Type tests](#type-tests).
- `SelectedValuePredicates` covers the externally selected value pattern (see [below](#n-arguments-class-based-externally-selected-value)) for a lookup client that is safe to share.

Every method on `StringPredicates` except two is ordinal-only and has a fixed behavior. The exceptions are `StringPredicates.IsEqualTo` and its twin `IsNotEqualTo`. Each is one predicate whose `ignoreCase` and `trim` arguments the rule sets. Both are `false` by default, so the comparison is case-sensitive and untrimmed; a rule sets `ignoreCase: true` to ignore case and `trim: true` to trim both sides. The comparison is always ordinal, so the predicate has no `culture` argument. A rule that passes a `culture` argument (even `culture: ""`) fails to compile with an `UnknownArgument` diagnostic that tells the author to remove it. Use `IsEqualTo` or `IsNotEqualTo` when a rule author needs this flexibility. Otherwise register one fixed-behavior predicate for each name.

### Null selected values

A null selected value never faults. A member cannot evaluate a missing value, so every member with an optional `nullBehavior` parameter answers a null selected value with `Unknown` by default (`NullBehavior.Unknown`). `NOT hasCrust(crust: "thin")` stays `Unknown` for an order with no crust, and `Decision.IsSatisfied` stays fail-closed.

The host can choose `NullBehavior.False` for each predicate at registration, or set a default once with `NullBehaviorScope`. Every factory call made inside `using (NullBehaviorScope.Begin(NullBehavior.False)) { ... }` that passes no `nullBehavior` follows the scope, and `NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.False)` does the same for one family. A `nullBehavior` argument on the call wins over a scope, and a family scope wins over an all-families scope. A positive member then answers `False` for a null selected value. The exception is `CollectionPredicates.SetEquals`. Under `NullBehavior.False` it reads a null collection as an empty collection, so it answers `True` when the argument array is empty.

A `NotX` twin is the Strong Kleene complement of its positive member for every selected value, a null one included: `True` becomes `False`, `False` becomes `True` and `Unknown` stays `Unknown`. A twin has the same default as its positive member, so both members of a pair registered with the defaults answer `Unknown` for a null selected value. Under `NullBehavior.False` a twin answers `True` for a null selected value, and `NotSetEquals` answers the complement of `SetEquals`. Register both members of a pair with the same setting.

The `StringPredicates` and `RegexPredicates` twins are:

| Member | Class | Meaning |
| --- | --- | --- |
| `IsNotEqualTo` | `StringPredicates` | Twin of `IsEqualTo`. It takes the same optional `ignoreCase` and `trim` arguments. |
| `NotStartsWith` | `StringPredicates` | Twin of `StartsWith` |
| `NotEndsWith` | `StringPredicates` | Twin of `EndsWith` |
| `NotContains` | `StringPredicates` | Twin of `Contains` |
| `IsIn` | `StringPredicates` | The selected string is one of the strings in a `StringArray` argument `values` (ordinal, case-sensitive). The selector returns one string, not a collection. |
| `IsNotIn` | `StringPredicates` | Twin of `IsIn` |
| `IsMatch` | `RegexPredicates` | The selected string matches the `pattern` argument. The optional `flags` argument sets options: `i` (ignore case, `RegexOptions.IgnoreCase` with `RegexOptions.CultureInvariant`), `m`, `s`, `n` and `x`. An invalid literal pattern or flags string is a compile error (`TRE0026`). An invalid value from a data source faults to `Unknown` with a `Fault`. |
| `IsNotMatch` | `RegexPredicates` | Twin of `IsMatch`. It takes the same optional `flags` argument. |
| `IsEmpty` | `StringPredicates` | A non-null empty string. A null selected value is missing, not empty. |
| `IsNotEmpty` | `StringPredicates` | Twin of `IsEmpty` |

`IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace` and `IsNotNullOrWhiteSpace` (all in `StringPredicates`) have no option. They are null tests and always return a definite answer: a null selected value is `True` for `IsNullOrEmpty` and `IsNullOrWhiteSpace`, and `False` for their complements. White space follows `char.IsWhiteSpace`.

```csharp
StringPredicates.IsEqualTo<PizzaOrder>(
    "hasCrust", order => order.Crust, "Has Crust", argumentName: "crust",
    nullBehavior: NullBehavior.False);
```

### Scalar and numeric predicates

`NumericPredicates` and `ScalarPredicates` take a nullable selector (`Func<TContext, long?>`, `Func<TContext, decimal?>`, `Func<TContext, bool?>`, `Func<TContext, Guid?>` or `Func<TContext, DateTimeOffset?>`). The selector type picks the overload and the argument kind. Each member has a `NotX` twin that is the Strong Kleene complement.

| Member | Twin | `NumericPredicates` (`Int64`, `Decimal`) | `ScalarPredicates` (`Boolean`, `Guid`, `DateTimeOffset`) |
| --- | --- | --- | --- |
| `IsEqualTo` | `IsNotEqualTo` | yes | yes |
| `IsLessThan` | `IsNotLessThan` | yes | not defined |
| `IsGreaterThan` | `IsNotGreaterThan` | yes | not defined |
| `IsLessThanOrEqualTo` | `IsNotLessThanOrEqualTo` | yes | not defined |
| `IsGreaterThanOrEqualTo` | `IsNotGreaterThanOrEqualTo` | yes | not defined |
| `IsBetween` | `IsNotBetween` | yes | not defined |
| `IsIn` | `IsNotIn` | yes | yes |
| `IsNull` | `IsNotNull` | yes | yes |
| `IsDefault` | `IsNotDefault` | yes | yes |

- Ordering and ranges have no meaning for `Boolean` and `Guid`, so they are not defined. For `DateTimeOffset`, ordering and ranges belong to the date-time predicates.
- `IsBetween` is inclusive on both bounds and `IsNotBetween` is its exact complement. Equal bounds are a single-point range. Reversed bounds (`lower` greater than `upper`) are an authoring error, and the bounds are never swapped:
  - When both bounds are literals, `Compile` reports a `TRE0026` error at the predicate call and returns no rule. The suggestion is to swap the bounds.
  - When a bound is not a literal, for example a value [read from a data source](#arguments-read-from-a-data-source), the compiler does not check it. A reversed value makes the predicate throw an `ArgumentException` at evaluation time, even for a null selection. The evaluator records a `Fault` and answers `Unknown`.
- `IsIn` and `IsNotIn` test one scalar value against a literal candidate array. A candidate array has the same kind as the selector.
- No value is promoted between kinds. A `long?` selector takes `Int64` literals. A `decimal?` selector takes `Decimal` literals, and a whole number such as `5` is a valid `Decimal` literal. To compare an integer value with a decimal literal, widen it in the selector: `c => (decimal?)c.Count`. Decimal values compare by value, so `1.0` equals `1.00`.
- `DateTimeOffset` values compare by instant, so the same instant in two offsets is equal.
- A null selected value answers as [Null selected values](#null-selected-values) describes. `IsNull` and `IsNotNull` are definite and have no option. `IsDefault` tests `default(T)`: `0`, `false`, `Guid.Empty` or the default `DateTimeOffset`. A null selection is a missing value, not a default.

```csharp
NumericPredicates.IsBetween<Order>("quantityInRange", order => order.Quantity, "Quantity In Range");
```

### Collection predicates

`CollectionPredicates` selects an `IReadOnlyCollection<string>?`. Comparison is ordinal and case-sensitive (see [Predicate conventions](predicate-conventions.md)). Every predicate has a `NotX` twin that is its Strong Kleene complement: `True` becomes `False`, `False` becomes `True`, and `Unknown` stays `Unknown`.

| Predicate | Twin | Argument | `True` when |
| --- | --- | --- | --- |
| `IsEmpty` | `IsNotEmpty` | none | The collection has no elements. |
| `IsNullOrEmpty` | `IsNotNullOrEmpty` | none | The collection is null or has no elements. A null test: it never answers `Unknown`. |
| `SetEquals` | `NotSetEquals` | `values` (`StringArray`) | The collection and the array hold the same set of strings. |
| `Contains` | `NotContains` | `value` (`String`) | The collection contains the value. |
| `ContainsAny` | `NotContainsAny` | `values` (`StringArray`) | At least one element is in the array. |
| `ContainsAll` | `NotContainsAll` | `values` (`StringArray`) | Every string in the array is an element. |
| `IsSubsetOf` | `IsNotSubsetOf` | `values` (`StringArray`) | Every element is in the array. |
| `HasCountEqualTo` | `NotHasCountEqualTo` | `count` (`Int64`) | The element count equals `count`. |
| `HasCountLessThan` | `NotHasCountLessThan` | `count` (`Int64`) | The element count is less than `count`. |
| `HasCountGreaterThan` | `NotHasCountGreaterThan` | `count` (`Int64`) | The element count is greater than `count`. |
| `HasCountLessThanOrEqualTo` | `NotHasCountLessThanOrEqualTo` | `count` (`Int64`) | The element count is at most `count`. |
| `HasCountGreaterThanOrEqualTo` | `NotHasCountGreaterThanOrEqualTo` | `count` (`Int64`) | The element count is at least `count`. |

To test one string against several candidates, use `StringPredicates.IsIn` and `StringPredicates.IsNotIn`.

Every predicate in this table except `IsNullOrEmpty` and `IsNotNullOrEmpty`, `IsEmpty` and `IsNotEmpty` included, answers a null selected value as [Null selected values](#null-selected-values) describes. The two null tests have no option: a null collection is `True` for `IsNullOrEmpty` and `False` for `IsNotNullOrEmpty`. A null collection is a missing value, not an empty one.

### Date and time predicates

`DateTimePredicates` selects a `DateTimeOffset?` and takes `DateTimeOffset` literal arguments. A date-time literal must end in `Z` or carry an offset such as `+02:00`. Text such as `"2026-01-01"` or `"2026-01-01T09:00"` is a compile error, so a stored rule means the same instant on every host. Values compare by instant, so the same moment with a different offset is equal. `IsAfter` and `IsBefore` are strict, and `IsInTimeWindow` is half-open by default (see [Predicate conventions](predicate-conventions.md#ranges-and-windows)).

| Predicate | Twin | Arguments | `True` when |
| --- | --- | --- | --- |
| `IsAfter` | `IsNotAfter` | `value` | The selected instant is later than `value`. An equal instant is `False`. |
| `IsBefore` | `IsNotBefore` | `value` | The selected instant is earlier than `value`. An equal instant is `False`. |
| `IsBetween` | `IsNotBetween` | `lower`, `upper` | `lower <= value <= upper`. Both bounds are inclusive. `IsNotBetween` is the exact complement. |

`IsAfterNow` and `IsBeforeNow` compare the selected instant with the current time. They take a required `TimeProvider` at registration and no rule-text arguments. There is no ambient default clock.

| Predicate | Twin | `True` when |
| --- | --- | --- |
| `IsAfterNow` | `IsNotAfterNow` | The selected instant is later than now. An instant equal to now is `False`. |
| `IsBeforeNow` | `IsNotBeforeNow` | The selected instant is earlier than now. An instant equal to now is `False`. |

An instant equal to now makes `IsAfterNow` and `IsBeforeNow` both `False`. Neither is the complement of the other, so each has its own twin.

```csharp
DateTimePredicates.IsAfterNow<Order>("expiresAfterNow", order => order.ExpiresAt, TimeProvider.System);
```

The predicate reads the clock each time the engine evaluates it, never at registration. The engine evaluates one term once per `Evaluate` call, so a repeated term sees one instant. Two different terms each read the clock and can see different instants if the clock advances between them. To give every term one instant, register a `TimeProvider` that returns a fixed instant for each evaluation.

`IsOnDayOfWeek`, `IsInMonth` and `IsInTimeWindow` read the selected instant in the fixed offset that the `offset` argument gives: `"Z"` or `"+hh:mm"` / `"-hh:mm"`. Time zone names such as `Europe/Paris` are not accepted, and there are no daylight-saving rules. The argument names are fixed.

| Predicate | Twin | Arguments | `True` when |
| --- | --- | --- | --- |
| `IsOnDayOfWeek` | `IsNotOnDayOfWeek` | `days` (English day names), `offset` | The day of the week in `offset` is in `days`. |
| `IsInMonth` | `IsNotInMonth` | `months` (numbers 1 to 12), `offset` | The month in `offset` is in `months`. |
| `IsInTimeWindow` | `IsNotInTimeWindow` | `start`, `end` or `duration`, `includeStart`, `includeEnd`, `offset` | The time of day in `offset` is inside the window. By default the window is $[start, end)$. A start later than the end crosses midnight. |

```csharp
DateTimePredicates.IsInTimeWindow<Order>("placedInHours", order => order.PlacedAt);
```

A rule then calls `placedInHours(start: "09:00", end: "17:00", offset: "+01:00")`, or gives `duration: "PT8H"` in place of `end`. A literal argument that is not valid, a time zone name included, is a `TRE0026` compile error. The [date-time predicates reference](strong-k3/predicates/README.md#fixed-offsets) gives every argument rule.

Reversed bounds (`lower` later than `upper`) follow the reversed-bounds rule of the numeric range predicates in [Scalar and numeric predicates](#scalar-and-numeric-predicates).

A null selected value answers as [Null selected values](#null-selected-values) describes.

There is no `DateTime` literal kind and no `DateTime` overload. A `DateTime` can have an unspecified `Kind`, so its meaning depends on the host time zone. Convert it to a `DateTimeOffset` in the selector:

```csharp
DateTimePredicates.IsAfter<Order>("placedAfter", order => new DateTimeOffset(order.PlacedUtc, TimeSpan.Zero));
```

### Type tests

Each `TypePredicates` test has two overloads under one name. The `string?` overload tests text. The `object?` overload tests the runtime type and also reads text. A null selected value answers `Unknown` by default; a host can pass `nullBehavior: NullBehavior.False` to make it `False` (and the `IsNotX` twin `True`). All parsing uses the invariant culture.

| Test | True when |
| --- | --- |
| `IsGuid` | `Guid.TryParse` accepts the text (`N`, `D`, `B`, `P` and `X` forms). An `object?` selector also accepts a `Guid`. |
| `IsNumeric` | The text parses as a finite `double` with `NumberStyles.Float`: a sign, a decimal point and an exponent. A thousands separator, a currency symbol, `NaN` and `Infinity` are rejected. An `object?` selector also accepts a numeric-typed value. |
| `IsUrl` | The text is an absolute URI with the `http` or `https` scheme. An `object?` selector also accepts such a `Uri`. |
| `IsString` | The value is a string. With a `string?` selector every non-null value is a string. |
| `IsDateTimeOffset` | The text is ISO 8601 with seconds and an explicit offset or `Z`, such as `2026-10-04T12:00:00Z`. Text without an offset is rejected. An `object?` selector also accepts a `DateTimeOffset`. |

## Collections for quantifiers

`PredicateRegistryBuilder<TContext>.AddCollection<TItem>` registers a named collection that `FORALL` and `EXISTS` can range over. It takes the collection name, a selector from `TContext` to `IEnumerable<TItem>?`, and a delegate that registers the element predicates on a nested builder of type `PredicateRegistryBuilder<Element<TContext, TItem>>`. An element predicate reads `Element<TOuter, TItem>`: `Outer` is the outer context, `Item` is the element and `Index` is its position. A nested collection registers on the element builder. A collection name is unique ignoring case, and it is a namespace of its own, separate from predicate names. It must also be a valid DSL identifier and not a reserved word, the same rule a predicate name follows (see [Grammar](rule-text.md#grammar)); registering a collection with a name that breaks either rule throws an `ArgumentException`.

```csharp
builder.AddCollection<OrderLine>(
    "lines",
    order => order.Lines,
    elements =>
        elements.Add(
            inStockSchema,
            (element, _, _) => ValueTask.FromResult(element.Item.InStock ? TruthValue.True : TruthValue.False)
        )
);
```

An optional fourth parameter, `elementSource`, projects one element to an `IDataSource`. A quantifier body over that collection can then read the element untyped with `from(variable, "...")`, as well as with the typed element predicates. Without it, only the typed path works. The rule text is in [Quantifiers](rule-text.md#quantifiers), and the meaning is in the [Quantifiers reference](strong-k3/quantifiers/README.md).

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

### Checking argument values at compile time

The argument declarations check that each argument is present and of its kind. A rule between argument values, for example "`min` is not greater than `max`", goes in the optional `PredicateSchema.ArgumentValidator`. The compiler calls it once for each call of the predicate, after the argument check passes. It receives the literal arguments only. An argument read from a data source is absent, so the validator skips any check that needs it, and the predicate checks that value at evaluation. An optional argument with no default that the call omits is also absent. `PredicateArguments.IsDeferred(name)` is `true` for a data-source argument and `false` for an omitted one, so a validator can tell them apart. At evaluation `IsDeferred` is `false` for every name, because every data-source value is present. Each `PredicateArgumentProblem` that the validator returns becomes a `TRE0026` error at the call, and `Compile` returns no rule. The validator must be pure, thread-safe and must not throw.

```csharp
PredicateSchema schema = new(
    "toppingCountBetween",
    "Topping Count Between",
    "Does the order have between min and max toppings?",
    [
        new PredicateArgumentSchema("min", "The fewest toppings.", LiteralKind.Int64),
        new PredicateArgumentSchema("max", "The most toppings.", LiteralKind.Int64),
    ])
{
    ArgumentValidator = args =>
        args.TryGetInt64("min", out long min) && args.TryGetInt64("max", out long max) && min > max
            ? [new PredicateArgumentProblem($"'min' ({min}) is greater than 'max' ({max}).", "'min' less than or equal to 'max'", $"'min' is {min} and 'max' is {max}", "Swap the values of 'min' and 'max'.")]
            : [],
};
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
