# Type IsNotUrl

`IsNotUrl` is `True` when the selected value is not an absolute `http` or `https` URL. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsNotUrl`
- Default label: `Is Not Url`
- Arguments: none
- Rule text: `websiteIsNotUrl`. The host chooses the name `websiteIsNotUrl` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsUrl](type-isurl.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the selected value is not a URL. `False` when it is a URL. Here a URL has the meaning of [IsUrl](type-isurl.md): `True` when the text is an absolute URI whose scheme is `http` or `https`. A relative reference and any other scheme are rejected. With an `object` selector, a `Uri` instance that is absolute and has one of these schemes is also `True`. `False` otherwise.

## Answers

The table shows the rule `websiteIsNotUrl`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"https://example.com/a?q=1"` | `False` |
| String | `"HTTP://EXAMPLE.COM"` | `False` |
| String | `"example.com"` | `True` |
| String | `"/relative/path"` | `True` |
| String | `"ftp://example.com"` | `True` |
| String | `"mailto:a@b.c"` | `True` |
| String | `""` | `True` |
| String | `null` | `Unknown` |
| Object | `Uri` for `https://example.com` | `False` |
| Object | `Uri` for `ftp://example.com` | `True` |
| Object | `relative `Uri`` | `True` |
| Object | `5` (an `int`) | `True` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `websiteIsNotUrl` | `website = "https://example.com"` (string selector) | `False` | The text is an absolute `https` URL. The twin gives the opposite answer. |
| `websiteIsNotUrl` | `website = "example.com"` (string selector) | `True` | The text has no scheme, so it is not an absolute URL. The twin gives the opposite answer. |
| `websiteIsNotUrl` | `website = "ftp://example.com"` (string selector) | `True` | The scheme is not `http` or `https`. The twin gives the opposite answer. |
| `websiteIsNotUrl` | `website = null` | `Unknown` | A null selected value is `Unknown`. |

## Edge cases

- The scheme test ignores case, so `HTTP://EXAMPLE.COM` is a URL.
- A bare host name has no scheme and is not an absolute URL.

## Related predicates

- [IsUrl](type-isurl.md) is the exact complement of this predicate.
- [NotStartsWith](string-notstartswith.md) tests a string prefix, for example `"https://"`.
- [NotMatches](regex-notmatches.md) tests text against your own pattern.
