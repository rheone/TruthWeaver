# Regex IsNotMatch

`IsNotMatch` is `True` when the selected string does not match the regular expression in the argument. The rule can set regular expression options with the optional argument `flags`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `RegexPredicates.IsNotMatch`
- Default label: `Is Not Match`
- Default argument name: `pattern`. The host can change it when it registers the predicate.
- Optional rule argument: `flags`. The host cannot rename it.
- Rule text: `codeIsNotMatch(pattern: "^[A-Z]{2}[0-9]{4}$")`. The host chooses the name `codeIsNotMatch` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `RegexPredicates`
- Twin: [IsMatch](regex-ismatch.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `pattern` | `String` | The regular expression. |
| `flags` | `String` | Optional. Default is the empty string, which sets no options. Single letters `i`, `m`, `s`, `n` and `x` that set regular expression options. They are the same as the flags of [IsMatch](regex-ismatch.md#flags). |

## Definition

`True` when the regular expression `pattern` compiled with the options that `flags` names, finds no match in the selected string. `False` when it finds a match. The pattern uses the .NET regular expression syntax.

## Answers

The table shows the rule `codeIsNotMatch(pattern: "^[A-Z]{2}[0-9]{4}$")`.

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
| `codeIsNotMatch(pattern: "[0-9]+")` | `code = "abc123"` | `False` | The pattern is not anchored, so it matches the digits inside the string. A match makes `IsNotMatch` `False`. |
| `codeIsNotMatch(pattern: "^[0-9]+$")` | `code = "abc123"` | `True` | The anchors require the whole string to be digits, so there is no match. |
| `codeIsNotMatch(pattern: "(?i)^ab")` | `code = "ABC"` | `False` | The inline option `(?i)` ignores case, so there is a match. |
| `codeIsNotMatch(pattern: "^ab", flags: "i")` | `code = "ABC"` | `False` | The flag `i` ignores case, so there is a match. |
| `codeIsNotMatch(pattern: "^ab", flags: "q")` | `code = "ABC"` | Compile error | `q` is not a flag, so the rule does not compile (`TRE0026`). |
| `codeIsNotMatch(pattern: "(")` | `code = "anything"` | Compile error | The pattern is a literal and is not valid, so the rule does not compile (`TRE0026`). |
| `codeIsNotMatch(pattern: from("request", "$.pattern"))` | `code = "anything"`, `request.pattern = "("` | `Unknown` | The pattern comes from a data source and is not valid. The predicate faults, and a fault answers `Unknown`. |
| `codeIsNotMatch(pattern: from("request", "$.pattern"))` | `code = null`, `request.pattern = "("` | `Unknown` | The engine reads and checks the pattern before it looks at the selected value. The pattern is not valid, so the predicate faults even though the selected value is null. |
| `codeIsNotMatch(pattern: "^[A-Z]{2}[0-9]{4}$")` | `code = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The match is a search, not a full match. Add `^` and `$` to require the whole string to match.
- Case is significant unless the rule sets the flag `i` or the pattern starts with the inline option `(?i)`.
- The compiler checks `flags` and `pattern` when they are literals. An unknown flag letter, a repeated flag letter and an invalid pattern are each a `TRE0026` error, and the rule does not compile. See [IsMatch](regex-ismatch.md#edge-cases).
- A `flags` or `pattern` value from a data source is checked when the rule evaluates, before the selected value is read, so a null selected value does not hide an invalid value. An invalid value faults: the answer is `Unknown` and the decision records one fault. `IsMatch` and `IsNotMatch` both answer `Unknown` in this case.
- A match attempt that takes longer than one second faults in the same way.
- The engine compiles each distinct pair of pattern and options once and reuses it for every later evaluation. The cache keeps at most 256 pairs. When it is full, the next new pair clears it, and the pairs it held are compiled again when they are used.

## Related predicates

- [IsMatch](regex-ismatch.md) is the exact complement of this predicate.
- [NotStartsWith](string-notstartswith.md) tests a fixed prefix.
- [NotEndsWith](string-notendswith.md) tests a fixed suffix.
- [NotContains](string-notcontains.md) tests a fixed substring.
