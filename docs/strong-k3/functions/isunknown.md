# IsUnknown

`True` when the operand is `Unknown`, and `False` when it is `True` or `False`. An external operator, not a Strong Kleene connective. Back to the [Functions index](README.md); shared rules are in the [specification](../specification/README.md).

> [!WARNING]
> `IsUnknown` is an **external operator**. It is not monotone in the information order: `IsUnknown(Unknown)` is `True` but `IsUnknown(True)` is `False`, so refining the operand from `Unknown` to `True` flipped a definite answer. Do not assume the Strong Kleene laws (the no-tautology theorem, `NAND` and `NOR` expressiveness, monotone rewrites) hold for a rule that contains it.

## Name

- Canonical name: `IsUnknown`
- DSL: `IsUnknown(a)`
- JSON and YAML `op`: `isUnknown`
- `RuleBuilder` member: `RuleBuilder.IsUnknown`

## Classification

- Category: Functions
- Category index: [Functions](README.md)
- An external operator, not a Strong Kleene connective: it is not monotone in the [information order](../specification/values.md#information-order), because it answers something definite because its operand is `Unknown`. The [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions) and the `NAND`-only and `NOR`-only rewrites do not extend to it ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

The inspections are Derived: each expands to [COALESCE](coalesce.md). They are external operators, so they are in Functions and not next to the connectives.

## Kind

Derived. `IsUnknown` is defined from `COALESCE` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Exactly one operand. `IsUnknown(a, b)` and `IsUnknown()` in the DSL, and a JSON or YAML node with another operand count, are the compile error `InfixArityViolation` (`TRE0006`, see [diagnostics](../specification/diagnostics.md)). `RuleBuilder.IsUnknown` takes one argument, so a wrong count cannot be written there.

## Input domain

The operand is a value in `{T, F, U}`.

## Output domain

`{T, F}`. The result is never `Unknown`, whatever the operand is, and also when the operand is a faulting term.

## Definition

`IsUnknown(a)` is `True` when `a` is `Unknown` and `False` when `a` is `True` or `False`. It is the SQL `a IS UNKNOWN` for a boolean expression.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `IsUnknown(a)` |
| JSON | `{"op": "isUnknown", "operands": [ <expression> ]}` |
| YAML | `op: isUnknown` with an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.IsUnknown(operand)` |

`IsUnknown` is a reserved word in any letter case (`isunknown(a)`), so a predicate cannot be named `IsUnknown`. It has only the call form: `IsUnknown a` without parentheses is a syntax error (`TRE0001`). A call has no precedence, so `NOT IsUnknown(a)` and `IsUnknown(a) AND b` compile as written.

## Aliases

None. The word is case-insensitive in the DSL, and the JSON and YAML `op` is case-insensitive on read. There is no symbol spelling, and every printer keeps the word.

## Formal semantics

`IsUnknown` is the characteristic function of the value `Unknown`: it asks whether the operand is undetermined. It is `True` for an `Unknown` operand and `False` for each of its refinements, so refining an input can change a definite output and the no-tautology theorem does not hold for rules that use it.

## Formula

$$\operatorname{IsUnknown}(a) = \begin{cases} \mathsf{T} & \text{if } a = \mathsf{U} \\ \mathsf{F} & \text{otherwise} \end{cases}$$

## Truth table

Only an `Unknown` operand gives `T`.

<!-- k3:truth IsUnknown -->
| a | IsUnknown(a) |
| --- | --- |
| T | F |
| U | T |
| F | F |

## Canonical form

`IsUnknown` is defined from `COALESCE`, the one operation that can observe `Unknown`:

<!-- k3:canonical IsUnknown vars=a -->
```text
AND(COALESCE(a, True), COALESCE(NOT(a), True))
```

## Equivalent forms

`IsUnknown` is the negation of `IsKnown`, and the rest of the three values once `True` and `False` are excluded:

<!-- k3:canonical IsUnknown vars=a -->
```text
NOT(IsKnown(a))
```

<!-- k3:canonical IsUnknown vars=a -->
```text
AND(NOT(IsTrue(a)), NOT(IsFalse(a)))
```

Exactly one of `IsTrue(a)`, `IsFalse(a)` and `IsUnknown(a)` is `True` for every `a`, so `IsKnown(a) OR IsUnknown(a)` is a tautology. A rule built only from Strong Kleene connectives can never be one ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

## Examples

| Rule | Operand value | Result | Why |
| --- | --- | --- | --- |
| `IsUnknown(canEdit)` | `Unknown` | `True` | The operand is undetermined. |
| `IsUnknown(canEdit)` | `True` | `False` | The operand is known. |
| `IsUnknown(canEdit)` | `False` | `False` | The operand is known. |

## Edge cases

- Never `Unknown`. `IsUnknown` answers `True` or `False`: asking whether something is undetermined always has a definite answer.
- A faulting operand. A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault`; `IsUnknown` still answers definitely, and the fault is kept. With `boom` a term that faults, `IsUnknown(boom)` is `True` with 1 fault recorded.
- A fault looks like `Unknown`. `IsUnknown` of a faulting term is `True`, because the term contributed `Unknown`. The recorded `Fault` is what tells a failure apart from a predicate that answered `Unknown`; check the decision's faults when the difference matters.

## Evaluation behavior

- The operand always runs.
- `ExpandToPrimitives` expands `IsUnknown` to `COALESCE(a, True) AND COALESCE(NOT a, True)`. `ExpandToNand` and `ExpandToNor` keep the `COALESCE` in that expansion, because no `NAND` or `NOR` circuit can express it.
- `CompressToDerived` recognises the expanded `COALESCE` conjunction and writes it back as `IsUnknown(a)`.

## Related operations

- [COALESCE](coalesce.md) defines it.
- [IsKnown](isknown.md) is its negation; [IsTrue](istrue.md) and [IsFalse](isfalse.md) are the other inspections.
- [semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators) uses `IsKnown(a) OR IsUnknown(a)` as the example of a tautology.
- [Result transformations](../result-transformations/README.md): `Decision.Project` and `Decision.Collapse` act on the final result, outside the rule.
