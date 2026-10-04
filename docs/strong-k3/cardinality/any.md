# ANY

`True` when at least one operand is `True`, `False` when every operand is `False`, and `Unknown` otherwise. The named form of `AtLeast(1, ...)`, and as a value the same function as `OR`. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `ANY`
- DSL: `ANY(a, b, ...)`
- JSON and YAML `op`: `any`
- `RuleBuilder` member: `RuleBuilder.Any`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `ANY(...)` is defined as `AtLeast(1, ...)` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Two or more operands, and no parameter. Fewer is the compile error `MalformedTree` (`TRE0014`, see [diagnostics](../specification/diagnostics.md)).

> [!NOTE]
> Unlike the threshold family ([AtLeast](atleast.md), [AtMost](atmost.md), [Exactly](exactly.md)), which the compiler accepts with a single operand, `ANY` rejects fewer than two operands: a one-operand `ANY` would only be that operand. `OperatorDefinitions` and the compiler agree on this minimum.

## Input domain

Each operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`ANY(x1, ..., xn)` is `True` when at least one operand is `True`, `False` when every operand is `False`, and `Unknown` when no operand is `True` and at least one is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `ANY(a, b, c)` |
| JSON | `{"op": "any", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: any` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.Any(params RuleBuilder[])` for two or more operands; `RuleBuilder.Any(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`ANY` has a real call form with no parameter: every argument is an operand. A call has no precedence (see [syntax](../specification/syntax.md#forms)). `ANY` is a reserved word, so a predicate cannot be named `ANY`. There is no symbol spelling, and every printer keeps the word.

## Aliases

None. The word is case-insensitive in the DSL (`any(a, b)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

`ANY` is `AtLeast` with `k = 1`: with $d$ operands definitely `True` and $p$ possibly `True`, it answers `True` when every count in $[d, p]$ is at least one, which holds exactly when $d \ge 1$, and `False` when none is, which holds exactly when $p = 0$. Anything else is `Unknown` ([semantics](../specification/semantics.md#cardinality-uses-an-interval)).

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)):

$$\operatorname{ANY}(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } d \ge 1 \\ \mathsf{F} & \text{if } p = 0 \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 2 operands

<!-- k3:eval ANY n=2 -->
| Definitely true (d) | Possibly true (p) | ANY(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 2 | 2 | T |

### 3 operands

<!-- k3:eval ANY n=3 -->
| Definitely true (d) | Possibly true (p) | ANY(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 1 | 3 | T |
| 2 | 2 | T |
| 2 | 3 | T |
| 3 | 3 | T |

### 4 operands

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval ANY n=4 -->
| Definitely true (d) | Possibly true (p) | ANY(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 0 | 4 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 1 | 3 | T |
| 1 | 4 | T |
| 2 | 2 | T |
| 2 | 3 | T |
| 2 | 4 | T |
| 3 | 3 | T |
| 3 | 4 | T |
| 4 | 4 | T |

</details>

## Canonical form

The definition in primitives, for two or more operands:

<!-- k3:canonical ANY n=2..4 -->
```text
ATLEAST(1, ...)
```

## Equivalent forms

### ANY and OR

As a value, `ANY` is `OR` of the same operands, `Unknown` cases included, at every operand count:

<!-- k3:canonical ANY n=2..4 -->
```text
OR(...)
```

They stay distinct Operations. `OR` is a [gate](../gates/or.md) and a primitive; `ANY` is a cardinality operation and keeps its own node, name, JSON `op` and trace label when a rule round-trips. The rewrites that normalise a rule (`Simplify`, `Canonicalize`) collapse `ANY` into `OR`, and `CompressToDerived` leaves it as `ANY`.

### Other forms

`ANY` is the negation of [NONE](none.md), and De Morgan's laws turn it into [ALL](all.md) of the negated operands:

<!-- k3:canonical ANY n=2..4 -->
```text
NOT(NONE(...))
```

<!-- k3:canonical ANY vars=a,b,c -->
```text
NOT(ALL(NOT(a), NOT(b), NOT(c)))
```

`ANY(...)` is also `GreaterThan(0, ...)`:

<!-- k3:canonical ANY n=2..4 -->
```text
GREATERTHAN(0, ...)
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `ANY(a, b, c)` | `True`, `False`, `False` | `True` | One operand is `True`. |
| `ANY(a, b, c)` | `False`, `False`, `False` | `False` | No operand is `True` or `Unknown`. |
| `ANY(a, b, c)` | `False`, `Unknown`, `False` | `Unknown` | The unknown operand decides the result. |
| `ANY(a, b, c)` | `True`, `Unknown`, `False` | `True` | One operand is already `True`; the unknown operand cannot lower the count. |
| `ANY(a, b, c)` | `Unknown`, `Unknown`, `Unknown` | `Unknown` | No operand is known. |

## Edge cases

| Input | Diagnostic |
| --- | --- |
| One operand, `ANY(a)` | `MalformedTree` (`TRE0014`) |
| JSON or YAML node with fewer than two operands | The same `MalformedTree` (`TRE0014`) diagnostic. |

- Two or more operands in the rule languages. A single operand is not "any of one" in the DSL, JSON or YAML; write the operand itself.
- `Unknown` is not absorbed. Only a `True` operand settles the result early; otherwise any `Unknown` operand leaves it `Unknown`.
- Empty and single-operand conventions. Only `RuleBuilder.Any(IEnumerable<RuleBuilder>)` applies a convention, at build time: an empty sequence becomes the constant `False` and a single operand is returned unchanged. `RuleBuilder.Any(params RuleBuilder[])` rejects fewer than two operands like the rule languages.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `ANY(isOn, boom)` | `True` | 1 |
| `ANY(isOff, boom)` | `Unknown` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `ANY`. Operand order is kept.
- `ExpandToPrimitives` rewrites `ANY(...)` to `AtLeast(1, ...)`. `Simplify` and `Canonicalize` collapse `ANY` to `OR`. The canonical printer writes `ANY(a, b)`, and every tree-printer style keeps the word.
- JSON and YAML print `{"op": "any", "operands": [...]}`.

## Related operations

- [OR](../gates/or.md) is the same function as a primitive gate, and [AtLeast](atleast.md) is the threshold form with `k = 1`.
- [NONE](none.md) is its negation and [ALL](all.md) its dual.
- [BETWEEN](between.md) covers the range `[min, max]`, and `BETWEEN(1, n, ...)` is `ANY` as a value.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
