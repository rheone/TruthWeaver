# IsKnown

`True` when the operand is `True` or `False`, and `False` when it is `Unknown`. An external operator, not a Strong Kleene connective. Back to the [Functions index](README.md); shared rules are in the [specification](../specification/README.md).

> [!WARNING]
> `IsKnown` is an **external operator**. It is not monotone in the information order: `IsKnown(Unknown)` is `False` but `IsKnown(True)` is `True`, so refining the operand from `Unknown` to `True` changed a definite answer. Do not assume the Strong Kleene laws (the no-tautology theorem, `NAND` and `NOR` expressiveness, monotone rewrites) hold for a rule that contains it.

## Name

- Canonical name: `IsKnown`
- DSL: `IsKnown(a)`
- JSON and YAML `op`: `isKnown`
- `RuleBuilder` member: `RuleBuilder.IsKnown`

## Classification

- Category: Functions
- Category index: [Functions](README.md)
- An external operator, not a Strong Kleene connective: it is not monotone in the [information order](../specification/values.md#information-order), because it answers something definite because its operand is `Unknown`. The [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions) and the `NAND`-only and `NOR`-only rewrites do not extend to it ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

> [!NOTE]
> The inspections are Derived (they expand to [COALESCE](coalesce.md)) and external, which puts them in Functions rather than next to the connectives ([proposal](../PROPOSAL.md#4-inspections-derived-but-external)). The owner accepted the recommendation. The classification stays on the unresolved-questions list for the validation report.

## Kind

Derived. `IsKnown` is defined from `COALESCE` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Exactly one operand. `IsKnown(a, b)` and `IsKnown()` in the DSL, and a JSON or YAML node with another operand count, are the compile error `MalformedTree` (`TRE0014`): "IsKnown requires exactly 1 operand but found N." `RuleBuilder.IsKnown` takes one argument, so a wrong count cannot be written there.

## Input Domain

The operand is a value in `{T, F, U}`.

## Output Domain

`{T, F}`. The result is never `Unknown`, whatever the operand is, and also when the operand is a faulting term.

## Definition

`IsKnown(a)` is `True` when `a` is `True` or `False` and `False` when `a` is `Unknown`. It is the SQL `a IS NOT UNKNOWN`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `IsKnown(a)` |
| JSON | `{"op": "isKnown", "operands": [ <expression> ]}` |
| YAML | `op: isKnown` with an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.IsKnown(operand)` |

`IsKnown` is a reserved word in any letter case (`isknown(a)`), so a predicate cannot be named `IsKnown`. It has only the call form: `IsKnown a` without parentheses is a syntax error (`TRE0001`). A call has no precedence, so `NOT IsKnown(a)` and `IsKnown(a) AND b` compile as written.

## Aliases

None. The word is case-insensitive in the DSL, and the JSON and YAML `op` is case-insensitive on read. There is no symbol spelling, and every printer keeps the word.

## Formal Semantics

`IsKnown` asks whether the operand is determined, in either direction. It maps `Unknown` to `False` and both definite values to `True`, which merges `True` and `False` and so cannot be monotone.

## Formula

$$\operatorname{IsKnown}(a) = \begin{cases} \mathsf{T} & \text{if } a \ne \mathsf{U} \\ \mathsf{F} & \text{if } a = \mathsf{U} \end{cases}$$

## Truth Table

Only an `Unknown` operand gives `F`.

<!-- k3:truth IsKnown -->
| a | IsKnown(a) |
| --- | --- |
| T | T |
| U | F |
| F | T |

## Canonical Form

`IsKnown` is defined from `COALESCE`, the one operation that can observe `Unknown`:

<!-- k3:canonical IsKnown vars=a -->
```text
OR(COALESCE(a, False), COALESCE(NOT(a), False))
```

## Equivalent Forms

`IsKnown` is the negation of `IsUnknown`, and the disjunction of the two definite inspections:

<!-- k3:canonical IsKnown vars=a -->
```text
NOT(IsUnknown(a))
```

<!-- k3:canonical IsKnown vars=a -->
```text
OR(IsTrue(a), IsFalse(a))
```

`IsKnown(IsKnown(a))` is always `True`, because the inner result is never `Unknown`; `Simplify` reduces it to `True`.

## Examples

| Rule | Operand value | Result | Why |
| --- | --- | --- | --- |
| `IsKnown(canEdit)` | `True` | `True` | The operand is determined. |
| `IsKnown(canEdit)` | `False` | `True` | A known `False` is still known. |
| `IsKnown(canEdit)` | `Unknown` | `False` | The operand is undetermined. |

## Edge Cases

- **Never `Unknown`.** `IsKnown` answers `True` or `False`. It is the guard that lets a rule say "only if this was answered", for example `IsKnown(a) AND a`.
- **A faulting operand.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)); `IsKnown` still answers definitely, and the fault is kept. With `boom` a term that faults, `IsKnown(boom)` is `False` with 1 fault recorded.
- **No short-circuit question.** There is one operand, so it is always evaluated.
- **A fault looks like `Unknown`.** `IsKnown` of a faulting term is `False`, because the term contributed `Unknown`. The recorded `Fault` is what tells a failure apart from a predicate that answered `Unknown`.

## Implementation Notes

- The evaluator reports a `IsKnown` node as `IsKnown` in the trace and the trace tree. The four inspections are one node type distinguished by the kind they test.
- `ExpandToPrimitives` expands `IsKnown` to `COALESCE(a, False) OR COALESCE(NOT a, False)`. `ExpandToNand` and `ExpandToNor` keep the `COALESCE` in that expansion, because no `NAND` or `NOR` circuit can express it.
- `CompressToDerived` recognises the expanded `COALESCE` disjunction and writes it back as `IsKnown(a)`.

## Related Operations

- [COALESCE](coalesce.md) defines it.
- [IsUnknown](isunknown.md) is its negation; [IsTrue](istrue.md) and [IsFalse](isfalse.md) are the other inspections.
- [COALESCE](coalesce.md): `If(IsKnown(a), a, b)` is `COALESCE(a, b)`.
- [Result transformations](../result-transformations/README.md): `Decision.Project` and `Decision.Collapse` act on the final result, outside the rule.
