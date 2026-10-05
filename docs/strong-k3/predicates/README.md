# Predicates

A predicate is a registered, reusable function. A term is a predicate bound to arguments. Both are defined in the [terminology](../specification/terminology.md#engine-terms). This index lists the built-in predicates that have a document. Back to the [reference](../README.md).

## How a predicate answers

A built-in predicate reads one value from the application context with a selector that the host supplies when it registers the predicate. The predicate compares that selected value with the arguments of the term. The answer is `True`, `False` or `Unknown`.

- A rule calls the predicate by the name that the host registered. The arguments are `name: literal` pairs, as in `quantityBetween(lower: 10, upper: 20)`.
- A non-null selected value always gives `True` or `False`.
- A predicate that cannot answer is a fault. A fault gives `Unknown` and is recorded on the decision. See [evaluation](../specification/evaluation.md#predicates-and-faults).

## Null selected values

A null selected value is a missing value. It is not a fault.

| Predicate | `NullBehavior.Unknown` (default) | `NullBehavior.False` |
| --- | --- | --- |
| Positive predicate | `Unknown` | `False` |
| Its twin | `Unknown` | `True` |
| `IsNull` | `True` | `True`. A null test has no option. |
| `IsNotNull` | `False` | `False`. A null test has no option. |

- The host chooses the behavior once, when it registers the predicate. A rule cannot change it.
- `Unknown` keeps `Decision.IsSatisfied` fail-closed: a rule that depends on a missing value is not satisfied.
- `NOT` of a predicate that answers `Unknown` stays `Unknown`.
- A host registers both predicates of a pair with the same `NullBehavior`.
- A null value is not a default value. `IsDefault` answers per `NullBehavior` for a null selected value.

## NotX twins

Every predicate in these families has a twin. The twin is the Strong Kleene complement of the predicate for every selected value, a null one included: `True` becomes `False`, `False` becomes `True` and `Unknown` stays `Unknown`. A twin has the same default `NullBehavior` as its predicate.

| Predicate | Twin |
| --- | --- |
| `Equal` | `NotEqual` |
| `LessThan` | `GreaterThanOrEqual` |
| `GreaterThan` | `LessThanOrEqual` |
| `Between` | `Outside` |
| `In` | `NotIn` |
| `IsNull` | `IsNotNull` |
| `IsDefault` | `IsNotDefault` |
| `Equals` | `NotEqual` |
| `EqualsIgnoreCase` | `NotEqualsIgnoreCase` |
| `EqualsConfigurable` | `NotEqualsConfigurable` |
| `StartsWith` | `NotStartsWith` |
| `EndsWith` | `NotEndsWith` |
| `Contains` | `NotContains` |
| `IsEmpty` | `IsNotEmpty` |
| `IsNullOrEmpty` | `IsNotNullOrEmpty` |
| `IsNullOrWhiteSpace` | `IsNotNullOrWhiteSpace` |
| `Matches` | `NotMatches` |

Each predicate and each twin has its own page.

## Argument kinds

The selector type fixes the kind of every argument. A rule literal of another kind is a compile error (see [diagnostics](../specification/diagnostics.md)).

| Kind | Rule-text literal | Example |
| --- | --- | --- |
| `Int64` | A whole number with no decimal point | `5` |
| `Decimal` | A number. A whole number is accepted. | `5`, `1.5` |
| `String` | A quoted string | `"Active"` |
| `Boolean` | `true` or `false` | `true` |
| `Guid` | A quoted string in GUID format | `"3f2504e0-4f89-11d3-9a0c-0305e82c3301"` |
| `DateTimeOffset` | A quoted ISO 8601 string with an offset | `"2026-01-01T00:00:00Z"` |
| An array kind | A list of literals of one kind | `[1, 2, 3]` |

- No value is promoted between kinds. A host that compares an integer value with a `Decimal` literal widens the value in the selector. The widening is exact.
- A selector over an `int` property binds to the `Int64` overload.
- `Decimal` values compare by value, so `1.0` equals `1.00`.
- `DateTimeOffset` values compare by instant, so the same instant in two offsets is equal.

## Numeric predicates

The `NumericPredicates` factories select an `Int64` (`long?`) or a `Decimal` (`decimal?`) value.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [Equal](numeric-equal.md) | [NotEqual](numeric-notequal.md) | `Equal` is `True` when the selected number equals the argument. |
| [NotEqual](numeric-notequal.md) | [Equal](numeric-equal.md) | `NotEqual` is `True` when the selected number differs from the argument. |
| [LessThan](numeric-lessthan.md) | [GreaterThanOrEqual](numeric-greaterthanorequal.md) | `LessThan` is `True` when the selected number is less than the argument. |
| [GreaterThanOrEqual](numeric-greaterthanorequal.md) | [LessThan](numeric-lessthan.md) | `GreaterThanOrEqual` is `True` when the selected number is greater than or equal to the argument. |
| [GreaterThan](numeric-greaterthan.md) | [LessThanOrEqual](numeric-lessthanorequal.md) | `GreaterThan` is `True` when the selected number is greater than the argument. |
| [LessThanOrEqual](numeric-lessthanorequal.md) | [GreaterThan](numeric-greaterthan.md) | `LessThanOrEqual` is `True` when the selected number is less than or equal to the argument. |
| [Between](numeric-between.md) | [Outside](numeric-outside.md) | `Between` is `True` when the selected number lies in a range. |
| [Outside](numeric-outside.md) | [Between](numeric-between.md) | `Outside` is `True` when the selected number lies outside a range. |
| [In](numeric-in.md) | [NotIn](numeric-notin.md) | `In` is `True` when the selected number is one of the candidates. |
| [NotIn](numeric-notin.md) | [In](numeric-in.md) | `NotIn` is `True` when the selected number is none of the candidates. |
| [IsNull](numeric-isnull.md) | [IsNotNull](numeric-isnotnull.md) | `IsNull` is `True` when the selector returns `null`. |
| [IsNotNull](numeric-isnotnull.md) | [IsNull](numeric-isnull.md) | `IsNotNull` is `True` when the selector returns a number. |
| [IsDefault](numeric-isdefault.md) | [IsNotDefault](numeric-isnotdefault.md) | `IsDefault` is `True` when the selected number is zero, the default value of the number type. |
| [IsNotDefault](numeric-isnotdefault.md) | [IsDefault](numeric-isdefault.md) | `IsNotDefault` is `True` when the selected number is not zero. |

## Scalar predicates

The `ScalarPredicates` factories select a `Boolean` (`bool?`), a `Guid` (`Guid?`) or a `DateTimeOffset` (`DateTimeOffset?`) value. Ordering and ranges are not defined for these kinds. The date-time predicates provide ordering and ranges for `DateTimeOffset`.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [Equal](scalar-equal.md) | [NotEqual](scalar-notequal.md) | `Equal` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` equals the argument. |
| [NotEqual](scalar-notequal.md) | [Equal](scalar-equal.md) | `NotEqual` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` differs from the argument. |
| [In](scalar-in.md) | [NotIn](scalar-notin.md) | `In` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is one of the candidates. |
| [NotIn](scalar-notin.md) | [In](scalar-in.md) | `NotIn` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is none of the candidates. |
| [IsNull](scalar-isnull.md) | [IsNotNull](scalar-isnotnull.md) | `IsNull` is `True` when the selector returns `null`. |
| [IsNotNull](scalar-isnotnull.md) | [IsNull](scalar-isnull.md) | `IsNotNull` is `True` when the selector returns a value. |
| [IsDefault](scalar-isdefault.md) | [IsNotDefault](scalar-isnotdefault.md) | `IsDefault` is `True` when the selected value is the default value of its type. |
| [IsNotDefault](scalar-isnotdefault.md) | [IsDefault](scalar-isdefault.md) | `IsNotDefault` is `True` when the selected value is not the default value of its type. |

## String predicates

The `StringPredicates` factories select a `string?` value. Every comparison is ordinal, so the culture of the host process has no effect. A string argument is a quoted string.

- `IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace` and `IsNotNullOrWhiteSpace` are null tests. They never answer `Unknown` and have no `nullBehavior` option.
- Every other string predicate answers per [Null selected values](#null-selected-values). This includes `IsEmpty` and `IsNotEmpty`, because a null string is a missing value and not an empty one.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [Equals](string-equals.md) | [NotEqual](string-notequal.md) | `Equals` is `True` when the selected string equals the argument. |
| [NotEqual](string-notequal.md) | [Equals](string-equals.md) | `NotEqual` is `True` when the selected string differs from the argument. |
| [EqualsIgnoreCase](string-equalsignorecase.md) | [NotEqualsIgnoreCase](string-notequalsignorecase.md) | `EqualsIgnoreCase` is `True` when the selected string equals the argument, ignoring case. |
| [NotEqualsIgnoreCase](string-notequalsignorecase.md) | [EqualsIgnoreCase](string-equalsignorecase.md) | `NotEqualsIgnoreCase` is `True` when the selected string differs from the argument, ignoring case. |
| [EqualsConfigurable](string-equalsconfigurable.md) | [NotEqualsConfigurable](string-notequalsconfigurable.md) | `EqualsConfigurable` is `True` when the selected string equals the argument. The rule chooses case handling and trimming. |
| [NotEqualsConfigurable](string-notequalsconfigurable.md) | [EqualsConfigurable](string-equalsconfigurable.md) | `NotEqualsConfigurable` is `True` when the selected string differs from the argument. The rule chooses case handling and trimming. |
| [StartsWith](string-startswith.md) | [NotStartsWith](string-notstartswith.md) | `StartsWith` is `True` when the selected string starts with the argument. |
| [NotStartsWith](string-notstartswith.md) | [StartsWith](string-startswith.md) | `NotStartsWith` is `True` when the selected string does not start with the argument. |
| [EndsWith](string-endswith.md) | [NotEndsWith](string-notendswith.md) | `EndsWith` is `True` when the selected string ends with the argument. |
| [NotEndsWith](string-notendswith.md) | [EndsWith](string-endswith.md) | `NotEndsWith` is `True` when the selected string does not end with the argument. |
| [Contains](string-contains.md) | [NotContains](string-notcontains.md) | `Contains` is `True` when the selected string contains the argument as a substring. |
| [NotContains](string-notcontains.md) | [Contains](string-contains.md) | `NotContains` is `True` when the selected string does not contain the argument as a substring. |
| [IsEmpty](string-isempty.md) | [IsNotEmpty](string-isnotempty.md) | `IsEmpty` is `True` when the selected string is the empty string. |
| [IsNotEmpty](string-isnotempty.md) | [IsEmpty](string-isempty.md) | `IsNotEmpty` is `True` when the selected string has at least one character. |
| [IsNullOrEmpty](string-isnullorempty.md) | [IsNotNullOrEmpty](string-isnotnullorempty.md) | `IsNullOrEmpty` is `True` when the selected string is `null` or empty. |
| [IsNotNullOrEmpty](string-isnotnullorempty.md) | [IsNullOrEmpty](string-isnullorempty.md) | `IsNotNullOrEmpty` is `True` when the selected string is not `null` and not empty. |
| [IsNullOrWhiteSpace](string-isnullorwhitespace.md) | [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) | `IsNullOrWhiteSpace` is `True` when the selected string is `null`, empty or only whitespace. |
| [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) | [IsNullOrWhiteSpace](string-isnullorwhitespace.md) | `IsNotNullOrWhiteSpace` is `True` when the selected string has a non-whitespace character. |

## Regex predicates

The `RegexPredicates` factories select a `string?` value and take a regular expression as the argument. A null selected value answers per [Null selected values](#null-selected-values). An invalid pattern is a fault.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [Matches](regex-matches.md) | [NotMatches](regex-notmatches.md) | `Matches` is `True` when the selected string matches the regular expression. |
| [NotMatches](regex-notmatches.md) | [Matches](regex-matches.md) | `NotMatches` is `True` when the selected string does not match the regular expression. |

## How a predicate page is organized

A predicate page has these sections, in this order: Name, Classification, Selector and kinds, Arguments, Definition, Answers, Null selected value and Examples. Reversed bounds, Edge cases and Related predicates appear where they apply. A page is named `<family>-<factory>.md` in lower case.
