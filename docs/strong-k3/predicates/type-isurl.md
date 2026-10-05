# Type IsUrl

`IsUrl` is `True` when the selected value is an absolute `http` or `https` URL. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsUrl`
- Default label: `Is Url`
- Arguments: none
- Rule text: `websiteIsUrl`. The host chooses the name `websiteIsUrl` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsNotUrl](type-isnoturl.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the text is an absolute URI whose scheme is `http` or `https`. A relative reference and any other scheme are rejected. With an `object` selector, a `Uri` instance that is absolute and has one of these schemes is also `True`. `False` otherwise.

## Answers

The table shows the rule `websiteIsUrl`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"https://example.com/a?q=1"` | `True` |
| String | `"HTTP://EXAMPLE.COM"` | `True` |
| String | `"example.com"` | `False` |
| String | `"/relative/path"` | `False` |
| String | `"ftp://example.com"` | `False` |
| String | `"mailto:a@b.c"` | `False` |
| String | `""` | `False` |
| String | `null` | `Unknown` |
| Object | `Uri` for `https://example.com` | `True` |
| Object | `Uri` for `ftp://example.com` | `False` |
| Object | `relative `Uri`` | `False` |
| Object | `5` (an `int`) | `False` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `websiteIsUrl` | `website = "https://example.com"` (string selector) | `True` | The text is an absolute `https` URL. |
| `websiteIsUrl` | `website = "example.com"` (string selector) | `False` | The text has no scheme, so it is not an absolute URL. |
| `websiteIsUrl` | `website = "ftp://example.com"` (string selector) | `False` | The scheme is not `http` or `https`. |
| `NOT websiteIsUrl` | `website = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The scheme test ignores case, so `HTTP://EXAMPLE.COM` is a URL.
- A bare host name has no scheme and is not an absolute URL.

## Related predicates

- [IsNotUrl](type-isnoturl.md) is the exact complement of this predicate.
- [StartsWith](string-startswith.md) tests a string prefix, for example `"https://"`.
- [Matches](regex-matches.md) tests text against your own pattern.
