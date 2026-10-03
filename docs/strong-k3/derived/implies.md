# IMPLIES

Strong Kleene material implication, `NOT a OR b`: `True` when the antecedent is `False` or the consequent is `True`, `False` only when the antecedent is `True` and the consequent `False`. Back to the [Derived Logical Operations index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `IMPLIES`
- DSL: `IMPLIES` (also `→` and `⇒`, see [Aliases](#aliases))
- JSON and YAML `op`: `implies`
- `RuleBuilder` member: `RuleBuilder.Implies`

## Classification

- Category: Derived Logical Operations
- Category index: [Derived Logical Operations](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `IMPLIES` is defined as `NOT a OR b` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Exactly two operands. `IMPLIES` is binary only. A chain with more operands (`a IMPLIES b IMPLIES c`) and a JSON or YAML node with any other operand count are compile errors, `InfixArityViolation` (`BRE0006`): "IMPLIES is binary only; found N operands. Add parentheses (or nest IMPLIES nodes) to say how chained implications group." `RuleBuilder` takes exactly two arguments, so a wrong count cannot be written there. See [Edge Cases](#edge-cases).

The first operand is the antecedent (the "if") and the second the consequent (the "then").

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`a IMPLIES b` is `True` when `a` is `False` or `b` is `True`, `False` when `a` is `True` and `b` is `False`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a IMPLIES b` |
| DSL, symbols | `a → b`, `a ⇒ b` |
| JSON | `{"op": "implies", "operands": [ <antecedent>, <consequent> ]}` |
| YAML | `op: implies` with an `operands:` list of exactly two items |
| `RuleBuilder` | `RuleBuilder.Implies(antecedent, consequent)` |

`IMPLIES` is an infix operator with no call form: `IMPLIES(a, b)` is a syntax error in the DSL. It sits outside the `NOT` > `AND` > `OR` precedence chain: `NOT` binds tighter, so `NOT a IMPLIES b` is `(NOT a) IMPLIES b`, and mixing `IMPLIES` with `AND`, `OR` or another of `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `??` or the ternary at one level without parentheses is the compile error `AmbiguousOperatorMixing` (`BRE0007`). The canonical printer writes the word form.

In this reference the function-call spelling `IMPLIES(a, b)` is only a plain-text convention for tables and canonical forms ([notation](../specification/notation.md#code-conventions)). It is not DSL input.

Because implication is not associative, the DSL never groups a chain for you: `a IMPLIES b IMPLIES c` is the compile error `BRE0006`. Write `a IMPLIES (b IMPLIES c)` or `(a IMPLIES b) IMPLIES c`.

## Aliases

| Alias | Kind |
| --- | --- |
| `→` | Symbol |
| `⇒` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive. The ASCII spellings `->` and `=>` are not accepted.

## Formal Semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `IMPLIES` is the maximum of the negated antecedent and the consequent. It is Kleene's strong implication, the strongest extension of the Boolean implication.

## Formula

$$a \to b = \neg a \lor b = \max(\neg a, b)$$

## Truth Table

A `False` antecedent or a `True` consequent settles the result as `T`, even when the other operand is `Unknown`. An unknown antecedent with an unknown consequent stays `Unknown`.

<!-- k3:truth IMPLIES -->
| a | b | IMPLIES(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | U |
| T | F | F |
| U | T | T |
| U | U | U |
| U | F | U |
| F | T | T |
| F | U | T |
| F | F | T |

## Canonical Form

<!-- k3:canonical IMPLIES vars=a,b -->
```text
OR(NOT(a), b)
```

## Equivalent Forms

Contraposition holds, and the implication is the negated conjunction of the antecedent with the negated consequent:

<!-- k3:canonical IMPLIES vars=a,b -->
```text
IMPLIES(NOT(b), NOT(a))
```

<!-- k3:canonical IMPLIES vars=a,b -->
```text
NOT(AND(a, NOT(b)))
```

<!-- k3:canonical IMPLIES vars=a,b -->
```text
NAND(a, NOT(b))
```

`IMPLIES` is neither commutative nor associative. `True IMPLIES b` is `b`, `False IMPLIES b` is `True`, `a IMPLIES True` is `True` and `a IMPLIES False` is `NOT a`. The identity `a IMPLIES a = True` fails at `a = U`; see [laws that fail](../specification/semantics.md#laws-that-fail).

### Kleene versus Lukasiewicz

Lukasiewicz's three-valued logic defines implication differently: with `Unknown` as one half, $a \to b = \min(1, 1 - a + b)$. The two agree on every row of the table except one. In Lukasiewicz logic `U IMPLIES U` is `T`; in Strong Kleene logic, and in TruthWeaver, it is `U`.

| a | b | Kleene (`IMPLIES`) | Lukasiewicz |
| --- | --- | --- | --- |
| U | U | U | T |

TruthWeaver is Kleene: `IMPLIES` is `OR(NOT(a), b)`, so it stays among the connectives built from `NOT`, `AND` and `OR` and keeps the [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isAdmin IMPLIES canDelete` | `True`, `True` | `True` | The antecedent holds and so does the consequent. |
| `isAdmin IMPLIES canDelete` | `True`, `False` | `False` | The only way to break an implication. |
| `isAdmin IMPLIES canDelete` | `False`, `Unknown` | `True` | An antecedent that fails is vacuous, whatever the consequent. |
| `isAdmin IMPLIES canDelete` | `Unknown`, `True` | `True` | A true consequent settles it whatever the antecedent. |
| `isAdmin IMPLIES canDelete` | `Unknown`, `Unknown` | `Unknown` | Neither side is known. |

## Edge Cases

- **More than two operands.** A chain, or a JSON or YAML node with any other operand count, is rejected with `BRE0006`; group with parentheses.
- **Operand order matters.** `a IMPLIES b` and `b IMPLIES a` differ. Swapping operands needs `NOT` on both, as in contraposition.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault. A `False` antecedent already gives `True`, but the consequent is still run.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom IMPLIES isOn` | `True` | 1 |
| `isOff IMPLIES boom` | `True` | 1, because `boom` still runs |
| `boom IMPLIES isOff` | `Unknown` | 1 |
| `isOn IMPLIES boom` | `Unknown` | 1 |

## Implementation Notes

- The evaluator reports an `IMPLIES` node as `IMPLIES` in the trace and the evaluated tree.
- Compression rewrites an `OR(NOT a, b)` shape to `IMPLIES`, and `ExpandToPrimitives` expands `IMPLIES` back ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)).

## Related Operations

- [OR](../gates/or.md) and [NOT](../gates/not.md) define it.
- [EQUIVALENT](equivalent.md) is the conjunction of the implication in both directions.
- [NAND](nand.md): `a IMPLIES b` is `NAND(a, NOT(b))`.
