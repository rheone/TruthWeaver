# Selected value Create

`SelectedValuePredicates.Create` is a factory that builds your own predicate. The predicate reads a value from outside the rule, for example a feature-flag service, and turns that value into `True`, `False` or `Unknown`. It is not a fixed predicate, so it has no twin and no `nullBehavior` option. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `SelectedValuePredicates.Create`
- Default label: none. The host passes `name`, `label` and `description` when it registers the predicate.
- Arguments: the host declares them. A predicate can have none.
- Rule text: `isFeatureEnabled(flagKey: "new-checkout")`. The host chooses the name `isFeatureEnabled` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `SelectedValuePredicates`
- Twin: none. Build the opposite with `NOT`, or write a second predicate.

## Selector and kinds

The factory has two overloads. Both take the same `name`, `label` and `description` and the `arguments` declarations.

| Overload | Select delegate | Test delegate | Use |
| --- | --- | --- | --- |
| Select and test | `Func<TContext, PredicateArguments, CancellationToken, ValueTask<TSelected>>` | `Func<TSelected, TruthValue>` | The select delegate reads a value of any type. The test delegate turns it into an answer. |
| Select only | `Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>>` | none | The select delegate returns the answer. |

The select delegate receives the application context, the rule arguments of the term and a cancellation token.

## Arguments

The host declares the arguments of the predicate with `PredicateArgumentSchema` values. The `arguments` parameter is optional.

| Field | Meaning |
| --- | --- |
| `Name` | The argument name in rule text. |
| `Description` | A text for people who author rules. |
| `Type` | The literal kind. See [Argument kinds](README.md#argument-kinds). |
| `Required` | `true` by default. A rule that omits a required argument does not compile. |
| `Default` | The value when the rule omits an optional argument. |

The select delegate reads each argument from `PredicateArguments`, for example `args.GetString("flagKey")`.

## Definition

The answer of the predicate is the answer that the delegates give.

- Select and test: the answer is `test(selected)`. The test delegate returns `Unknown` when the selected value cannot decide the answer.
- Select only: the answer is the value that the select delegate returns.

The factory adds no null handling. The delegates decide how a missing value answers.

## Answers

The tables show two predicates that the host registers over a context with a `Balance` of type `decimal?`.

The first predicate is `balanceCheck`. It uses the select and test overload. The select delegate returns `Balance`. The test delegate returns `Unknown` for `null`, `True` for 100 or more, and `False` otherwise.

| Selected value | Answer |
| --- | --- |
| `150` | `True` |
| `100` | `True` |
| `99` | `False` |
| `null` | `Unknown` |

The second predicate is `isFeatureEnabled(flagKey: "...")`. It uses the select only overload. The select delegate looks up the key in a table that holds `"new-checkout"` as `True` and `"beta"` as `False`. It returns `Unknown` for a key that is not in the table.

| Rule | Answer |
| --- | --- |
| `isFeatureEnabled(flagKey: "new-checkout")` | `True` |
| `isFeatureEnabled(flagKey: "beta")` | `False` |
| `isFeatureEnabled(flagKey: "other")` | `Unknown` |

## Null selected value

The factory has no `nullBehavior` option. The test delegate or the select delegate chooses the answer for a missing value. `Unknown` keeps `Decision.IsSatisfied` fail-closed. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `balanceCheck` | `Balance = 150` | `True` | The test delegate accepts 150. |
| `balanceCheck` | `Balance = 99` | `False` | The test delegate rejects 99. |
| `NOT balanceCheck` | `Balance = null` | `Unknown` | The test delegate answers `Unknown` for `null`, and `NOT` keeps `Unknown`. |
| `isFeatureEnabled(flagKey: "other")` | none | `Unknown` | The select delegate has no flag with this key. |
| `lookupFails OR balanceCheck` | `Balance = 150` | `True` | `lookupFails` is a predicate whose select delegate throws. It is `Unknown` and records one fault. `balanceCheck` is `True`, so the `OR` is `True`. |

## Edge cases

- An exception from the select delegate or the test delegate reaches the evaluator unchanged. The evaluator gives the term `Unknown` and records a fault. See [evaluation](../specification/evaluation.md#predicates-and-faults).
- The host captures the delegates once, when it registers the predicate. Use a long-lived, thread-safe source in the delegates, for example a cached flag reader.
- A source that must be created again for each evaluation, for example a scoped database context, does not fit this factory. Implement `IPredicate<TContext>` in a class for that case.
- A rule that omits a required argument does not compile.

## Related predicates

- The fixed predicates in this index select a value with a selector that the host supplies. Use one of them when a comparison fits, for example [numeric Equal](numeric-equal.md) or [string Equals](string-equals.md).
