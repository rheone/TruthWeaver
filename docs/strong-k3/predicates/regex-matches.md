# Regex Matches

`Matches` is `True` when the selected string matches the regular expression in the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `RegexPredicates.Matches`
- Default label: `Matches`
- Default argument name: `pattern`. The host can change it when it registers the predicate.
- Rule text: `codeMatches(pattern: "^[A-Z]{2}[0-9]{4}$")`. The host chooses the name `codeMatches` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `RegexPredicates`
- Twin: [NotMatches](regex-notmatches.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the regular expression `pattern` finds a match anywhere in the selected string. `False` otherwise. The pattern uses the .NET regular expression syntax with no extra options.

## Answers

The table shows the rule `codeMatches(pattern: "^[A-Z]{2}[0-9]{4}$")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"AB1234"` | `True` | `True` |
| `"ab1234"` | `False` | `False` |
| `"AB12345"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `codeMatches(pattern: "[0-9]+")` | `code = "abc123"` | `True` | The pattern is not anchored, so it matches the digits inside the string. |
| `codeMatches(pattern: "^[0-9]+$")` | `code = "abc123"` | `False` | The anchors require the whole string to be digits. |
| `codeMatches(pattern: "(?i)^ab")` | `code = "ABC"` | `True` | The inline option `(?i)` ignores case. |
| `codeMatches(pattern: "(")` | `code = "anything"` | Compile error | The pattern is a literal and is not valid, so the rule does not compile (`TRE0026`). |
| `codeMatches(pattern: from("request", "$.pattern"))` | `code = "anything"`, `request.pattern = "("` | `Unknown` | The pattern comes from a data source and is not valid. The predicate faults, and a fault answers `Unknown`. |
| `codeMatches(pattern: from("request", "$.pattern"))` | `code = null`, `request.pattern = "("` | `Unknown` | A null selected value answers `Unknown` before the engine reads the pattern, so there is no fault. |
| `NOT codeMatches(pattern: "^[A-Z]{2}[0-9]{4}$")` | `code = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The match is a search, not a full match. Add `^` and `$` to require the whole string to match.
- Case is significant. To ignore case, use [MatchesIgnoreCase](regex-matchesignorecase.md), or start the pattern with the inline option `(?i)`.
- The compiler checks a pattern that is a literal. An invalid literal pattern is a `TRE0026` error and the rule does not compile.
- A pattern from a data source is checked when the rule evaluates. An invalid pattern faults: the answer is `Unknown` and the decision records one fault. `Matches` and `NotMatches` both answer `Unknown` in this case.
- A match attempt that takes longer than one second faults in the same way.
- The engine compiles each distinct pattern once and reuses it for every later evaluation. The cache keeps at most 256 patterns. When it is full, the next new pattern clears it, and the patterns it held are compiled again when they are used.

## Related predicates

- [NotMatches](regex-notmatches.md) is the exact complement of this predicate.
- [MatchesIgnoreCase](regex-matchesignorecase.md) ignores case.
- [StartsWith](string-startswith.md) tests a fixed prefix.
- [EndsWith](string-endswith.md) tests a fixed suffix.
- [Contains](string-contains.md) tests a fixed substring.
