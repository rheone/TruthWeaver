# Regex MatchesIgnoreCase

`MatchesIgnoreCase` is `True` when the selected string matches the regular expression in the argument, ignoring case. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `RegexPredicates.MatchesIgnoreCase`
- Default label: `Matches Ignore Case`
- Default argument name: `pattern`. The host can change it when it registers the predicate.
- Rule text: `codeMatchesIgnoreCase(pattern: "^[A-Z]{2}[0-9]{4}$")`. The host chooses the name `codeMatchesIgnoreCase` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `RegexPredicates`
- Twin: [NotMatchesIgnoreCase](regex-notmatchesignorecase.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `pattern` | `String` | The regular expression. |

## Definition

`True` when the regular expression `pattern` finds a match anywhere in the selected string. `False` otherwise. The pattern uses the .NET regular expression syntax. Case is ignored with the invariant culture, as if the pattern started with `(?i)`.

## Answers

The table shows the rule `codeMatchesIgnoreCase(pattern: "^[A-Z]{2}[0-9]{4}$")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"AB1234"` | `True` | `True` |
| `"ab1234"` | `True` | `True` |
| `"AB12345"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `codeMatchesIgnoreCase(pattern: "[0-9]+")` | `code = "abc123"` | `True` | The pattern is not anchored, so it matches the digits inside the string. |
| `codeMatchesIgnoreCase(pattern: "^[0-9]+$")` | `code = "abc123"` | `False` | The anchors require the whole string to be digits. |
| `codeMatchesIgnoreCase(pattern: "^ab")` | `code = "ABC"` | `True` | Case is ignored, so `ABC` matches `^ab`. |
| `codeMatchesIgnoreCase(pattern: "(")` | `code = "anything"` | Compile error | The pattern is a literal and is not valid, so the rule does not compile (`TRE0026`). |
| `codeMatchesIgnoreCase(pattern: from("request", "$.pattern"))` | `code = "anything"`, `request.pattern = "("` | `Unknown` | The pattern comes from a data source and is not valid. The predicate faults, and a fault answers `Unknown`. |
| `codeMatchesIgnoreCase(pattern: from("request", "$.pattern"))` | `code = null`, `request.pattern = "("` | `Unknown` | A null selected value answers `Unknown` before the engine reads the pattern, so there is no fault. |
| `NOT codeMatchesIgnoreCase(pattern: "^[A-Z]{2}[0-9]{4}$")` | `code = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The match is a search, not a full match. Add `^` and `$` to require the whole string to match.
- Case is ignored with the invariant culture. An inline option in the pattern still applies: `(?-i)` turns case sensitivity back on for the rest of the pattern.
- The compiler checks a pattern that is a literal. An invalid literal pattern is a `TRE0026` error and the rule does not compile.
- A pattern from a data source is checked when the rule evaluates. An invalid pattern faults: the answer is `Unknown` and the decision records one fault. `MatchesIgnoreCase` and `NotMatchesIgnoreCase` both answer `Unknown` in this case.
- A match attempt that takes longer than one second faults in the same way.
- The engine compiles each distinct pattern once and reuses it for every later evaluation. The cache keeps at most 256 patterns. When it is full, the next new pattern clears it, and the patterns it held are compiled again when they are used.

## Related predicates

- [NotMatchesIgnoreCase](regex-notmatchesignorecase.md) is the exact complement of this predicate.
- [StartsWith](string-startswith.md) tests a fixed prefix.
- [EndsWith](string-endswith.md) tests a fixed suffix.
- [Contains](string-contains.md) tests a fixed substring.
