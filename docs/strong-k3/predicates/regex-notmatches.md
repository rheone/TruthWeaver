# Regex NotMatches

`NotMatches` is `True` when the selected string does not match the regular expression in the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `RegexPredicates.NotMatches`
- Default label: `Not Matches`
- Default argument name: `pattern`. The host can change it when it registers the predicate.
- Rule text: `codeNotMatches(pattern: "^[A-Z]{2}[0-9]{4}$")`. The host chooses the name `codeNotMatches` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `RegexPredicates`
- Twin: [Matches](regex-matches.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the regular expression `pattern` finds no match in the selected string. `False` when it finds a match. The pattern uses the .NET regular expression syntax with no extra options.

## Answers

The table shows the rule `codeNotMatches(pattern: "^[A-Z]{2}[0-9]{4}$")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"AB1234"` | `False` | `False` |
| `"ab1234"` | `True` | `True` |
| `"AB12345"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `codeNotMatches(pattern: "[0-9]+")` | `code = "abc123"` | `False` | The pattern is not anchored, so it matches the digits inside the string. A match makes `NotMatches` `False`. |
| `codeNotMatches(pattern: "^[0-9]+$")` | `code = "abc123"` | `True` | The anchors require the whole string to be digits, so there is no match. |
| `codeNotMatches(pattern: "(?i)^ab")` | `code = "ABC"` | `False` | The inline option `(?i)` ignores case, so there is a match. |
| `codeNotMatches(pattern: "(")` | `code = "anything"` | `Unknown` | The pattern is not valid. The predicate faults, and a fault answers `Unknown`. |
| `codeNotMatches(pattern: "(")` | `code = null` | `Unknown` | A null selected value answers `Unknown` before the engine reads the pattern, so there is no fault. |
| `codeNotMatches(pattern: "^[A-Z]{2}[0-9]{4}$")` | `code = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The match is a search, not a full match. Add `^` and `$` to require the whole string to match.
- Case is significant. The inline option `(?i)` at the start of the pattern ignores case.
- The compiler does not check the pattern. An invalid pattern faults when the rule evaluates: the answer is `Unknown` and the decision records one fault. `Matches` and `NotMatches` both answer `Unknown` in this case.
- A match attempt that takes longer than one second faults in the same way.
- The engine compiles each distinct pattern once and reuses it for every later evaluation.

## Related predicates

- [Matches](regex-matches.md) is the exact complement of this predicate.
- [NotStartsWith](string-notstartswith.md) tests a fixed prefix.
- [NotEndsWith](string-notendswith.md) tests a fixed suffix.
- [NotContains](string-notcontains.md) tests a fixed substring.
