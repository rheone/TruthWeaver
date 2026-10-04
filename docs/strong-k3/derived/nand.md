# NAND

Negated conjunction, `NOT(a AND b)`: `False` only when both operands are `True`, `True` as soon as either is `False`, and `Unknown` otherwise. Back to the [Derived Logical Operations index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `NAND`
- DSL: `NAND` (also `↑` and `⊼`, see [Aliases](#aliases))
- JSON and YAML `op`: `nand`
- `RuleBuilder` member: `RuleBuilder.Nand`

## Classification

- Category: Derived Logical Operations
- Category index: [Derived Logical Operations](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `NAND` is defined as `NOT(AND(a, b))` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Exactly two operands. `NAND` is binary only. A chain with more operands (`a NAND b NAND c`) and a JSON or YAML node with any other operand count are compile errors, `InfixArityViolation` (`TRE0006`, see [diagnostics](../specification/diagnostics.md)). `RuleBuilder` takes exactly two arguments, so a wrong count cannot be written there. See [Edge cases](#edge-cases).

## Input domain

Each operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`a NAND b` is `False` when both operands are `True`, `True` when at least one operand is `False`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a NAND b` |
| DSL, symbols | `a ↑ b`, `a ⊼ b` |
| JSON | `{"op": "nand", "operands": [ <expression>, <expression> ]}` |
| YAML | `op: nand` with an `operands:` list of exactly two items |
| `RuleBuilder` | `RuleBuilder.Nand(left, right)` |

`NAND` is an infix operator with no call form: `NAND(a, b)` is a syntax error. It sits outside the `NOT`, `AND`, `OR` precedence chain, so it needs parentheses to combine with another operator at the same level. Precedence, the mixing rule and the printed form are in [syntax](../specification/syntax.md#precedence-and-grouping).

## Aliases

| Alias | Kind |
| --- | --- |
| `↑` | Symbol |
| `⊼` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive.

## Formal semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `NAND` is the negation of the minimum, which is the maximum of the negated operands. A single `False` operand settles it as `True`.

## Formula

$$a \uparrow b = \neg(a \land b) = \neg a \lor \neg b = \max(\neg a, \neg b)$$

## Truth table

<!-- k3:truth NAND -->
| a | b | NAND(a, b) |
| --- | --- | --- |
| T | T | F |
| T | U | U |
| T | F | T |
| U | T | U |
| U | U | U |
| U | F | T |
| F | T | T |
| F | U | T |
| F | F | T |

## Canonical form

<!-- k3:canonical NAND vars=a,b -->
```text
NOT(AND(a, b))
```

## Equivalent forms

De Morgan's law gives the disjunction of the negations, and `NAND` is also an implication to a negation:

<!-- k3:canonical NAND vars=a,b -->
```text
OR(NOT(a), NOT(b))
```

<!-- k3:canonical NAND vars=a,b -->
```text
IMPLIES(a, NOT(b))
```

Repeating one operand gives `NOT`: `NAND(a, a)` is `NOT(a)` (see [NOT](../gates/not.md#equivalent-forms)). `NAND` is commutative and not associative, which is one reason a chain is rejected.

`NAND` is a universal gate for the Strong Kleene connectives: `NOT`, `AND`, `OR` and everything built from them can be written with `NAND` alone, and `ExpandToNand` performs that rewrite. The external operators `COALESCE` and the inspections cannot be, because every circuit of `NAND` is monotone in the information order and they are not.

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isOnline NAND isBusy` | `True`, `True` | `False` | Both hold, so the negated conjunction fails. |
| `isOnline NAND isBusy` | `False`, `Unknown` | `True` | One `False` settles the conjunction as `False`. |
| `isOnline NAND isBusy` | `True`, `Unknown` | `Unknown` | The conjunction is open. |
| `isOnline NAND isBusy` | `Unknown`, `Unknown` | `Unknown` | The conjunction is open. |

## Edge cases

- More than two operands. A chain, or a JSON or YAML node with any other operand count, is rejected with `TRE0006`; group with parentheses. `NAND` is not associative, so no grouping is implied.
- False dominates, Unknown does not. `NAND(F, x)` is `T` for every `x`; `NAND(U, x)` is `T` only when `x` is `F`.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom NAND isOff` | `True` | 1 |
| `boom NAND isOn` | `Unknown` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- `CompressToDerived` rewrites `NOT(AND(a, b))` and `OR(NOT a, NOT b)` to `NAND`.

## Related operations

- [NOR](nor.md) is the dual: it negates `OR`.
- [AND](../gates/and.md) and [NOT](../gates/not.md) define it.
- [IMPLIES](implies.md) is `NAND(a, NOT(b))`.
