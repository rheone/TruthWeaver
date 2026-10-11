# Predicate conventions

The built-in predicates follow one set of conventions for case, argument names, whitespace, range ends, null values and culture. This page states each convention and lists the settings that change a default. Back to the [README](../README.md); the list of predicates is in [Predicate types](predicates.md).

## Naming

A predicate name tells you the form of the test. There are two forms, and each predicate has a twin.

| Form | Use for | Positive | Twin |
| --- | --- | --- | --- |
| A: `IsX` and `IsNotX` | A state or a relation: equality, ordering, range, membership, dates, calendar, null and type tests | `IsEqualTo`, `IsLessThan`, `IsBetween`, `IsIn`, `IsAfter`, `IsInMonth`, `IsNull`, `IsString` | `IsNotEqualTo`, `IsNotLessThan`, `IsNotBetween`, `IsNotIn`, `IsNotAfter`, `IsNotInMonth`, `IsNotNull`, `IsNotString` |
| B: `Verb` and `NotVerb` | A verb phrase | `Contains`, `ContainsAll`, `ContainsAny`, `StartsWith`, `EndsWith`, `SetEquals` | `NotContains`, `NotContainsAll`, `NotContainsAny`, `NotStartsWith`, `NotEndsWith`, `NotSetEquals` |

