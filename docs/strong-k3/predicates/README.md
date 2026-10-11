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

- The host chooses the behavior when it registers the predicate. A rule cannot change it.
- A host can choose it once for every registration, or for one family, with `NullBehaviorScope`: `using (NullBehaviorScope.Begin(NullBehavior.False)) { ... }` sets the default for the factory calls made inside the block, and `NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.False)` sets it for one family. A `nullBehavior` argument on a factory call wins over a scope, and a family scope wins over an all-families scope. With no scope the default is `Unknown`.
- `Unknown` keeps `Decision.IsSatisfied` fail-closed: a rule that depends on a missing value is not satisfied.
- `NOT` of a predicate that answers `Unknown` stays `Unknown`.
- A host registers both predicates of a pair with the same `NullBehavior`.
- A missing value is `Unknown` unless the predicate is a null test. This includes the collection `IsEmpty` and `IsNotEmpty`: a null collection is missing, not empty.
- A null value is not a default value. `IsDefault` answers per `NullBehavior` for a null selected value.

### When a predicate answers Unknown

`Unknown` is the default for a missing value because it is the only answer that does not guess. `NOT` of it stays `Unknown`, so a rule that depends on a missing value is not satisfied: `Decision.IsSatisfied` is fail-closed. `NullBehavior.False` trades that safety for a definite answer, and `NOT` then turns the missing value into `True`. With a null age, `NOT ageAbove(value: 18)` is `Unknown` by default and `True` when the host chooses `NullBehavior.False`.

| Predicate group | Null selected value | Fault (selector or predicate throws, times out, or an argument value is not valid) | Data-source failure (argument value missing, ambiguous, wrong kind, source error or unsupplied source) |
| --- | --- | --- | --- |
| Comparison, range, membership and default tests: numeric, scalar, string, date-time, regex, type | `Unknown`, or `False` under `NullBehavior.False` | `Unknown` plus a fault | `Unknown` plus a fault |
| Collection tests, including `IsEmpty` and `IsNotEmpty` | `Unknown`, or `False` under `NullBehavior.False` (`SetEquals` reads a null collection as the empty set) | `Unknown` plus a fault | `Unknown` plus a fault |
| Clock and calendar tests: `IsAfterNow`, `IsOnDayOfWeek`, `IsInMonth`, `IsInTimeWindow` | `Unknown`, or `False` under `NullBehavior.False` | `Unknown` plus a fault | `Unknown` plus a fault |
| Null tests: `IsNull`, `IsNotNull`, `IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace` | Never `Unknown`: always `True` or `False` | `Unknown` plus a fault | `Unknown` plus a fault |
| A `NotX` twin | The complement of its positive predicate: `Unknown`, or `True` under `NullBehavior.False` | `Unknown` plus a fault | `Unknown` plus a fault |

A fault is never changed by `NullBehavior` or by a scope. A data-source failure applies only to a term that takes an argument from a data source.

## NotX twins

Every predicate in these families has a twin. The twin is the Strong Kleene complement of the predicate for every selected value, a null one included: `True` becomes `False`, `False` becomes `True` and `Unknown` stays `Unknown`. A twin has the same default `NullBehavior` as its predicate.

