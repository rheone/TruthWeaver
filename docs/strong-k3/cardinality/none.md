# NONE

`True` when no operand is `True` (every operand is `False`), `False` when at least one operand is `True`, and `Unknown` otherwise. The named form of `AtMost(0, ...)`, and as a value the negation of `OR`. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `NONE`
- DSL: `NONE(a, b, ...)`
- JSON and YAML `op`: `none`
- `RuleBuilder` member: `RuleBuilder.None`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `NONE(...)` is defined as `AtMost(0, ...)` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Two or more operands, and no parameter. Fewer is the compile error `MalformedTree` (`TRE0014`).

> [!NOTE]
> Unlike the threshold family ([AtLeast](atleast.md), [AtMost](atmost.md), [Exactly](exactly.md)), which the compiler accepts with a single operand, `NONE` rejects fewer than two operands: a one-operand `NONE` would only be the negation of that operand. `OperatorDefinitions` and the compiler agree on this minimum ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3a).

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`NONE(x1, ..., xn)` is `True` when every operand is `False`, `False` when at least one operand is `True`, and `Unknown` when no operand is `True` and at least one is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `NONE(a, b, c)` |
| JSON | `{"op": "none", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: none` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.None(params RuleBuilder[])` for two or more operands; `RuleBuilder.None(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`NONE` has a real call form with no parameter: every argument is an operand. A call has no precedence, so it needs no parentheses when mixed with `AND`, `OR` or the infix operators (`NONE(a, b) AND c` and `NOT NONE(a, b)` compile as written). `NONE` is a reserved word, so a predicate cannot be named `NONE`. There is no symbol spelling, and every printer keeps the word.

## Aliases

None. The word is case-insensitive in the DSL (`none(a, b)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal Semantics

`NONE` is `AtMost` with `k = 0`: with $d$ operands definitely `True` and $p$ possibly `True`, it answers `True` when every count in $[d, p]$ is zero, which holds exactly when $p = 0$, and `False` when none is, which holds exactly when $d \ge 1$. Anything else is `Unknown` ([semantics](../specification/semantics.md#cardinality-uses-an-interval)).

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)):

$$\operatorname{NONE}(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } p = 0 \\ \mathsf{F} & \text{if } d \ge 1 \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation Table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 2 operands

<!-- k3:eval NONE n=2 -->
| Definitely true (d) | Possibly true (p) | NONE(...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | U |
| 0 | 2 | U |
| 1 | 1 | F |
| 1 | 2 | F |
| 2 | 2 | F |

### 3 operands

<!-- k3:eval NONE n=3 -->
| Definitely true (d) | Possibly true (p) | NONE(...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | F |
| 1 | 2 | F |
| 1 | 3 | F |
| 2 | 2 | F |
| 2 | 3 | F |
| 3 | 3 | F |

### 4 operands

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval NONE n=4 -->
| Definitely true (d) | Possibly true (p) | NONE(...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 0 | 4 | U |
| 1 | 1 | F |
| 1 | 2 | F |
| 1 | 3 | F |
| 1 | 4 | F |
| 2 | 2 | F |
| 2 | 3 | F |
| 2 | 4 | F |
| 3 | 3 | F |
| 3 | 4 | F |
| 4 | 4 | F |

</details>

## Canonical Form

The definition in primitives, for two or more operands:

<!-- k3:canonical NONE n=2..4 -->
```text
ATMOST(0, ...)
```

## Equivalent Forms

### NONE and NOT OR

As a value, `NONE` is the negation of `OR` of the same operands, `Unknown` cases included, at every operand count. It is also the negation of [ANY](any.md):

<!-- k3:canonical NONE n=2..4 -->
```text
NOT(OR(...))
```

<!-- k3:canonical NONE n=2..4 -->
```text
NOT(ANY(...))
```

By De Morgan's laws it is `AND` of the negated operands:

<!-- k3:canonical NONE vars=a,b,c -->
```text
AND(NOT(a), NOT(b), NOT(c))
```

They stay distinct Operations. `NONE` is a cardinality operation and keeps its own node, name, JSON `op` and trace label when a rule round-trips. Unlike [ANY](any.md) and [ALL](all.md), the rewrites that normalise a rule (`Simplify`, `Canonicalize`) leave `NONE` as `NONE` rather than expanding it to `NOT OR`.

### Other forms

With two operands `NONE` is [NOR](../derived/nor.md):

<!-- k3:canonical NOR vars=a,b -->
```text
NONE(a, b)
```

`NONE(...)` is also `Exactly(0, ...)` and `LessThan(1, ...)`:

<!-- k3:canonical NONE n=2..4 -->
```text
EXACTLY(0, ...)
```

<!-- k3:canonical NONE n=2..4 -->
```text
LESSTHAN(1, ...)
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `NONE(a, b, c)` | `False`, `False`, `False` | `True` | No operand is `True` or `Unknown`. |
| `NONE(a, b, c)` | `True`, `False`, `False` | `False` | One operand is `True`. |
| `NONE(a, b, c)` | `False`, `Unknown`, `False` | `Unknown` | The unknown operand decides the result. |
| `NONE(a, b, c)` | `True`, `Unknown`, `False` | `False` | One operand is already `True`; the unknown operand cannot lower the count. |
| `NONE(a, b, c)` | `Unknown`, `Unknown`, `Unknown` | `Unknown` | No operand is known. |


## Edge Cases

| Input | Diagnostic |
| --- | --- |
| One operand, `NONE(a)` | `MalformedTree` (`TRE0014`): "This operator requires at least 2 operands but found 1." |
| JSON or YAML node with fewer than two operands | The same `MalformedTree` (`TRE0014`) diagnostic. |

- **Two or more operands in the rule languages.** A single operand is not "none of one" in the DSL, JSON or YAML; write `NOT` of the operand.
- **`Unknown` is not absorbed.** Only a `True` operand settles the result early; otherwise any `Unknown` operand leaves it `Unknown`.
- **Empty and single-operand conventions.** Only `RuleBuilder.None(IEnumerable<RuleBuilder>)` applies a convention, at build time: an empty sequence becomes the constant `True` and a single operand becomes its negation. `RuleBuilder.None(params RuleBuilder[])` rejects fewer than two operands like the rule languages.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault, even when the result is already settled.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `NONE(isOff, boom)` | `Unknown` | 1 |
| `NONE(isOn, boom)` | `False` | 1 |

## Implementation Notes

- The evaluator reports the node as `NONE` in the trace and the trace tree; the rule outline label is `NONE`. Operand order is kept.
- Evaluation reuses the threshold evaluator (`AtMost(0, ...)`), so it is linear in the operand count.
- `ExpandToPrimitives` rewrites `NONE(...)` to `AtMost(0, ...)`. `Simplify` and `Canonicalize` keep `NONE` ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)). The canonical printer writes `NONE(a, b)`, and every tree-printer style keeps the word.
- JSON and YAML print `{"op": "none", "operands": [...]}`.

## Related Operations

- [ANY](any.md) is its negation and [ALL](all.md) is `NONE` of the negated operands.
- [AtMost](atmost.md) is the threshold form with `k = 0`, and `NOT` of [OR](../gates/or.md) gives the same value.
- [NOR](../derived/nor.md) is the binary form.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