- A twin is always the Strong Kleene complement of its positive predicate: `True` becomes `False`, `False` becomes `True` and `Unknown` stays `Unknown`. This holds for a null selected value too. See [Null selected values](predicates.md#null-selected-values).
- Each of the four ordering predicates `IsLessThan`, `IsGreaterThan`, `IsLessThanOrEqualTo` and `IsGreaterThanOrEqualTo` has its own twin: `IsNotLessThan`, `IsNotGreaterThan`, `IsNotLessThanOrEqualTo` and `IsNotGreaterThanOrEqualTo`. `IsNotLessThan` gives the same answers as `IsGreaterThanOrEqualTo` for a value that is not null.
- String equality is one predicate. `IsEqualTo` takes the optional rule arguments `ignoreCase` and `trim`, both `false` by default. There is no separate case-insensitive predicate.

### Names before the naming guide

The table lists each predicate whose name changed. The packages were not published before the change, so there are no old names to keep. `NumericPredicates` and `ScalarPredicates` take no `ignoreCase` or `trim` argument.

| Old name | New name |
| --- | --- |
| `StringPredicates.Equals` | `StringPredicates.IsEqualTo` |
| `StringPredicates.NotEquals` | `StringPredicates.IsNotEqualTo` |
| `StringPredicates.EqualsIgnoreCase` | `StringPredicates.IsEqualTo`, rule argument `ignoreCase: true` |
| `StringPredicates.NotEqualsIgnoreCase` | `StringPredicates.IsNotEqualTo`, rule argument `ignoreCase: true` |
| `StringPredicates.EqualsConfigurable` | `StringPredicates.IsEqualTo` |
| `StringPredicates.NotEqualsConfigurable` | `StringPredicates.IsNotEqualTo` |
| `StringPredicates.In`, `NotIn` | `StringPredicates.IsIn`, `IsNotIn` |
| `NumericPredicates.Equal`, `NotEqual` | `NumericPredicates.IsEqualTo`, `IsNotEqualTo` |
| `NumericPredicates.LessThan` | `NumericPredicates.IsLessThan` (new twin `IsNotLessThan`) |
| `NumericPredicates.GreaterThan` | `NumericPredicates.IsGreaterThan` (new twin `IsNotGreaterThan`) |
| `NumericPredicates.LessThanOrEqual` | `NumericPredicates.IsLessThanOrEqualTo` (new twin `IsNotLessThanOrEqualTo`) |
| `NumericPredicates.GreaterThanOrEqual` | `NumericPredicates.IsGreaterThanOrEqualTo` (new twin `IsNotGreaterThanOrEqualTo`) |
| `NumericPredicates.Between`, `NotBetween` | `NumericPredicates.IsBetween`, `IsNotBetween` |
| `NumericPredicates.In`, `NotIn` | `NumericPredicates.IsIn`, `IsNotIn` |
| `ScalarPredicates.Equal`, `NotEqual` | `ScalarPredicates.IsEqualTo`, `IsNotEqualTo` |
| `ScalarPredicates.In`, `NotIn` | `ScalarPredicates.IsIn`, `IsNotIn` |
| `DateTimePredicates.After`, `NotAfter` | `DateTimePredicates.IsAfter`, `IsNotAfter` |
| `DateTimePredicates.Before`, `NotBefore` | `DateTimePredicates.IsBefore`, `IsNotBefore` |
| `DateTimePredicates.AfterNow`, `NotAfterNow` | `DateTimePredicates.IsAfterNow`, `IsNotAfterNow` |
| `DateTimePredicates.BeforeNow`, `NotBeforeNow` | `DateTimePredicates.IsBeforeNow`, `IsNotBeforeNow` |
| `DateTimePredicates.Between`, `NotBetween` | `DateTimePredicates.IsBetween`, `IsNotBetween` |
| `DateTimePredicates.OnDayOfWeek`, `NotOnDayOfWeek` | `DateTimePredicates.IsOnDayOfWeek`, `IsNotOnDayOfWeek` |
| `DateTimePredicates.InMonth`, `NotInMonth` | `DateTimePredicates.IsInMonth`, `IsNotInMonth` |
| `DateTimePredicates.InTimeWindow`, `NotInTimeWindow` | `DateTimePredicates.IsInTimeWindow`, `IsNotInTimeWindow` |
| `CollectionPredicates.CountEqual`, `NotCountEqual` | `CollectionPredicates.HasCountEqualTo`, `NotHasCountEqualTo` |
| `CollectionPredicates.CountGreaterThan`, `NotCountGreaterThan` | `CollectionPredicates.HasCountGreaterThan`, `NotHasCountGreaterThan` |
| `CollectionPredicates.CountGreaterThanOrEqual`, `NotCountGreaterThanOrEqual` | `CollectionPredicates.HasCountGreaterThanOrEqualTo`, `NotHasCountGreaterThanOrEqualTo` |
| `CollectionPredicates.CountLessThan`, `NotCountLessThan` | `CollectionPredicates.HasCountLessThan`, `NotHasCountLessThan` |
| `CollectionPredicates.CountLessThanOrEqual`, `NotCountLessThanOrEqual` | `CollectionPredicates.HasCountLessThanOrEqualTo`, `NotHasCountLessThanOrEqualTo` |
| `RegexPredicates.Matches`, `NotMatches` | `RegexPredicates.IsMatch`, `IsNotMatch` |
| `RegexPredicates.MatchesIgnoreCase`, `NotMatchesIgnoreCase` | `RegexPredicates.IsMatch`, `IsNotMatch`, rule argument `flags: "i"` |

The predicates `IsEmpty`, `IsNotEmpty`, `IsNull`, `IsNotNull`, `IsDefault`, `IsNotDefault`, `IsNullOrEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`, `IsSubsetOf`, `IsNotSubsetOf`, the type tests and the Form B predicates keep their names. The collection count predicates now use the `HasCount` names, which are Form B verb phrases with the twin `NotHasCount…`.

## Conventions

| Topic | Convention |
| --- | --- |
| Case | String and collection comparison is ordinal and case-sensitive. Case-insensitive string equality is the `ignoreCase: true` argument of `IsEqualTo` and `IsNotEqualTo`. |
| Argument names | A rule matches an argument name ignoring case, as it does a predicate name. `Crust:` and `crust:` name the same argument. The canonical text prints the name as the schema spells it. A schema cannot declare two argument names that differ only in case: registration fails with an `ArgumentException`. |
| Range bound names | `lower` and `upper` bound a value. `min` and `max` bound an operand count. `start` and `end` bound a time of day. |
| Whitespace | A comparison does not trim. A predicate that trims has a `trim` argument, and `trim` defaults to `false`. |
| Boolean options | `ignoreCase` and `trim` default to `false`. |
| Value ranges | A range of values is closed. `IsBetween` and `IsNotBetween` treat both bounds as inside the range. |
| Ordering | An ordering comparison is strict. `IsAfter` and `IsBefore` do not match an equal instant. |
| Time windows | A window of time is half-open: `[start, end)`. The flags `includeStart` and `includeEnd` move either end. |
| Null values | A null selected value answers `Unknown`. The host chooses `nullBehavior` at registration. |
| Culture | Culture is invariant. No predicate reads the culture of the host process. |

## Settings that change a default

| Predicate | Setting | Default |
| --- | --- | --- |
| `IsEqualTo`, `IsNotEqualTo` (string) | `ignoreCase` | `false` |
| `IsEqualTo`, `IsNotEqualTo` (string) | `trim` | `false` |
| `IsMatch`, `IsNotMatch` | `flags` | empty (no options) |
| `IsInTimeWindow`, `IsNotInTimeWindow` | `includeStart` | `true` |
| `IsInTimeWindow`, `IsNotInTimeWindow` | `includeEnd` | `false` |
| String, collection, regex, numeric, scalar and date predicates (set at registration) | `nullBehavior` | `Unknown` |

A predicate that is not in the table has no setting. Its behavior is fixed.

## Fixed behavior

- String predicates compare with ordinal, case-sensitive rules and do not trim.
- Collection predicates compare their elements with ordinal, case-sensitive rules.
- `Contains`, `StartsWith` and `EndsWith` have no case or trim setting. Use `IsEqualTo` to match a whole string with options.
- `IsMatch` and `IsNotMatch` use no regular expression options unless the rule sets the optional `flags` argument. They use a timeout of one second.
- `IsBetween` and `IsNotBetween` include both bounds.
- `IsAfter` and `IsBefore` exclude an equal instant.

## Ranges and windows

A range of values and a window of time differ on purpose. A value range includes both bounds, because the bounds are values that a rule author names. A time window excludes its end, so two windows that meet at one time do not overlap.

| Predicate | Start | End |
| --- | --- | --- |
| `IsBetween` | Inside | Inside |
| `IsAfter` | Outside | Not applicable |
| `IsInTimeWindow` | Inside | Outside |

With `includeEnd: true`, a window includes its end. With `includeStart: false`, a window excludes its start.

## Examples

The first term matches a crust of `thin` with the default case-sensitive comparison. The second term opts in to a case-insensitive comparison.

<!-- doctest:rule conventions -->
```text
hasCrust(crust: "thin") OR hasCrust(crust: "stuffed", ignoreCase: true)
```

The canonical text of the rule shows the defaults that the compiler fills in: `ignoreCase: false` for the first term and `trim: false` for both.

## Argument names

The compiler matches each argument name to the schema ignoring case. The next rule writes the argument as `Crust`. Its canonical text spells it `crust`, the way the schema declares it, so the term is the same as one that was written with `crust`.

<!-- doctest:rule argument-case -->
```text
hasCrust(Crust: "thin")
```

An argument that is written twice is an error, whatever the case of each spelling. `hasCrust(crust: "thin", Crust: "stuffed")` is `DuplicateArgument` (`TRE0032`). This applies to rule text, JSON, YAML and `RuleBuilder`. Argument values stay case-sensitive.
