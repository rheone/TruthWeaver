# Regex IsMatch

`IsMatch` is `True` when the selected string matches the regular expression in the argument. The rule can set regular expression options with the optional argument `flags`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `RegexPredicates.IsMatch`
- Default label: `Is Match`
- Default argument name: `pattern`. The host can change it when it registers the predicate.
- Optional rule argument: `flags`. The host cannot rename it.
- Rule text: `codeIsMatch(pattern: "^[A-Z]{2}[0-9]{4}$")`. The host chooses the name `codeIsMatch` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `RegexPredicates`
- Twin: [IsNotMatch](regex-isnotmatch.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `pattern` | `String` | The regular expression. |
| `flags` | `String` | Optional. Default is the empty string, which sets no options. Single letters that set regular expression options. See [Flags](#flags). |

## Flags

The `flags` argument is a string of the letters in the table. Each letter can occur at most once. The letters are case-sensitive. The engine always adds `RegexOptions.CultureInvariant`, whatever the flags are. The culture of the host process has no effect on a match, also when the pattern turns on the inline option `(?i)`.

| Letter | Option | Effect |
| --- | --- | --- |
| `i` | `RegexOptions.IgnoreCase` | The match ignores case. |
| `m` | `RegexOptions.Multiline` | `^` and `$` also match at the start and end of each line. |
| `s` | `RegexOptions.Singleline` | `.` also matches a line break. |
| `n` | `RegexOptions.ExplicitCapture` | Only named groups capture. |
| `x` | `RegexOptions.IgnorePatternWhitespace` | The pattern ignores unescaped white space and `#` comments. |

## Definition

`True` when the regular expression `pattern`, compiled with the options that `flags` names, finds a match anywhere in the selected string. `False` otherwise. The pattern uses the .NET regular expression syntax.

## Answers

The table shows the rule `codeIsMatch(pattern: "^[A-Z]{2}[0-9]{4}$")`.

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
| `codeIsMatch(pattern: "[0-9]+")` | `code = "abc123"` | `True` | The pattern is not anchored, so it matches the digits inside the string. |
| `codeIsMatch(pattern: "^[0-9]+$")` | `code = "abc123"` | `False` | The anchors require the whole string to be digits. |
| `codeIsMatch(pattern: "^ab", flags: "i")` | `code = "ABC"` | `True` | The flag `i` ignores case. |
| `codeIsMatch(pattern: "^b$", flags: "m")` | `code = "a\nb"` | `True` | The flag `m` lets `^` and `$` match at line breaks. |
| `codeIsMatch(pattern: "^ab", flags: "q")` | `code = "ABC"` | Compile error | `q` is not a flag, so the rule does not compile (`TRE0026`). |
| `codeIsMatch(pattern: "^ab", flags: "ii")` | `code = "ABC"` | Compile error | A flag letter occurs twice, so the rule does not compile (`TRE0026`). |
| `codeIsMatch(pattern: "(")` | `code = "anything"` | Compile error | The pattern is a literal and is not valid, so the rule does not compile (`TRE0026`). |
| `codeIsMatch(pattern: from("request", "$.pattern"))` | `code = "anything"`, `request.pattern = "("` | `Unknown` | The pattern comes from a data source and is not valid. The predicate faults, and a fault answers `Unknown`. |
| `codeIsMatch(pattern: from("request", "$.pattern"))` | `code = null`, `request.pattern = "("` | `Unknown` | The engine reads and checks the pattern before it looks at the selected value. The pattern is not valid, so the predicate faults even though the selected value is null. |
| `NOT codeIsMatch(pattern: "^[A-Z]{2}[0-9]{4}$")` | `code = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The match is a search, not a full match. Add `^` and `$` to require the whole string to match.
- Case is significant unless the rule sets the flag `i` or the pattern starts with the inline option `(?i)`. An inline option in the pattern, such as `(?-i)`, takes effect after the flags.
- The compiler checks `flags` and `pattern` when they are literals. An unknown flag letter, a repeated flag letter and an invalid pattern are each a `TRE0026` error, and the rule does not compile. The compiler checks the pattern with the options that `flags` names, so a pattern that is valid only with the flag `x` compiles. When the flags are not valid, the compiler reports only the flags.
- When the flags come from a data source, the compiler does not check a literal pattern, because the options are unknown. The pattern is checked when the rule evaluates.
- A `flags` or `pattern` value from a data source is checked when the rule evaluates, before the selected value is read, so a null selected value does not hide an invalid value. An invalid value faults: the answer is `Unknown` and the decision records one fault. `IsMatch` and `IsNotMatch` both answer `Unknown` in this case.
- A match attempt that takes longer than one second faults in the same way.
- The engine compiles each distinct pair of pattern and options once and reuses it for every later evaluation. The cache keeps at most 256 pairs. When it is full, the next new pair clears it, and the pairs it held are compiled again when they are used.

## Related predicates

- [IsNotMatch](regex-isnotmatch.md) is the exact complement of this predicate.
- [StartsWith](string-startswith.md) tests a fixed prefix.
- [EndsWith](string-endswith.md) tests a fixed suffix.
- [Contains](string-contains.md) tests a fixed substring.
