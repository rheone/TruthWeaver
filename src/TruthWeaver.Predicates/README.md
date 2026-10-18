# TruthWeaver.Predicates

Ready-made predicates for strings, numbers, dates, collections, regular expressions and types.

## Purpose

Each factory is a static method that returns a predicate with its schema, so you register it without a predicate class.
The package needs only the abstractions. Each positive predicate has a `Not` twin, such as `IsNotEqualTo`. The reference
is in [Predicates](../../docs/predicates.md).

### Families

| Family | Class | Holds |
| --- | --- | --- |
| `Numeric` | `NumericPredicates` | Equality, order, range, membership and default tests of numbers. |
| `Scalar` | `ScalarPredicates` | Equality, membership, null and default tests of booleans, GUIDs and date-times. |
| `String` | `StringPredicates` | Equality, membership, empty and whitespace tests, `Contains`, `StartsWith` and `EndsWith`. |
| `DateTime` | `DateTimePredicates` | Order, range, clock, day of week, month and time window. |
| `Collection` | `CollectionPredicates` | Contains, subset, set equality, count and emptiness. |
| `Regex` | `RegexPredicates` | `IsMatch` and `IsNotMatch`. |
| `Type` | `TypePredicates` | Tests for GUID, string, number, date-time offset and URL. |

`SelectedValuePredicates` builds a predicate for your own value test from a selector function.

### Null behavior

A selector can return `null`. `NullBehavior` sets what the predicate answers. The host picks it when it registers the
predicate. A rule author cannot change it.

| Option | A null value gives |
| --- | --- |
| `Unknown` | `Unknown`, with no fault. This is the default. |
| `False` | `False` for a positive predicate, and `True` for its `Not` twin. |
| `Default` | No choice for this predicate. It follows the open `NullBehaviorScope` of its family, and gives `Unknown` when no scope applies. |

`NullBehaviorScope.Begin` sets a default for every predicate that you register without an explicit choice, for all
families or for one `PredicateFamily`.

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `IPredicate`.

## Key types

| Type | Role |
| --- | --- |
| `NumericPredicates`, `ScalarPredicates`, `StringPredicates`, `DateTimePredicates`, `CollectionPredicates`, `RegexPredicates`, `TypePredicates` | The factories of each family. |
| `SelectedValuePredicates` | Builds a predicate from a selector and a test. |
| `NullBehavior` | What a predicate answers for a null value. |
| `NullBehaviorScope` | Sets the default `NullBehavior` for a block of registrations. |
| `PredicateFamily` | Names a group of factories for `NullBehaviorScope`. |
