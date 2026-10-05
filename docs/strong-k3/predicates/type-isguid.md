# Type IsGuid

`IsGuid` is `True` when the selected value is a GUID. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsGuid`
- Default label: `Is Guid`
- Arguments: none
- Rule text: `idIsGuid`. The host chooses the name `idIsGuid` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsNotGuid](type-isnotguid.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the text is accepted by `Guid.TryParse`. The accepted forms are the 32-digit form, the hyphenated form, the form in braces, the form in parentheses and the hexadecimal-struct form. With an `object` selector, a `Guid` instance is also `True`. `False` otherwise.

## Answers

The table shows the rule `idIsGuid`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f"` | `True` |
| String | `"d3b07384d9a34c1e8f3b0a1b2c3d4e5f"` | `True` |
| String | `"{d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f}"` | `True` |
| String | `"(d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f)"` | `True` |
| String | `"{0xd3b07384,0xd9a3,0x4c1e,{0x8f,0x3b,0x0a,0x1b,0x2c,0x3d,0x4e,0x5f}}"` | `True` |
| String | `"not-a-guid"` | `False` |
| String | `"d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5"` | `False` |
| String | `""` | `False` |
| String | `null` | `Unknown` |
| Object | `Guid` instance | `True` |
| Object | `"d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f"` (a `string`) | `True` |
| Object | `5` (an `int`) | `False` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `idIsGuid` | `id = "d3b07384-d9a3-4c1e-8f3b-0a1b2c3d4e5f"` (string selector) | `True` | The text is a hyphenated GUID. |
| `idIsGuid` | `id = "hello"` (string selector) | `False` | The text is not a GUID. |
| `idIsGuid` | `id = Guid.NewGuid()` (object selector) | `True` | A `Guid` instance counts for the `object` selector. |
| `NOT idIsGuid` | `id = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The text must be a whole GUID. This predicate does not trim surrounding spaces.
- The empty string is not a GUID. It answers `False`, not `Unknown`.

## Related predicates

- [IsNotGuid](type-isnotguid.md) is the exact complement of this predicate.
- [Equal](scalar-equal.md) tests a `Guid` selector for equality with a `Guid` literal.
- [IsString](type-isstring.md) tests for a string.
