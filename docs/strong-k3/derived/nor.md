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

Derived. `NOR` is defined as `NOT(OR(a, b))` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Exactly two operands. `NOR` is binary only. A chain with more operands (`a NOR b NOR c`) and a JSON or YAML node with any other operand count are compile errors, `InfixArityViolation` (`TRE0006`): "NOR is binary only; found N operands. Add parentheses (or nest NOR nodes) to say how chained operations group." `RuleBuilder` takes exactly two arguments, so a wrong count cannot be written there. See [Edge Cases](#edge-cases).

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

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

`NOR` is an infix operator with no call form: `NOR(a, b)` is a syntax error in the DSL. It sits outside the `NOT` > `AND` > `OR` precedence chain: `NOT` binds tighter, so `NOT a NOR b` is `(NOT a) NOR b`, and mixing `NOR` with `AND`, `OR` or another of `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `??` or the ternary at one level without parentheses is the compile error `AmbiguousOperatorMixing` (`TRE0007`). The canonical printer writes the word form.

In this reference the function-call spelling `NOR(a, b)` is only a plain-text convention for tables and canonical forms ([notation](../specification/notation.md#code-conventions)). It is not DSL input.

## Aliases

| Alias | Kind |
| --- | --- |
| `↓` | Symbol |
| `⊽` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive.

## Formal Semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `NOR` is the negation of the maximum, which is the minimum of the negated operands. A single `True` operand settles it as `False`.

## Formula

$$a \downarrow b = \neg(a \lor b) = \neg a \land \neg b = \min(\neg a, \neg b)$$

## Truth Table

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

## Canonical Form

<!-- k3:canonical NOR vars=a,b -->
```text
NOT(OR(a, b))
```

## Equivalent Forms

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

`NOR` is a universal gate for the Strong Kleene connectives, like [NAND](nand.md), and `ExpandToNor` performs that rewrite ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)). The external operators `COALESCE` and the inspections cannot be expressed with it.

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isBanned NOR isSuspended` | `False`, `False` | `True` | Neither holds. |
| `isBanned NOR isSuspended` | `True`, `Unknown` | `False` | One `True` settles the disjunction as `True`. |
| `isBanned NOR isSuspended` | `False`, `Unknown` | `Unknown` | The disjunction is open. |
| `isBanned NOR isSuspended` | `Unknown`, `Unknown` | `Unknown` | The disjunction is open. |

## Edge Cases

- **More than two operands.** A chain, or a JSON or YAML node with any other operand count, is rejected with `TRE0006`; group with parentheses. `NOR` is not associative, so no grouping is implied.
- **True dominates, Unknown does not.** `NOR(T, x)` is `F` for every `x`; `NOR(U, x)` is `F` only when `x` is `T`. For "none of several" use `NONE(...)`.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom NOR isOn` | `False` | 1 |
| `boom NOR isOff` | `Unknown` | 1 |

## Implementation Notes

- The evaluator reports a `NOR` node as `NOR` in the trace and the evaluated tree.
- Compression rewrites `NOT(OR(a, b))` and `AND(NOT a, NOT b)` to `NOR` ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)).

## Related Operations

- [NAND](nand.md) is the dual: it negates `AND`.
- [OR](../gates/or.md) and [NOT](../gates/not.md) define it.
- `NONE` in the [Cardinality Functions](../cardinality/README.md) equals it as a value for two operands and accepts more.
