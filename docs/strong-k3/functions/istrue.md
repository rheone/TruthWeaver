# IsTrue

`True` when the operand is `True`, and `False` when it is `False` or `Unknown`: the `Unknown` answer is turned into a definite `False`. An external operator, not a Strong Kleene connective. Back to the [Functions index](README.md); shared rules are in the [specification](../specification/README.md).

> [!WARNING]
> `IsTrue` is an **external operator**. It is not monotone in the information order: `IsTrue(Unknown)` is `False` but `IsTrue(True)` is `True`, so refining the operand from `Unknown` to `True` changed a definite answer. Do not assume the Strong Kleene laws (the no-tautology theorem, `NAND` and `NOR` expressiveness, monotone rewrites) hold for a rule that contains it.

## Name

- Canonical name: `IsTrue`
- DSL: `IsTrue(a)`
- JSON and YAML `op`: `isTrue`
- `RuleBuilder` member: `RuleBuilder.IsTrue`

## Classification

- Category: Functions
- Category index: [Functions](README.md)
- An external operator, not a Strong Kleene connective: it is not monotone in the [information order](../specification/values.md#information-order), because it answers something definite because its operand is `Unknown`. The [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions) and the `NAND`-only and `NOR`-only rewrites do not extend to it ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

The inspections are Derived: each expands to [COALESCE](coalesce.md). They are external operators, so they are in Functions and not next to the connectives.

## Kind

Derived. `IsTrue` is defined from `COALESCE` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Exactly one operand. `IsTrue(a, b)` and `IsTrue()` in the DSL, and a JSON or YAML node with another operand count, are the compile error `InfixArityViolation` (`TRE0006`, see [diagnostics](../specification/diagnostics.md)). `RuleBuilder.IsTrue` takes one argument, so a wrong count cannot be written there.

## Input domain

The operand is a value in `{T, F, U}`.

## Output domain

`{T, F}`. The result is never `Unknown`, whatever the operand is, and also when the operand is a faulting term.

## Definition

`IsTrue(a)` is `True` when `a` is `True` and `False` otherwise, so an `Unknown` operand gives `False`. It is the SQL `a IS TRUE`: unlike the bare operand, it is never `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `IsTrue(a)` |
| JSON | `{"op": "isTrue", "operands": [ <expression> ]}` |
| YAML | `op: isTrue` with an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.IsTrue(operand)` |

`IsTrue` is a reserved word in any letter case (`istrue(a)`), so a predicate cannot be named `IsTrue`. It has only the call form: `IsTrue a` without parentheses is a syntax error (`TRE0001`). A call has no precedence, so `NOT IsTrue(a)` and `IsTrue(a) AND b` compile as written.

## Aliases

None. The word is case-insensitive in the DSL, and the JSON and YAML `op` is case-insensitive on read. There is no symbol spelling, and every printer keeps the word.

## Formal semantics

`IsTrue` is the characteristic function of the value `True`: it asks whether the operand is definitely true. It maps `Unknown` to `False`, which no Strong Kleene connective can do, because an operand that might still become `True` would have to keep the result open.

## Formula

$$\operatorname{IsTrue}(a) = \begin{cases} \mathsf{T} & \text{if } a = \mathsf{T} \\ \mathsf{F} & \text{otherwise} \end{cases}$$

## Truth table

Only a `True` operand gives `T`; both `U` and `F` give `F`.

<!-- k3:truth IsTrue -->
| a | IsTrue(a) |
| --- | --- |
| T | T |
| U | F |
| F | F |

## Canonical form

`IsTrue` is defined from `COALESCE`, the one operation that can observe `Unknown`:

<!-- k3:canonical IsTrue vars=a -->
```text
COALESCE(a, False)
```

## Equivalent forms

`IsTrue` is the fail-closed reading of a result: the same replacement of `Unknown` by `False` that `Decision.IsSatisfied` and `Decision.Project(false)` apply at the end of an evaluation. Inside a rule it is idempotent, and it is `IsFalse` of the negation:

<!-- k3:canonical IsTrue vars=a -->
```text
IsTrue(IsTrue(a))
```

<!-- k3:canonical IsTrue vars=a -->
```text
IsFalse(NOT(a))
```

`IsTrue(a)` differs from `a` exactly when `a` is `Unknown`. It is the condition that makes [If](if.md) behave as SQL `CASE`: `If(IsTrue(c), t, f)` is `CASE WHEN c THEN t ELSE f END`.

## Examples

| Rule | Operand value | Result | Why |
| --- | --- | --- | --- |
| `IsTrue(canEdit)` | `True` | `True` | The operand is definitely true. |
| `IsTrue(canEdit)` | `False` | `False` | The operand is false. |
| `IsTrue(canEdit)` | `Unknown` | `False` | An unresolved answer is not a definite yes. |

## Edge cases

- Never `Unknown`. `IsTrue(Unknown)` is `False`, not `Unknown`. A rule that must not grant access on an unresolved answer can wrap it: `IsTrue(canEdit)`.
- A faulting operand. A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault`; `IsTrue` still answers definitely, and the fault is kept. With `boom` a term that faults, `IsTrue(boom)` is `False` with 1 fault recorded.
- Relation to the result transformations. As a value, `IsTrue(x)` equals `COALESCE(x, False)` and `Decision.Project(false)` of the result of `x`. They differ in where they apply: inside a rule or on the final decision.

## Evaluation behavior

- The operand always runs.
- `ExpandToPrimitives` expands `IsTrue` to `COALESCE(a, False)`. `ExpandToNand` and `ExpandToNor` keep the `COALESCE` in that expansion, because no `NAND` or `NOR` circuit can express it.
- `Simplify` rewrites `IsTrue(NOT x)` to `IsFalse(x)` and `IsTrue(IsTrue(x))` to `IsTrue(x)`. `CompressToDerived` does not fold `COALESCE(x, False)` back into `IsTrue(x)`, so an expanded rule keeps its `COALESCE` unless `Simplify` runs.

## Related operations

- [COALESCE](coalesce.md) defines it.
- [IsFalse](isfalse.md), [IsUnknown](isunknown.md) and [IsKnown](isknown.md) are the other inspections; exactly one of `IsTrue`, `IsFalse` and `IsUnknown` is `True` for any operand.
- [If](if.md): `If(IsTrue(c), t, f)` is the SQL `CASE` equivalent.
- [Result transformations](../result-transformations/README.md): `Decision.Project` and `Decision.Collapse` act on the final result, outside the rule.
