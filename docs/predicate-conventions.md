# Predicate conventions

The built-in predicates follow one set of conventions for case, argument names, whitespace, range ends, null values and culture. This page states each convention and lists the settings that change a default. Back to the [README](../README.md); the list of predicates is in [Predicate types](predicates.md).

## Conventions

| Topic | Convention |
| --- | --- |
| Case | String and collection comparison is ordinal and case-sensitive. A case-insensitive form is a separate predicate (`EqualsIgnoreCase`) or an explicit `ignoreCase: true`. |
| Argument names | A rule matches an argument name ignoring case, as it does a predicate name. `Crust:` and `crust:` name the same argument. The canonical text prints the name as the schema spells it. A schema cannot declare two argument names that differ only in case: registration fails with an `ArgumentException`. |
| Range bound names | `lower` and `upper` bound a value. `min` and `max` bound an operand count. `start` and `end` bound a time of day. |
| Whitespace | A comparison does not trim. A predicate that trims has a `trim` argument, and `trim` defaults to `false`. |
| Boolean options | `ignoreCase` and `trim` default to `false`. |
| Value ranges | A range of values is closed. `Between` and `Outside` treat both bounds as inside the range. |
| Ordering | An ordering comparison is strict. `After` and `Before` do not match an equal instant. |
| Time windows | A window of time is half-open: `[start, end)`. The flags `includeStart` and `includeEnd` move either end. |
| Null values | A null selected value answers `Unknown`. The host chooses `nullBehavior` at registration. |
| Culture | Culture is invariant. No predicate reads the culture of the host process. |

## Settings that change a default

| Predicate | Setting | Default |
| --- | --- | --- |
| `EqualsConfigurable`, `NotEqualsConfigurable` | `ignoreCase` | `false` |
| `EqualsConfigurable`, `NotEqualsConfigurable` | `trim` | `false` |
| `InTimeWindow`, `NotInTimeWindow` | `includeStart` | `true` |
| `InTimeWindow`, `NotInTimeWindow` | `includeEnd` | `false` |
| String, collection, regex, numeric, scalar and date predicates (set at registration) | `nullBehavior` | `Unknown` |

A predicate that is not in the table has no setting. Its behavior is fixed.

## Fixed behavior

- String predicates compare with ordinal, case-sensitive rules and do not trim.
- Collection predicates compare their elements with ordinal, case-sensitive rules.
- `Contains`, `StartsWith` and `EndsWith` have no case or trim setting. Use `EqualsConfigurable` to match a whole string with options.
- `Matches` uses `RegexOptions.None` and a timeout of one second. To ignore case, start the pattern with `(?i)`.
- `Between` and `Outside` include both bounds.
- `After` and `Before` exclude an equal instant.

## Ranges and windows

A range of values and a window of time differ on purpose. A value range includes both bounds, because the bounds are values that a rule author names. A time window excludes its end, so two windows that meet at one time do not overlap.

| Predicate | Start | End |
| --- | --- | --- |
| `Between` | Inside | Inside |
| `After` | Outside | Not applicable |
| `InTimeWindow` | Inside | Outside |

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