| Predicate | Twin |
| --- | --- |
| `IsEqualTo` | `IsNotEqualTo` |
| `IsLessThan` | `IsNotLessThan` |
| `IsGreaterThan` | `IsNotGreaterThan` |
| `IsLessThanOrEqualTo` | `IsNotLessThanOrEqualTo` |
| `IsGreaterThanOrEqualTo` | `IsNotGreaterThanOrEqualTo` |
| `IsBetween` | `IsNotBetween` |
| `IsIn` | `IsNotIn` |
| `IsNull` | `IsNotNull` |
| `IsDefault` | `IsNotDefault` |
| `StartsWith` | `NotStartsWith` |
| `EndsWith` | `NotEndsWith` |
| `Contains` | `NotContains` |
| `IsEmpty` | `IsNotEmpty` |
| `IsNullOrEmpty` | `IsNotNullOrEmpty` |
| `IsNullOrWhiteSpace` | `IsNotNullOrWhiteSpace` |
| `IsMatch` | `IsNotMatch` |
| `ContainsAny` | `NotContainsAny` |
| `ContainsAll` | `NotContainsAll` |
| `IsSubsetOf` | `IsNotSubsetOf` |
| `SetEquals` | `NotSetEquals` |
| `HasCountEqualTo` | `NotHasCountEqualTo` |
| `HasCountLessThan` | `NotHasCountLessThan` |
| `HasCountGreaterThan` | `NotHasCountGreaterThan` |
| `HasCountLessThanOrEqualTo` | `NotHasCountLessThanOrEqualTo` |
| `HasCountGreaterThanOrEqualTo` | `NotHasCountGreaterThanOrEqualTo` |
| `IsAfter` | `IsNotAfter` |
| `IsBefore` | `IsNotBefore` |
| `IsAfterNow` | `IsNotAfterNow` |
| `IsBeforeNow` | `IsNotBeforeNow` |
| `IsOnDayOfWeek` | `IsNotOnDayOfWeek` |
| `IsInMonth` | `IsNotInMonth` |
| `IsInTimeWindow` | `IsNotInTimeWindow` |
| `IsGuid` | `IsNotGuid` |
| `IsNumeric` | `IsNotNumeric` |
| `IsUrl` | `IsNotUrl` |
| `IsString` | `IsNotString` |
| `IsDateTimeOffset` | `IsNotDateTimeOffset` |

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
| `DateTimeOffset` | A quoted ISO 8601 string that ends in `Z` or carries an offset such as `+02:00`. Text without an offset is a compile error. | `"2026-01-01T00:00:00Z"` |
| An array kind | A list of literals of one kind | `[1, 2, 3]` |

- No value is promoted between kinds. A host that compares an integer value with a `Decimal` literal widens the value in the selector. The widening is exact.
- A selector over an `int` property binds to the `Int64` overload.
- `Decimal` values compare by value, so `1.0` equals `1.00`.
- `DateTimeOffset` values compare by instant, so the same instant in two offsets is equal.

## Numeric predicates

The `NumericPredicates` factories select an `Int64` (`long?`) or a `Decimal` (`decimal?`) value. `IsBetween` and `IsNotBetween` include both bounds. `IsLessThan` and `IsGreaterThan` are strict.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsEqualTo](numeric-isequalto.md) | [IsNotEqualTo](numeric-isnotequalto.md) | `IsEqualTo` is `True` when the selected number equals the argument. |
| [IsNotEqualTo](numeric-isnotequalto.md) | [IsEqualTo](numeric-isequalto.md) | `IsNotEqualTo` is `True` when the selected number differs from the argument. |
| [IsLessThan](numeric-islessthan.md) | [IsNotLessThan](numeric-isnotlessthan.md) | `IsLessThan` is `True` when the selected number is less than the argument. |
| [IsNotLessThan](numeric-isnotlessthan.md) | [IsLessThan](numeric-islessthan.md) | `IsNotLessThan` is `True` when the selected number is not less than the argument. |
| [IsGreaterThan](numeric-isgreaterthan.md) | [IsNotGreaterThan](numeric-isnotgreaterthan.md) | `IsGreaterThan` is `True` when the selected number is greater than the argument. |
| [IsNotGreaterThan](numeric-isnotgreaterthan.md) | [IsGreaterThan](numeric-isgreaterthan.md) | `IsNotGreaterThan` is `True` when the selected number is not greater than the argument. |
| [IsLessThanOrEqualTo](numeric-islessthanorequalto.md) | [IsNotLessThanOrEqualTo](numeric-isnotlessthanorequalto.md) | `IsLessThanOrEqualTo` is `True` when the selected number is less than or equal to the argument. |
| [IsNotLessThanOrEqualTo](numeric-isnotlessthanorequalto.md) | [IsLessThanOrEqualTo](numeric-islessthanorequalto.md) | `IsNotLessThanOrEqualTo` is `True` when the selected number is not less than or equal to the argument. |
| [IsGreaterThanOrEqualTo](numeric-isgreaterthanorequalto.md) | [IsNotGreaterThanOrEqualTo](numeric-isnotgreaterthanorequalto.md) | `IsGreaterThanOrEqualTo` is `True` when the selected number is greater than or equal to the argument. |
| [IsNotGreaterThanOrEqualTo](numeric-isnotgreaterthanorequalto.md) | [IsGreaterThanOrEqualTo](numeric-isgreaterthanorequalto.md) | `IsNotGreaterThanOrEqualTo` is `True` when the selected number is not greater than or equal to the argument. |
| [IsBetween](numeric-isbetween.md) | [IsNotBetween](numeric-isnotbetween.md) | `IsBetween` is `True` when the selected number lies in a range. |
| [IsNotBetween](numeric-isnotbetween.md) | [IsBetween](numeric-isbetween.md) | `IsNotBetween` is `True` when the selected number lies outside a range. |
| [IsIn](numeric-isin.md) | [IsNotIn](numeric-isnotin.md) | `IsIn` is `True` when the selected number is one of the candidates. |
| [IsNotIn](numeric-isnotin.md) | [IsIn](numeric-isin.md) | `IsNotIn` is `True` when the selected number is none of the candidates. |
| [IsNull](numeric-isnull.md) | [IsNotNull](numeric-isnotnull.md) | `IsNull` is `True` when the selector returns `null`. |
| [IsNotNull](numeric-isnotnull.md) | [IsNull](numeric-isnull.md) | `IsNotNull` is `True` when the selector returns a number. |
| [IsDefault](numeric-isdefault.md) | [IsNotDefault](numeric-isnotdefault.md) | `IsDefault` is `True` when the selected number is zero, the default value of the number type. |
| [IsNotDefault](numeric-isnotdefault.md) | [IsDefault](numeric-isdefault.md) | `IsNotDefault` is `True` when the selected number is not zero. |

