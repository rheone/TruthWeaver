# NOT

Negation: reverses `True` and `False` and leaves `Unknown` alone. Back to the [Gates / Operators index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `NOT`
- DSL: `NOT` (also `!` and `¬`, see [Aliases](#aliases))
- JSON and YAML `op`: `not`
- `RuleBuilder` member: `RuleBuilder.Not`

## Classification

- Category: Gates / Operators
- Category index: [Gates / Operators](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `NOT` has no definition in other Operations. It is one of the three connectives from which the others are defined ([semantics](../specification/semantics.md#the-three-primitive-connectives)).

## Arity

Exactly one operand. In JSON and YAML any other count is a compile error, `InfixArityViolation` (`TRE0006`, see [diagnostics](../specification/diagnostics.md)). In the DSL the operand is whatever follows the prefix, so the count cannot be wrong.

## Input domain

The operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`NOT a` is `True` when `a` is `False`, `False` when `a` is `True`, and `Unknown` when `a` is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `NOT a` |
| DSL, symbols | `!a`, `¬a` |
| JSON | `{"op": "not", "operands": [ <expression> ]}` |
| YAML | `op: not` with a one-item `operands:` list |
| `RuleBuilder` | `RuleBuilder.Not(operand)` |

`NOT` is a prefix operator. It binds tighter than every other operator and is right-associative, so `NOT a AND b` is `(NOT a) AND b` and `NOT NOT a` is valid. There is no call form: `NOT(a)` parses as `NOT` applied to the parenthesised group `(a)`, with the same result as `NOT a`. The canonical printer writes the word form, so `!a` and `¬a` print as `NOT a`.

## Aliases

| Alias | Kind |
| --- | --- |
| `!` | Symbol |
| `¬` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive (`not`, `Not`, `NOT`). No other spelling is accepted: `~` is a syntax error.

## Formal semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, negation reverses the order and fixes its middle element.

## Formula

$$\neg a = \begin{cases} \mathsf{F} & \text{if } a = \mathsf{T} \\ \mathsf{U} & \text{if } a = \mathsf{U} \\ \mathsf{T} & \text{if } a = \mathsf{F} \end{cases}$$

The plain-text form is `NOT(a)`, following the [notation](../specification/notation.md#code-conventions) conventions.

## Truth table

<!-- k3:truth NOT -->
| a | NOT(a) |
| --- | --- |
| T | F |
| U | U |
| F | T |

`NOT` is its own inverse on every value, so $\neg\neg a = a$ holds for `Unknown` too.

## Equivalent forms

`NOT` can be written with `NAND` or with `NOR` by repeating the operand. This is a fact about those Operations, not a definition of `NOT`:

<!-- k3:canonical NOT vars=a -->
```text
NAND(a, a)
```

<!-- k3:canonical NOT vars=a -->
```text
NOR(a, a)
```

Three negations equal one:

<!-- k3:canonical NOT vars=a -->
```text
NOT(NOT(NOT(a)))
```

The classical laws that pair a value with its negation fail, because `NOT(U)` is `U` ([laws that fail](../specification/semantics.md#laws-that-fail)). In particular `a OR NOT a` is `U` when `a` is `U`, so it is not a tautology. De Morgan's laws, which relate `NOT` to `AND` and `OR`, hold ([laws that hold](../specification/semantics.md#laws-that-hold)).

## Examples

| Rule | State of `isBanned` | Result |
| --- | --- | --- |
| `NOT isBanned` | `True` | `False` |
| `NOT isBanned` | `False` | `True` |
| `NOT isBanned` | `Unknown` | `Unknown` |
| `NOT NOT isBanned` | `Unknown` | `Unknown` |

An unknown ban status does not grant access: `NOT isBanned` stays `Unknown`, and `Decision.IsSatisfied` is `True` only for a `True` result.

## Edge cases

- Unknown propagates. `NOT` never turns `Unknown` into a definite value. That is what makes it safe in a permission rule: an unanswerable predicate cannot become a grant.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). `NOT` of it is `Unknown`, with the fault on the decision. The engine does not coerce a fault to `False` before `NOT`, because that would produce `True`.
- Nesting. Each `NOT` is a separate node: `NOT NOT NOT a` is a chain of three, evaluated outward from `a`.

## Evaluation behavior

- The operand always runs.
- The analyzer does not report `a AND NOT a` as a contradiction or `a OR NOT a` as a tautology.

## Related operations

- [AND](and.md) and [OR](or.md) complete the primitive connectives; De Morgan's laws relate the three.
- The [derived logical operations](../derived/README.md) `NAND`, `NOR` and `IMPLIES` are defined using `NOT`.
- The inspections in [Functions](../functions/README.md) test for `Unknown` itself; `NOT` cannot.
