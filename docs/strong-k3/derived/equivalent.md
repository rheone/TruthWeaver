# EQUIVALENT

Biconditional: `True` when the two operands are the same definite value, `False` when they are different definite values, and `Unknown` when either is `Unknown`. Back to the [Derived Logical Operations index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `EQUIVALENT`
- DSL: `EQUIVALENT` (also `IFF`, `XNOR`, `↔` and `⇔`, see [Aliases](#aliases))
- JSON and YAML `op`: `equivalent` (`iff` and `xnor` are accepted on read)
- `RuleBuilder` member: `RuleBuilder.Equivalent` (`RuleBuilder.Xnor` forwards to it)

## Classification

- Category: Derived Logical Operations
- Category index: [Derived Logical Operations](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `EQUIVALENT` is defined from `AND`, `OR` and `NOT` (see [Canonical Form](#canonical-form)); it is also the negation of [XOR](xor.md). It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Exactly two operands. `EQUIVALENT` is binary only. A chain with more operands (`a EQUIVALENT b EQUIVALENT c`) and a JSON or YAML node with any other operand count are compile errors, `InfixArityViolation` (`BRE0006`): "EQUIVALENT is binary only; found N operands. Add parentheses (or nest EQUIVALENT nodes) to say how chained equivalences group." `RuleBuilder` takes exactly two arguments, so a wrong count cannot be written there. See [Edge Cases](#edge-cases).

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`a EQUIVALENT b` is `True` when `a` and `b` are both `True` or both `False`, `False` when one is `True` and the other `False`, and `Unknown` when either is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a EQUIVALENT b` |
| DSL, other | `a IFF b`, `a XNOR b`, `a ↔ b`, `a ⇔ b` |
| JSON | `{"op": "equivalent", "operands": [ <expression>, <expression> ]}` |
| YAML | `op: equivalent` with an `operands:` list of exactly two items |
| `RuleBuilder` | `RuleBuilder.Equivalent(left, right)` |

`EQUIVALENT` is an infix operator with no call form: `EQUIVALENT(a, b)` is a syntax error in the DSL. It sits outside the `NOT` > `AND` > `OR` precedence chain: `NOT` binds tighter, so `NOT a EQUIVALENT b` is `(NOT a) EQUIVALENT b`, and mixing `EQUIVALENT` with `AND`, `OR` or another of `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `??` or the ternary at one level without parentheses is the compile error `AmbiguousOperatorMixing` (`BRE0007`). The canonical printer writes the word form.

In this reference the function-call spelling `EQUIVALENT(a, b)` is only a plain-text convention for tables and canonical forms ([notation](../specification/notation.md#code-conventions)). It is not DSL input.

## Aliases

| Alias | Kind |
| --- | --- |
| `IFF` | Word |
| `XNOR` | Word, the name used before [ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 5 |
| `↔` | Symbol |
| `⇔` | Symbol |

Every alias compiles to the same node as `EQUIVALENT`, so persisted rules written with `XNOR` keep compiling, and the canonical printer writes `EQUIVALENT`. In JSON and YAML `iff` and `xnor` are accepted as the `op`. `RuleBuilder.Xnor` forwards to `RuleBuilder.Equivalent`. The ASCII spelling `<=>` is not accepted.

## Formal Semantics

`EQUIVALENT` is the strongest extension of the Boolean biconditional ([semantics](../specification/semantics.md#truth-functional-evaluation-and-the-strongest-extension)): it is definite only when both operands are, because refining either `Unknown` operand can change the answer.

## Formula

$$a \leftrightarrow b = (a \land b) \lor (\neg a \land \neg b) = \neg(a \oplus b)$$

When both operands are definite this is $\mathsf{T}$ if $a = b$ and $\mathsf{F}$ if $a \ne b$; if either is $\mathsf{U}$ the result is $\mathsf{U}$.

## Truth Table

<!-- k3:truth EQUIVALENT -->
| a | b | EQUIVALENT(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | U |
| T | F | F |
| U | T | U |
| U | U | U |
| U | F | U |
| F | T | F |
| F | U | U |
| F | F | T |

## Canonical Form

The definition in primitives is the disjunction of the two ways the operands can agree:

<!-- k3:canonical EQUIVALENT vars=a,b -->
```text
OR(AND(a, b), AND(NOT(a), NOT(b)))
```

## Equivalent Forms

`EQUIVALENT` is the negation of [XOR](xor.md), and the conjunction of the implication in both directions:

<!-- k3:canonical EQUIVALENT vars=a,b -->
```text
NOT(XOR(a, b))
```

<!-- k3:canonical EQUIVALENT vars=a,b -->
```text
AND(IMPLIES(a, b), IMPLIES(b, a))
```

`EQUIVALENT` is commutative and associative, `a EQUIVALENT True` is `a` and `a EQUIVALENT False` is `NOT a`. The classical law `a EQUIVALENT a = True` fails at `a = U`; see [laws that fail](../specification/semantics.md#laws-that-fail).

## Mermaid Diagram

The composition of the canonical form: the upper `AND` is the case where both operands are `True`, the lower one the case where both are `False`.

```mermaid
flowchart LR
    a --> A1["AND"]
    b --> A1
    a --> N1["NOT"] --> A2["AND"]
    b --> N2["NOT"] --> A2
    A1 --> O["OR"]
    A2 --> O
    O --> R["a EQUIVALENT b"]
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isActive EQUIVALENT isVerified` | `True`, `True` | `True` | Both `True`. |
| `isActive EQUIVALENT isVerified` | `False`, `False` | `True` | Both `False` also agree. |
| `isActive EQUIVALENT isVerified` | `True`, `False` | `False` | They differ. |
| `isActive EQUIVALENT isVerified` | `Unknown`, `Unknown` | `Unknown` | Not `True`: two unknowns are not known to agree. |

## Edge Cases

- **More than two operands.** `a EQUIVALENT b EQUIVALENT c` (in any spelling, and the same node in JSON or YAML) is rejected with `BRE0006` and a hint to add parentheses. A chain of biconditionals reads two ways in everyday speech, so it is never silently grouped. Nest explicitly, `(a EQUIVALENT b) EQUIVALENT c`.
- **Unknown propagates.** One `Unknown` operand makes the result `Unknown`, whatever the other is.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom EQUIVALENT isOff` | `Unknown` | 1 |

## Implementation Notes

- The evaluator reports an `EQUIVALENT` node as `EQUIVALENT` in the trace and the evaluated tree, whichever spelling the rule used.
- `a EQUIVALENT a` is not reported as a tautology: the no-tautology theorem covers it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 17).

## Related Operations

- [XOR](xor.md) is its negation.
- [IMPLIES](implies.md) is one direction of it.
- [NOT](../gates/not.md), [AND](../gates/and.md) and [OR](../gates/or.md) define it.