## Scalar predicates

The `ScalarPredicates` factories select a `Boolean` (`bool?`), a `Guid` (`Guid?`) or a `DateTimeOffset` (`DateTimeOffset?`) value. Ordering and ranges are not defined for these kinds. The date-time predicates provide ordering and ranges for `DateTimeOffset`.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsEqualTo](scalar-isequalto.md) | [IsNotEqualTo](scalar-isnotequalto.md) | `IsEqualTo` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` equals the argument. |
| [IsNotEqualTo](scalar-isnotequalto.md) | [IsEqualTo](scalar-isequalto.md) | `IsNotEqualTo` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` differs from the argument. |
| [IsIn](scalar-isin.md) | [IsNotIn](scalar-isnotin.md) | `IsIn` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is one of the candidates. |
| [IsNotIn](scalar-isnotin.md) | [IsIn](scalar-isin.md) | `IsNotIn` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is none of the candidates. |
| [IsNull](scalar-isnull.md) | [IsNotNull](scalar-isnotnull.md) | `IsNull` is `True` when the selector returns `null`. |
| [IsNotNull](scalar-isnotnull.md) | [IsNull](scalar-isnull.md) | `IsNotNull` is `True` when the selector returns a value. |
| [IsDefault](scalar-isdefault.md) | [IsNotDefault](scalar-isnotdefault.md) | `IsDefault` is `True` when the selected value is the default value of its type. |
| [IsNotDefault](scalar-isnotdefault.md) | [IsDefault](scalar-isdefault.md) | `IsNotDefault` is `True` when the selected value is not the default value of its type. |

## String predicates

The `StringPredicates` factories select a `string?` value. Every comparison is ordinal. It is case-sensitive, and nothing is trimmed, unless the rule sets `ignoreCase` or `trim` on `IsEqualTo` or `IsNotEqualTo`. The culture of the host process has no effect. A string argument is a quoted string.

The comparison does not normalize Unicode. A precomposed character such as `é` (U+00E9) and the same character written as `e` plus a combining accent (U+0065 U+0301) are different strings, so they compare unequal. The host must normalize the selected value and the rule argument to the same form, NFC or NFKC, before the predicate sees them.

