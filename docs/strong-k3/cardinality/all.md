# ALL

`True` when every operand is `True`, `False` when at least one operand is `False`, and `Unknown` otherwise. The named form of `AtLeast(n, ...)` for the `n` operands, and as a value the same function as `AND`. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `ALL`
- DSL: `ALL(a, b, ...)`
- JSON and YAML `op`: `all`
- `RuleBuilder` member: `RuleBuilder.All`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `ALL(x1, ..., xn)` is defined as `AtLeast(n, x1, ..., xn)` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Two or more operands, and no parameter: the threshold `n` is the operand count. Fewer is the compile error `MalformedTree` (`TRE0014`).

> [!NOTE]
> Unlike the threshold family ([AtLeast](atleast.md), [AtMost](atmost.md), [Exactly](exactly.md)), which the compiler accepts with a single operand, `ALL` rejects fewer than two operands: a one-operand `ALL` would only be that operand. `OperatorDefinitions` and the compiler agree on this minimum ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3a).

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`ALL(x1, ..., xn)` is `True` when every operand is `True`, `False` when at least one operand is `False`, and `Unknown` when no operand is `False` and at least one is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `ALL(a, b, c)` |
| JSON | `{"op": "all", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: all` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.All(params RuleBuilder[])` for two or more operands; `RuleBuilder.All(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`ALL` has a real call form with no parameter: every argument is an operand. A call has no precedence, so it needs no parentheses when mixed with `AND`, `OR` or the infix operators (`ALL(a, b) AND c` and `NOT ALL(a, b)` compile as written). `ALL` is a reserved word, so a predicate cannot be named `ALL`. There is no symbol spelling, and every printer keeps the word.

## Aliases

None. The word is case-insensitive in the DSL (`all(a, b)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal Semantics

`ALL` is `AtLeast` with `k = n`: with $d$ operands definitely `True` and $p$ possibly `True`, it answers `True` when every count in $[d, p]$ equals $n$, which holds exactly when $d = n$, and `False` when none does, which holds exactly when $p < n$ ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). The second condition is the one that is easy to misstate: `ALL` is `False` only when $T + U < n$, that is, when at least one operand is `False`. Having fewer than $n$ definitely true operands is not enough.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)):

$$\operatorname{ALL}(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } d = n \\ \mathsf{F} & \text{if } p < n \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation Table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 2 operands

<!-- k3:eval ALL n=2 -->
| Definitely true (d) | Possibly true (p) | ALL(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | U |
| 1 | 1 | F |
| 1 | 2 | U |
| 2 | 2 | T |

### 3 operands

<!-- k3:eval ALL n=3 -->
| Definitely true (d) | Possibly true (p) | ALL(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | F |
| 0 | 3 | U |
| 1 | 1 | F |
| 1 | 2 | F |
| 1 | 3 | U |
| 2 | 2 | F |
| 2 | 3 | U |
| 3 | 3 | T |

### 4 operands

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval ALL n=4 -->
| Definitely true (d) | Possibly true (p) | ALL(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | F |
| 0 | 3 | F |
| 0 | 4 | U |
| 1 | 1 | F |
| 1 | 2 | F |
| 1 | 3 | F |
| 1 | 4 | U |
| 2 | 2 | F |
| 2 | 3 | F |
| 2 | 4 | U |
| 3 | 3 | F |
| 3 | 4 | U |
| 4 | 4 | T |

</details>

## Canonical Form

The definition in primitives. The threshold is the operand count, so the form is written out for two, three and four operands:

<!-- k3:canonical ALL vars=a,b -->
```text
ATLEAST(2, a, b)
```

<!-- k3:canonical ALL vars=a,b,c -->
```text
ATLEAST(3, a, b, c)
```

<!-- k3:canonical ALL vars=a,b,c,d -->
```text
ATLEAST(4, a, b, c, d)
```

## Equivalent Forms

### ALL and AND

As a value, `ALL` is `AND` of the same operands, `Unknown` cases included, at every operand count:

<!-- k3:canonical ALL n=2..4 -->
```text
AND(...)
```

They stay distinct Operations. `AND` is a [gate](../gates/and.md) and a primitive; `ALL` is a cardinality operation and keeps its own node, name, JSON `op` and trace label when a rule round-trips. The rewrites that normalise a rule (`Simplify`, `Canonicalize`) collapse `ALL` into `AND`, and `CompressToDerived` leaves it as `ALL`.

### Other forms

`ALL` is `NONE` of the negated operands, and the negation of `ANY` of the negated operands:

<!-- k3:canonical ALL vars=a,b,c -->
```text
NONE(NOT(a), NOT(b), NOT(c))
```

<!-- k3:canonical ALL vars=a,b,c -->
```text
NOT(ANY(NOT(a), NOT(b), NOT(c)))
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `ALL(a, b, c)` | `True`, `True`, `True` | `True` | Every operand is `True`. |
| `ALL(a, b, c)` | `True`, `True`, `False` | `False` | One operand is `False`. |
| `ALL(a, b, c)` | `True`, `Unknown`, `True` | `Unknown` | The unknown operand decides the result. |
| `ALL(a, b, c)` | `Unknown`, `Unknown`, `False` | `False` | One operand is `False`; the unknown operands cannot make the count reach three. |
| `ALL(a, b, c)` | `Unknown`, `Unknown`, `Unknown` | `Unknown` | No operand is `False`, although none is definitely `True`. |

The last row is the case a reading of `ALL` as "`False` when fewer than `n` operands are `True`" gets wrong: with fewer than `n` definitely true operands but no `False` operand the result is `Unknown`, so `ALL(True, Unknown)` and `ALL(Unknown, Unknown)` are both `Unknown`.


## Edge Cases

| Input | Diagnostic |
| --- | --- |
| One operand, `ALL(a)` | `MalformedTree` (`TRE0014`): "This operator requires at least 2 operands but found 1." |
| JSON or YAML node with fewer than two operands | The same `MalformedTree` (`TRE0014`) diagnostic. |

- **`False` only when `T + U < n`.** At least one `False` operand is required for a definite `False`; see the [last example row](#examples).
- **Empty and single-operand conventions.** Only `RuleBuilder.All(IEnumerable<RuleBuilder>)` applies a convention, at build time: an empty sequence becomes the constant `True` and a single operand is returned unchanged. `RuleBuilder.All(params RuleBuilder[])` rejects fewer than two operands like the rule languages.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault, even when the result is already settled.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `ALL(isOn, boom)` | `Unknown` | 1 |
| `ALL(isOff, boom)` | `False` | 1 |

## Implementation Notes

- The evaluator reports the node as `ALL` in the trace and the evaluated tree; the rule description label is `ALL`. Operand order is kept.
- Evaluation reuses the threshold evaluator (`k` equal to the operand count), so it is linear in the operand count.
- `ExpandToPrimitives` rewrites `ALL(a, b, c)` to `AtLeast(3, a, b, c)`. `Simplify` and `Canonicalize` collapse it to `AND` ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)). The canonical printer writes `ALL(a, b)`, and every tree-printer style keeps the word.
- JSON and YAML print `{"op": "all", "operands": [...]}`.

## Related Operations

- [AND](../gates/and.md) is the same function as a primitive gate, and [AtLeast](atleast.md) is the threshold form with `k = n`.
- [ANY](any.md) is its dual and [NONE](none.md) is `ALL` of the negations.
- [BETWEEN](between.md) covers the range `[min, max]`.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
