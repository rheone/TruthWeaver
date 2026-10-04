# IsFalse

`True` when the operand is `False`, and `False` when it is `True` or `Unknown`. An external operator, not a Strong Kleene connective. Back to the [Functions index](README.md); shared rules are in the [specification](../specification/README.md).

> [!WARNING]
> `IsFalse` is an **external operator**. It is not monotone in the information order: `IsFalse(Unknown)` is `False` but `IsFalse(False)` is `True`, so refining the operand from `Unknown` to `False` changed a definite answer. Do not assume the Strong Kleene laws (the no-tautology theorem, `NAND` and `NOR` expressiveness, monotone rewrites) hold for a rule that contains it.

## Name

- Canonical name: `IsFalse`
- DSL: `IsFalse(a)`
- JSON and YAML `op`: `isFalse`
- `RuleBuilder` member: `RuleBuilder.IsFalse`

## Classification

- Category: Functions
- Category index: [Functions](README.md)
- An external operator, not a Strong Kleene connective: it is not monotone in the [information order](../specification/values.md#information-order), because it answers something definite because its operand is `Unknown`. The [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions) and the `NAND`-only and `NOR`-only rewrites do not extend to it ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

> [!NOTE]
> The inspections are Derived (they expand to [COALESCE](coalesce.md)) and external, which puts them in Functions rather than next to the connectives ([proposal](../PROPOSAL.md#4-inspections-derived-but-external)). The owner accepted the recommendation. The classification stays on the unresolved-questions list for the validation report.

## Kind

Derived. `IsFalse` is defined from `COALESCE` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Exactly one operand. `IsFalse(a, b)` and `IsFalse()` in the DSL, and a JSON or YAML node with another operand count, are the compile error `MalformedTree` (`TRE0014`): "IsFalse requires exactly 1 operand but found N." `RuleBuilder.IsFalse` takes one argument, so a wrong count cannot be written there.

## Input Domain

The operand is a value in `{T, F, U}`.

## Output Domain

`{T, F}`. The result is never `Unknown`, whatever the operand is, and also when the operand is a faulting term.

## Definition

`IsFalse(a)` is `True` when `a` is `False` and `False` otherwise, so an `Unknown` operand gives `False`. It is the SQL `a IS FALSE`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `IsFalse(a)` |
| JSON | `{"op": "isFalse", "operands": [ <expression> ]}` |
| YAML | `op: isFalse` with an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.IsFalse(operand)` |

`IsFalse` is a reserved word in any letter case (`isfalse(a)`), so a predicate cannot be named `IsFalse`. It has only the call form: `IsFalse a` without parentheses is a syntax error (`TRE0001`). A call has no precedence, so `NOT IsFalse(a)` and `IsFalse(a) AND b` compile as written.

## Aliases

None. The word is case-insensitive in the DSL, and the JSON and YAML `op` is case-insensitive on read. There is no symbol spelling, and every printer keeps the word.

## Formal Semantics

`IsFalse` is the characteristic function of the value `False`: it asks whether the operand is definitely false. It maps `Unknown` to `False`, which no Strong Kleene connective can do.

## Formula

$$\operatorname{IsFalse}(a) = \begin{cases} \mathsf{T} & \text{if } a = \mathsf{F} \\ \mathsf{F} & \text{otherwise} \end{cases}$$

## Truth Table

Only a `False` operand gives `T`; both `U` and `T` give `F`.

<!-- k3:truth IsFalse -->
| a | IsFalse(a) |
| --- | --- |
| T | F |
| U | F |
| F | T |

## Canonical Form

`IsFalse` is defined from `COALESCE`, the one operation that can observe `Unknown`:

<!-- k3:canonical IsFalse vars=a -->
```text
COALESCE(NOT(a), False)
```

## Equivalent Forms

`IsFalse` is `IsTrue` of the negation. Unlike `NOT a`, which stays `Unknown` for an `Unknown` `a`, `IsFalse(a)` is `False` there.

<!-- k3:canonical IsFalse vars=a -->
```text
IsTrue(NOT(a))
```

`IsFalse(a)` is not `NOT IsTrue(a)`: for `Unknown` the first is `False` and the second is `True`. Together with `IsTrue`, it covers the two definite values:

<!-- k3:canonical IsKnown vars=a -->
```text
OR(IsTrue(a), IsFalse(a))
```

## Examples

| Rule | Operand value | Result | Why |
| --- | --- | --- | --- |
| `IsFalse(canEdit)` | `False` | `True` | The operand is definitely false. |
| `IsFalse(canEdit)` | `True` | `False` | The operand is true. |
| `IsFalse(canEdit)` | `Unknown` | `False` | An unresolved answer is not a definite no. |

## Edge Cases

- **Never `Unknown`.** `IsFalse(Unknown)` is `False`, not `Unknown`. `IsFalse` is not the negation of `IsTrue`: both are `False` for `Unknown`.
- **A faulting operand.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)); `IsFalse` still answers definitely, and the fault is kept. With `boom` a term that faults, `IsFalse(boom)` is `False` with 1 fault recorded.
- **No short-circuit question.** There is one operand, so it is always evaluated.

## Implementation Notes

- The evaluator reports a `IsFalse` node as `IsFalse` in the trace and the evaluated tree. The four inspections are one node type distinguished by the kind they test.
- `ExpandToPrimitives` expands `IsFalse` to `COALESCE(NOT a, False)`. `ExpandToNand` and `ExpandToNor` keep the `COALESCE` in that expansion, because no `NAND` or `NOR` circuit can express it.
- `CompressToDerived` writes `COALESCE(NOT a, False)` back as `IsFalse(a)`.

## Related Operations

- [COALESCE](coalesce.md) defines it.
- [IsTrue](istrue.md), [IsUnknown](isunknown.md) and [IsKnown](isknown.md) are the other inspections.
- [NOT](../gates/not.md): `NOT` keeps `Unknown`, while `IsFalse` does not.
- [Result transformations](../result-transformations/README.md): `Decision.Project` and `Decision.Collapse` act on the final result, outside the rule.
