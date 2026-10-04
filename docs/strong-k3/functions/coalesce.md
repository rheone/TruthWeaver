# COALESCE

The first operand that is not `Unknown`: `True` and `False` pass through unchanged and only `Unknown` is replaced. An external operator, not a Strong Kleene connective. Back to the [Functions index](README.md); shared rules are in the [specification](../specification/README.md).

> [!WARNING]
> `COALESCE` is an **external operator**. It is not monotone in the information order: `COALESCE(Unknown, False)` is `False` but `COALESCE(True, False)` is `True`, so refining the first operand from `Unknown` to `True` changed a definite answer. Do not assume the Strong Kleene laws (the no-tautology theorem, `NAND` and `NOR` expressiveness, monotone rewrites) hold for a rule that contains it.

## Name

- Canonical name: `COALESCE`
- DSL: `COALESCE(a, b, ...)` (also the infix `??`, see [Aliases](#aliases))
- JSON and YAML `op`: `coalesce`
- `RuleBuilder` member: `RuleBuilder.Coalesce`

## Classification

- Category: Functions
- Category index: [Functions](README.md)
- An external operator, not a Strong Kleene connective: it is not monotone in the [information order](../specification/values.md#information-order), because it can answer something definite because an operand is `Unknown`. The [no-tautology theorem](../specification/semantics.md#no-tautologies-no-contradictions) and the `NAND`-only and `NOR`-only rewrites do not extend to it ([semantics](../specification/semantics.md#strong-kleene-connectives-and-external-operators)).

> [!NOTE]
> The category and kind of `COALESCE` were an open question in the approved [proposal](../PROPOSAL.md#3-coalesce-gate-like-but-external): it folds its operands like `OR`, which would suggest the gates. The owner accepted the recommendation, so it is a Functions document, Primitive and flagged external. The classification stays on the unresolved-questions list for the validation report.

## Kind

Primitive. `COALESCE` is the one primitive that can observe `Unknown`, and the four [inspections](istrue.md) are defined from it. It cannot be written with `NOT`, `AND` and `OR`, because those are monotone and it is not.

## Arity

Two or more operands. `COALESCE(a)` and `COALESCE()` in the DSL, and a JSON or YAML node with fewer than two operands, are the compile error `MalformedTree` (`BRE0014`): "This operator requires at least 2 operands but found N." `RuleBuilder.Coalesce(params RuleBuilder[])` rejects the same counts when it builds. The overload `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` is for lists whose length is known only at run time: an empty list builds the constant `Unknown` and a single operand builds that operand unchanged.

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`. The result is `Unknown` only when every operand is.

## Definition

`COALESCE(x1, ..., xn)` is the first operand, read left to right, that is `True` or `False`; it is `Unknown` when there is none.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `COALESCE(a, b, c)` |
| DSL, infix | `a ?? b ?? c` |
| JSON | `{"op": "coalesce", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: coalesce` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.Coalesce(params RuleBuilder[])` for two or more operands; `RuleBuilder.Coalesce(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

The function-call form has no precedence, so it needs no parentheses next to `AND`, `OR` or the infix operators. The infix `??` sits outside the `NOT` > `AND` > `OR` precedence chain: its operands are `NOT`-level expressions (`NOT a ?? b` is `COALESCE(NOT a, b)`), and mixing `??` with `AND`, `OR` or another infix operator at one level without parentheses is the compile error `AmbiguousOperatorMixing` (`BRE0007`). `(a ?? b) AND c` is fine; `a ?? b AND c` is not. Unlike the binary-only infix operators, a chain of `??` is accepted: `a ?? b ?? c` is one three-operand node, because coalescing is associative ([Equivalent Forms](#equivalent-forms)). The canonical printer writes the call form `COALESCE(a, b)`; the tree printers label the node `COALESCE`, or `??` in the symbolic and C-style styles.

## Aliases

| Alias | Kind |
| --- | --- |
| `??` | Infix symbol; a chain compiles to one n-ary node |

`??` compiles to the same node as the word, so notation never changes meaning. The word is case-insensitive (`coalesce(a, b)`), and the JSON and YAML `op` is case-insensitive on read.

## Formal Semantics

`COALESCE` is a left-biased merge. With the information order $\mathsf{U} \sqsubseteq \mathsf{T}$ and $\mathsf{U} \sqsubseteq \mathsf{F}$, `Unknown` is its identity element on both sides, and where two operands are both definite the left one wins. It is therefore neither commutative nor monotone, but it is associative and idempotent.

## Formula

For two operands:

$$\operatorname{COALESCE}(a, b) = \begin{cases} a & \text{if } a \ne \mathsf{U} \\ b & \text{if } a = \mathsf{U} \end{cases}$$

For $n$ operands, with $x_i$ the first operand different from $\mathsf{U}$:

$$\operatorname{COALESCE}(x_1, \dots, x_n) = \begin{cases} x_i & \text{if some } x_j \ne \mathsf{U} \\ \mathsf{U} & \text{if every } x_j = \mathsf{U} \end{cases}$$

## Truth Table

### Two operands

The first operand wins whenever it is definite; only an `Unknown` first operand lets the second show through.

<!-- k3:truth COALESCE -->
| a | b | COALESCE(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | T |
| T | F | T |
| U | T | T |
| U | U | U |
| U | F | F |
| F | T | F |
| F | U | F |
| F | F | F |

### Three operands

Twenty-seven rows, one for each assignment of `T`, `U` and `F` to three operands. The result is the first entry of the row that is not `U`, or `U` when there is none.

<!-- k3:truth COALESCE -->
| a | b | c | COALESCE(a, b, c) |
| --- | --- | --- | --- |
| T | T | T | T |
| T | T | U | T |
| T | T | F | T |
| T | U | T | T |
| T | U | U | T |
| T | U | F | T |
| T | F | T | T |
| T | F | U | T |
| T | F | F | T |
| U | T | T | T |
| U | T | U | T |
| U | T | F | T |
| U | U | T | T |
| U | U | U | U |
| U | U | F | F |
| U | F | T | F |
| U | F | U | F |
| U | F | F | F |
| F | T | T | F |
| F | T | U | F |
| F | T | F | F |
| F | U | T | F |
| F | U | U | F |
| F | U | F | F |
| F | F | T | F |
| F | F | U | F |
| F | F | F | F |

## Equivalent Forms

`COALESCE` is **associative**: grouping never changes the result, which is why a chain `a ?? b ?? c` is one node. Both groupings equal the three-operand node for every assignment:

<!-- k3:canonical COALESCE vars=a,b,c -->
```text
COALESCE(COALESCE(a, b), c)
```

<!-- k3:canonical COALESCE vars=a,b,c -->
```text
COALESCE(a, COALESCE(b, c))
```

`Unknown` is an identity on either side, so a one-operand case is the operand itself:

<!-- k3:canonical COALESCE vars=a -->
```text
COALESCE(a, Unknown)
```

<!-- k3:canonical COALESCE vars=a -->
```text
COALESCE(Unknown, a)
```

For two operands it is also a selection written with `If` and the inspection [IsKnown](isknown.md): keep `a` when it is known, otherwise take `b`.

<!-- k3:canonical COALESCE vars=a,b -->
```text
If(IsKnown(a), a, b)
```

`COALESCE` is not commutative: `COALESCE(True, False)` is `True` but `COALESCE(False, True)` is `False`. It is also not `OR`: `COALESCE(False, True)` is `False` while `OR(False, True)` is `True`.

The [inspections](istrue.md#canonical-form) are short uses of it, and inside a rule `COALESCE(x, True)` and `COALESCE(x, False)` give the same value as the call-site methods `Decision.Project(true)` and `Decision.Project(false)` ([result transformations](../result-transformations/README.md)).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `canEdit ?? False` | `Unknown` | `False` | The common idiom: an unresolved answer becomes a definite `False`. |
| `canEdit ?? False` | `True` | `True` | A known answer passes through; the default is not consulted. |
| `a ?? b` | `False`, `True` | `False` | A known `False` is still known, so the second operand is ignored. |
| `a ?? b ?? c` | `Unknown`, `Unknown`, `False` | `False` | The first known value is the third operand. |
| `a ?? b` | `Unknown`, `Unknown` | `Unknown` | Nothing is known, so nothing replaces the `Unknown`. |

## Edge Cases

- **Only `Unknown` is replaced.** `False` is not "missing": `COALESCE(False, True)` is `False`. SQL's `COALESCE` over `NULL` is the closest precedent.
- **Not monotone.** `COALESCE(Unknown, False)` is `False` and `COALESCE(True, False)` is `True`; refining the first operand turned a definite `False` into a definite `True`. A rule containing `COALESCE` is no longer guaranteed to be `Unknown` when every term is `Unknown`.
- **Short-circuit.** In the default mode the operands are evaluated left to right and evaluation stops at the first definite value; the remaining operands are recorded as `NotEvaluated` and their predicates are not invoked. `EvaluationMode.Exhaustive` evaluates every operand. Short-circuit changes the trace and which faults are recorded, never `Decision.Result`.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)), so a faulting operand is replaced by the next one. In the table below `boom` is a term that faults, `isOn` is `True`, `isOff` is `False` and `isUnsure` answers `Unknown`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `isOn ?? boom` | `True` | 0, because `boom` is not run |
| `boom ?? isOn` | `True` | 1 |
| `boom ?? isOff` | `False` | 1 |
| `isUnsure ?? boom` | `Unknown` | 1 |

## Implementation Notes

- The evaluator reports a `COALESCE` node as `COALESCE` in the trace and the evaluated tree.
- `Simplify` and `Canonicalize` flatten a nested `COALESCE` into one node (`COALESCE(COALESCE(a, b), c)` becomes `COALESCE(a, b, c)`); the plain printer keeps the nesting you wrote.
- `ExpandToPrimitives` leaves `COALESCE` unchanged, because it is primitive. `ExpandToNand` and `ExpandToNor` leave it in place with its operands rewritten: no `NAND` or `NOR` circuit can express it, so such a rule is not `NAND`-only ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 10).
- The analyzer folds the rule from the right over the definite and possible rails of each operand.

## Related Operations

- [OR](../gates/or.md) also folds its operands, but takes the maximum rather than the first known value.
- [IsTrue](istrue.md), [IsFalse](isfalse.md), [IsUnknown](isunknown.md) and [IsKnown](isknown.md) are defined from `COALESCE`.
- [If](if.md) is a Strong Kleene connective that selects between branches; `If(IsKnown(a), a, b)` equals `COALESCE(a, b)`.
- [Result transformations](../result-transformations/README.md): `Decision.Project` is the call-site form of `COALESCE(rule, v)`.
