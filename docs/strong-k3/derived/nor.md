# NOR

Negated disjunction, `NOT(a OR b)`: `True` only when both operands are `False`, `False` as soon as either is `True`, and `Unknown` otherwise. Back to the [Derived Logical Operations index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `NOR`
- DSL: `NOR` (also `↓` and `⊽`, see [Aliases](#aliases))
- JSON and YAML `op`: `nor`
- `RuleBuilder` member: `RuleBuilder.Nor`

## Classification

- Category: Derived Logical Operations
- Category index: [Derived Logical Operations](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `NOR` is defined as `NOT(OR(a, b))` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Exactly two operands. `NOR` is binary only. A chain with more operands (`a NOR b NOR c`) and a JSON or YAML node with any other operand count are compile errors, `InfixArityViolation` (`TRE0006`, see [diagnostics](../specification/diagnostics.md)). `RuleBuilder` takes exactly two arguments, so a wrong count cannot be written there. See [Edge cases](#edge-cases).

## Input domain

Each operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`a NOR b` is `True` when both operands are `False`, `False` when at least one operand is `True`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a NOR b` |
| DSL, symbols | `a ↓ b`, `a ⊽ b` |
| JSON | `{"op": "nor", "operands": [ <expression>, <expression> ]}` |
| YAML | `op: nor` with an `operands:` list of exactly two items |
| `RuleBuilder` | `RuleBuilder.Nor(left, right)` |

`NOR` is an infix operator with no call form: `NOR(a, b)` is a syntax error. It sits outside the `NOT`, `AND`, `OR` precedence chain, so it needs parentheses to combine with another operator at the same level. Precedence, the mixing rule and the printed form are in [syntax](../specification/syntax.md#precedence-and-grouping).

## Aliases

| Alias | Kind |
| --- | --- |
| `↓` | Symbol |
| `⊽` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive.

## Formal semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `NOR` is the negation of the maximum, which is the minimum of the negated operands. A single `True` operand settles it as `False`.

## Formula

$$a \downarrow b = \neg(a \lor b) = \neg a \land \neg b = \min(\neg a, \neg b)$$

## Truth table

<!-- k3:truth NOR -->
| a | b | NOR(a, b) |
| --- | --- | --- |
| T | T | F |
| T | U | F |
| T | F | F |
| U | T | F |
| U | U | U |
| U | F | U |
| F | T | F |
| F | U | U |
| F | F | T |

## Canonical form

<!-- k3:canonical NOR vars=a,b -->
```text
NOT(OR(a, b))
```

## Equivalent forms

De Morgan's law gives the conjunction of the negations:

<!-- k3:canonical NOR vars=a,b -->
```text
AND(NOT(a), NOT(b))
```

As a value, `NOR` equals the two-operand `NONE` of the [Cardinality Functions](../cardinality/README.md). They stay distinct Operations, because `NONE` is a cardinality operation and keeps its own node when a rule round-trips:

<!-- k3:canonical NOR vars=a,b -->
```text
NONE(a, b)
```

Repeating one operand gives `NOT`: `NOR(a, a)` is `NOT(a)` (see [NOT](../gates/not.md#equivalent-forms)). `NOR` is commutative and not associative, which is one reason a chain is rejected.

`NOR` is a universal gate for the Strong Kleene connectives, like [NAND](nand.md), and `ExpandToNor` performs that rewrite. The external operators `COALESCE` and the inspections cannot be expressed with it.

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isBanned NOR isSuspended` | `False`, `False` | `True` | Neither holds. |
| `isBanned NOR isSuspended` | `True`, `Unknown` | `False` | One `True` settles the disjunction as `True`. |
| `isBanned NOR isSuspended` | `False`, `Unknown` | `Unknown` | The disjunction is open. |
| `isBanned NOR isSuspended` | `Unknown`, `Unknown` | `Unknown` | The disjunction is open. |

## Edge cases

- More than two operands. A chain, or a JSON or YAML node with any other operand count, is rejected with `TRE0006`; group with parentheses. `NOR` is not associative, so no grouping is implied.
- True dominates, Unknown does not. `NOR(T, x)` is `F` for every `x`; `NOR(U, x)` is `F` only when `x` is `T`. For "none of several" use `NONE(...)`.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom NOR isOn` | `False` | 1 |
| `boom NOR isOff` | `Unknown` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- `CompressToDerived` rewrites `NOT(OR(a, b))` and `AND(NOT a, NOT b)` to `NOR`.

## Related operations

- [NAND](nand.md) is the dual: it negates `AND`.
- [OR](../gates/or.md) and [NOT](../gates/not.md) define it.
- `NONE` in the [Cardinality Functions](../cardinality/README.md) equals it as a value for two operands and accepts more.