- `IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace` and `IsNotNullOrWhiteSpace` are null tests. They never answer `Unknown` and have no `nullBehavior` option.
- Every other string predicate answers per [Null selected values](#null-selected-values). This includes `IsEmpty` and `IsNotEmpty`, because a null string is a missing value and not an empty one.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsEqualTo](string-isequalto.md) | [IsNotEqualTo](string-isnotequalto.md) | `IsEqualTo` is `True` when the selected string equals the argument. The optional arguments `ignoreCase` and `trim` (both `false` by default) change the comparison. |
| [IsNotEqualTo](string-isnotequalto.md) | [IsEqualTo](string-isequalto.md) | `IsNotEqualTo` is `True` when the selected string differs from the argument. It takes the same optional arguments. |
| [StartsWith](string-startswith.md) | [NotStartsWith](string-notstartswith.md) | `StartsWith` is `True` when the selected string starts with the argument. |
| [NotStartsWith](string-notstartswith.md) | [StartsWith](string-startswith.md) | `NotStartsWith` is `True` when the selected string does not start with the argument. |
| [EndsWith](string-endswith.md) | [NotEndsWith](string-notendswith.md) | `EndsWith` is `True` when the selected string ends with the argument. |
| [NotEndsWith](string-notendswith.md) | [EndsWith](string-endswith.md) | `NotEndsWith` is `True` when the selected string does not end with the argument. |
| [Contains](string-contains.md) | [NotContains](string-notcontains.md) | `Contains` is `True` when the selected string contains the argument as a substring. |
| [NotContains](string-notcontains.md) | [Contains](string-contains.md) | `NotContains` is `True` when the selected string does not contain the argument as a substring. |
| [IsIn](string-isin.md) | [IsNotIn](string-isnotin.md) | `IsIn` is `True` when the selected string is one of the candidates. |
| [IsNotIn](string-isnotin.md) | [IsIn](string-isin.md) | `IsNotIn` is `True` when the selected string is none of the candidates. |
| [IsEmpty](string-isempty.md) | [IsNotEmpty](string-isnotempty.md) | `IsEmpty` is `True` when the selected string is the empty string. |
| [IsNotEmpty](string-isnotempty.md) | [IsEmpty](string-isempty.md) | `IsNotEmpty` is `True` when the selected string has at least one character. |
| [IsNullOrEmpty](string-isnullorempty.md) | [IsNotNullOrEmpty](string-isnotnullorempty.md) | `IsNullOrEmpty` is `True` when the selected string is `null` or empty. |
| [IsNotNullOrEmpty](string-isnotnullorempty.md) | [IsNullOrEmpty](string-isnullorempty.md) | `IsNotNullOrEmpty` is `True` when the selected string is not `null` and not empty. |
| [IsNullOrWhiteSpace](string-isnullorwhitespace.md) | [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) | `IsNullOrWhiteSpace` is `True` when the selected string is `null`, empty or only whitespace. |
| [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) | [IsNullOrWhiteSpace](string-isnullorwhitespace.md) | `IsNotNullOrWhiteSpace` is `True` when the selected string has a non-whitespace character. |

## Regex predicates

The `RegexPredicates` factories select a `string?` value and take a regular expression as the argument. A null selected value answers per [Null selected values](#null-selected-values). A pattern or flags string that is a literal in the rule is checked when the rule compiles: an invalid pattern, an unknown flag letter and a repeated flag letter are each a `TRE0026` error. An invalid pattern or flags string from a data source is a fault. The optional argument `flags` holds the letters `i` (ignore case, with the invariant culture), `m` (multiline), `s` (single line), `n` (explicit capture) and `x` (ignore pattern white space). Both predicates use a timeout of one second. The cache keeps at most 256 compiled pairs of pattern and options.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsMatch](regex-ismatch.md) | [IsNotMatch](regex-isnotmatch.md) | `IsMatch` is `True` when the selected string matches the regular expression. The optional argument `flags` sets regular expression options. |
| [IsNotMatch](regex-isnotmatch.md) | [IsMatch](regex-ismatch.md) | `IsNotMatch` is `True` when the selected string does not match the regular expression. It takes the same optional argument `flags`. |

## Collection predicates

The `CollectionPredicates` factories select a collection of strings (`IReadOnlyCollection<string>?`). The element type is `string`. Every comparison is ordinal and case-sensitive. An array argument has the kind `StringArray`. A count argument has the kind `Int64`.

- Every collection predicate answers per [Null selected values](#null-selected-values), except that `SetEquals` and `NotSetEquals` read a null collection as the empty set under `NullBehavior.False`.
- To test a single string against several candidates, use `IsIn` and `IsNotIn` in the [string predicates](#string-predicates).
- A repeated element and a `null` element count as elements in the `HasCount` predicates. They have no effect on the other predicates, and a `null` element never equals a string.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsEmpty](collection-isempty.md) | [IsNotEmpty](collection-isnotempty.md) | `IsEmpty` is `True` when the selected collection has no elements. |
| [IsNotEmpty](collection-isnotempty.md) | [IsEmpty](collection-isempty.md) | `IsNotEmpty` is `True` when the selected collection has at least one element. |
| [Contains](collection-contains.md) | [NotContains](collection-notcontains.md) | `Contains` is `True` when one element equals the argument. |
| [NotContains](collection-notcontains.md) | [Contains](collection-contains.md) | `NotContains` is `True` when no element equals the argument. |
| [ContainsAny](collection-containsany.md) | [NotContainsAny](collection-notcontainsany.md) | `ContainsAny` is `True` when at least one element is in the argument array. |
| [NotContainsAny](collection-notcontainsany.md) | [ContainsAny](collection-containsany.md) | `NotContainsAny` is `True` when no element is in the argument array. |
| [ContainsAll](collection-containsall.md) | [NotContainsAll](collection-notcontainsall.md) | `ContainsAll` is `True` when every argument string is an element. |
| [NotContainsAll](collection-notcontainsall.md) | [ContainsAll](collection-containsall.md) | `NotContainsAll` is `True` when an argument string is not an element. |
| [IsSubsetOf](collection-issubsetof.md) | [IsNotSubsetOf](collection-isnotsubsetof.md) | `IsSubsetOf` is `True` when every element is in the argument array. |
| [IsNotSubsetOf](collection-isnotsubsetof.md) | [IsSubsetOf](collection-issubsetof.md) | `IsNotSubsetOf` is `True` when an element is not in the argument array. |
| [SetEquals](collection-setequals.md) | [NotSetEquals](collection-notsetequals.md) | `SetEquals` is `True` when the collection and the argument array have the same distinct elements. |
| [NotSetEquals](collection-notsetequals.md) | [SetEquals](collection-setequals.md) | `NotSetEquals` is `True` when the collection and the argument array have different distinct elements. |
| [HasCountEqualTo](collection-hascountequalto.md) | [NotHasCountEqualTo](collection-nothascountequalto.md) | `HasCountEqualTo` is `True` when the number of elements equals the argument. |
| [NotHasCountEqualTo](collection-nothascountequalto.md) | [HasCountEqualTo](collection-hascountequalto.md) | `NotHasCountEqualTo` is `True` when the number of elements differs from the argument. |
| [HasCountLessThan](collection-hascountlessthan.md) | [NotHasCountLessThan](collection-nothascountlessthan.md) | `HasCountLessThan` is `True` when the number of elements is less than the argument. |
| [NotHasCountLessThan](collection-nothascountlessthan.md) | [HasCountLessThan](collection-hascountlessthan.md) | `NotHasCountLessThan` is `True` when the number of elements is not less than the argument. |
| [HasCountGreaterThan](collection-hascountgreaterthan.md) | [NotHasCountGreaterThan](collection-nothascountgreaterthan.md) | `HasCountGreaterThan` is `True` when the number of elements is greater than the argument. |
| [NotHasCountGreaterThan](collection-nothascountgreaterthan.md) | [HasCountGreaterThan](collection-hascountgreaterthan.md) | `NotHasCountGreaterThan` is `True` when the number of elements is not greater than the argument. |
| [HasCountLessThanOrEqualTo](collection-hascountlessthanorequalto.md) | [NotHasCountLessThanOrEqualTo](collection-nothascountlessthanorequalto.md) | `HasCountLessThanOrEqualTo` is `True` when the number of elements is at most the argument. |
| [NotHasCountLessThanOrEqualTo](collection-nothascountlessthanorequalto.md) | [HasCountLessThanOrEqualTo](collection-hascountlessthanorequalto.md) | `NotHasCountLessThanOrEqualTo` is `True` when the number of elements is more than the argument. |
| [HasCountGreaterThanOrEqualTo](collection-hascountgreaterthanorequalto.md) | [NotHasCountGreaterThanOrEqualTo](collection-nothascountgreaterthanorequalto.md) | `HasCountGreaterThanOrEqualTo` is `True` when the number of elements is at least the argument. |
| [NotHasCountGreaterThanOrEqualTo](collection-nothascountgreaterthanorequalto.md) | [HasCountGreaterThanOrEqualTo](collection-hascountgreaterthanorequalto.md) | `NotHasCountGreaterThanOrEqualTo` is `True` when the number of elements is less than the argument. |

## Date-time predicates

The `DateTimePredicates` factories select a `DateTimeOffset` (`DateTimeOffset?`) value. Values compare by instant. `IsAfter` and `IsBefore` are strict. `IsBetween` and `IsNotBetween` include both bounds. `IsInTimeWindow` is half-open by default. A `DateTime` is not accepted: the host converts it to a `DateTimeOffset` in the selector. Every bound is a quoted ISO 8601 string with an offset. A null selected value answers per [Null selected values](#null-selected-values).

- `IsAfter`, `IsBefore` and their twins compare with one instant. `IsBetween` and `IsNotBetween` use a range with inclusive bounds. Reversed literal bounds are a compile error.
- `IsAfterNow`, `IsBeforeNow` and their twins take no rule argument. They compare with `TimeProvider.GetUtcNow()` of the `TimeProvider` that the host passes when it registers the predicate.
- `IsOnDayOfWeek`, `IsInMonth`, `IsInTimeWindow` and their twins read the selected instant in a fixed offset. See [Fixed offsets](#fixed-offsets).

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsAfter](datetime-isafter.md) | [IsNotAfter](datetime-isnotafter.md) | `IsAfter` is `True` when the selected instant is later than the argument. |
| [IsNotAfter](datetime-isnotafter.md) | [IsAfter](datetime-isafter.md) | `IsNotAfter` is `True` when the selected instant is equal to or earlier than the argument. |
| [IsBefore](datetime-isbefore.md) | [IsNotBefore](datetime-isnotbefore.md) | `IsBefore` is `True` when the selected instant is earlier than the argument. |
| [IsNotBefore](datetime-isnotbefore.md) | [IsBefore](datetime-isbefore.md) | `IsNotBefore` is `True` when the selected instant is equal to or later than the argument. |
| [IsBetween](datetime-isbetween.md) | [IsNotBetween](datetime-isnotbetween.md) | `IsBetween` is `True` when the selected instant lies in a range. |
| [IsNotBetween](datetime-isnotbetween.md) | [IsBetween](datetime-isbetween.md) | `IsNotBetween` is `True` when the selected instant lies outside a range. |
| [IsAfterNow](datetime-isafternow.md) | [IsNotAfterNow](datetime-isnotafternow.md) | `IsAfterNow` is `True` when the selected instant is later than now. |
| [IsNotAfterNow](datetime-isnotafternow.md) | [IsAfterNow](datetime-isafternow.md) | `IsNotAfterNow` is `True` when the selected instant is equal to now or earlier. |
| [IsBeforeNow](datetime-isbeforenow.md) | [IsNotBeforeNow](datetime-isnotbeforenow.md) | `IsBeforeNow` is `True` when the selected instant is earlier than now. |
| [IsNotBeforeNow](datetime-isnotbeforenow.md) | [IsBeforeNow](datetime-isbeforenow.md) | `IsNotBeforeNow` is `True` when the selected instant is equal to now or later. |
| [IsOnDayOfWeek](datetime-isondayofweek.md) | [IsNotOnDayOfWeek](datetime-isnotondayofweek.md) | `IsOnDayOfWeek` is `True` when the selected instant falls on one of the listed days of the week. |
| [IsNotOnDayOfWeek](datetime-isnotondayofweek.md) | [IsOnDayOfWeek](datetime-isondayofweek.md) | `IsNotOnDayOfWeek` is `True` when the selected instant falls on none of the listed days of the week. |
| [IsInMonth](datetime-isinmonth.md) | [IsNotInMonth](datetime-isnotinmonth.md) | `IsInMonth` is `True` when the selected instant falls in one of the listed months. |
| [IsNotInMonth](datetime-isnotinmonth.md) | [IsInMonth](datetime-isinmonth.md) | `IsNotInMonth` is `True` when the selected instant falls in none of the listed months. |
| [IsInTimeWindow](datetime-isintimewindow.md) | [IsNotInTimeWindow](datetime-isnotintimewindow.md) | `IsInTimeWindow` is `True` when the time of day of the selected instant is inside a daily window. |
| [IsNotInTimeWindow](datetime-isnotintimewindow.md) | [IsInTimeWindow](datetime-isintimewindow.md) | `IsNotInTimeWindow` is `True` when the time of day of the selected instant is outside a daily window. |

### Fixed offsets

`IsOnDayOfWeek`, `IsInMonth`, `IsInTimeWindow` and their twins convert the selected instant to a fixed offset from UTC before they read the day, the month or the time of day. The `offset` argument gives the offset.

| Offset text | Meaning |
| --- | --- |
| `"Z"` | UTC |
| `"+hh:mm"` | Later than UTC, for example `"+05:30"` |
| `"-hh:mm"` | Earlier than UTC, for example `"-03:00"` |

- The offset is from `-14:00` to `+14:00`. Two digits are necessary for the hours and for the minutes.
- Time zone names, such as the IANA name `Europe/Paris`, are not accepted. The diagnostic says that time zone names are not supported.
- There are no daylight-saving rules. A host that needs local time in a zone with daylight saving selects the instant in the correct offset, or registers one predicate for each offset.
- The argument names of these predicates are fixed. The host cannot change them.
- A literal argument that is not valid is a `TRE0026` compile error at the call (see [diagnostics](../specification/diagnostics.md)). A value from a data source is checked at evaluation. A value that is not valid makes the predicate throw an `ArgumentException`. The evaluator records a fault and the term is `Unknown`. The check runs before the selector, so a null selected value does not hide it.

## Type-test predicates

The `TypePredicates` factories test what a value is. Each has two overloads: a `string?` selector, which parses the text, and an `object?` selector, which tests the runtime type and also reads text. They take no rule argument. Parsing uses the invariant culture.

- A null selected value answers `Unknown` by default, because the type of a missing value is not known. A host can register a type test with `NullBehavior.False`: the positive test then answers `False` and its `IsNotX` twin answers `True`. See [Null selected values](#null-selected-values).
- A twin answers `Unknown` for a null selected value too.

| Predicate | Twin | Summary |
| --- | --- | --- |
| [IsGuid](type-isguid.md) | [IsNotGuid](type-isnotguid.md) | `IsGuid` is `True` when the selected value is a GUID. |
| [IsNotGuid](type-isnotguid.md) | [IsGuid](type-isguid.md) | `IsNotGuid` is `True` when the selected value is not a GUID. |
| [IsNumeric](type-isnumeric.md) | [IsNotNumeric](type-isnotnumeric.md) | `IsNumeric` is `True` when the selected value is numeric. |
| [IsNotNumeric](type-isnotnumeric.md) | [IsNumeric](type-isnumeric.md) | `IsNotNumeric` is `True` when the selected value is not numeric. |
| [IsUrl](type-isurl.md) | [IsNotUrl](type-isnoturl.md) | `IsUrl` is `True` when the selected value is an absolute `http` or `https` URL. |
| [IsNotUrl](type-isnoturl.md) | [IsUrl](type-isurl.md) | `IsNotUrl` is `True` when the selected value is not an absolute `http` or `https` URL. |
| [IsString](type-isstring.md) | [IsNotString](type-isnotstring.md) | `IsString` is `True` when the selected value is a string. |
| [IsNotString](type-isnotstring.md) | [IsString](type-isstring.md) | `IsNotString` is `True` when the selected value is not a string. |
| [IsDateTimeOffset](type-isdatetimeoffset.md) | [IsNotDateTimeOffset](type-isnotdatetimeoffset.md) | `IsDateTimeOffset` is `True` when the selected value is an ISO 8601 date and time with an offset. |
| [IsNotDateTimeOffset](type-isnotdatetimeoffset.md) | [IsDateTimeOffset](type-isdatetimeoffset.md) | `IsNotDateTimeOffset` is `True` when the selected value is not an ISO 8601 date and time with an offset. |

## Selected-value factory

`SelectedValuePredicates.Create` is not a fixed predicate. It is a factory that builds a predicate over a value that the host reads from an external source. It has no twin and no `nullBehavior` option.

| Factory | Summary |
| --- | --- |
| [Create](selectedvalue-create.md) | `Create` builds a predicate from a select delegate, and optionally a test delegate, that the host supplies. |

## How a predicate page is organized

A predicate page has these sections, in this order: Name, Classification, Selector and kinds, Arguments, Definition, Answers, Null selected value and Examples. Reversed bounds, Argument errors, Edge cases and Related predicates appear where they apply. A page is named `<family>-<factory>.md` in lower case.
