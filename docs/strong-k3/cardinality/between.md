# BETWEEN

`True` when the number of `True` operands lies in the inclusive range `[min, max]`, `False` when no possible count lies in it, and `Unknown` otherwise. The named form of `AND(AtLeast(min, ...), AtMost(max, ...))`. It counts operands; it is unrelated to the SQL `BETWEEN` or to a numeric range test. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `BETWEEN`
- DSL: `BETWEEN(min, max, a, b, ...)`
- JSON and YAML `op`: `between`, with integers `min` and `max`
- `RuleBuilder` member: `RuleBuilder.Between`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `BETWEEN(min, max, ...)` is defined as `AND(AtLeast(min, ...), AtMost(max, ...))` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Two or more operands after the two integer bounds, with `0 <= min <= max <= n` for `n` operands, and not the whole range `0..n` (`min = 0` with `max = n`). The valid bounds depend on the operand count, so the same bounds can be valid for one rule and rejected for another. Fewer than two operands is the compile error `MalformedTree` (`TRE0014`, see [diagnostics](../specification/diagnostics.md)).

> [!NOTE]
> Unlike the threshold family ([AtLeast](atleast.md), [AtMost](atmost.md), [Exactly](exactly.md)), which the compiler accepts with a single operand, `BETWEEN` rejects fewer than two operands, like [ANY](any.md), [ALL](all.md) and [NONE](none.md). `OperatorDefinitions` and the compiler agree on this minimum.

## Input domain

Each operand is a value in `{T, F, U}`. The parameters `min` and `max` are integers with `0 <= min <= max <= n`, excluding `min = 0` together with `max = n`.

## Output domain

`{T, F, U}`.

## Definition

`BETWEEN(min, max, x1, ..., xn)` is `True` when the number of `True` operands is at least `min` and at most `max` however the `Unknown` operands resolve, `False` when no way of resolving them puts the count in `[min, max]`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `BETWEEN(min, max, a, b, ...)`, for example `BETWEEN(1, 2, a, b, c)` |
| JSON | `{"op": "between", "min": 1, "max": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: between` with `min:`, `max:` and an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.Between(int min, int max, params RuleBuilder[])`; `RuleBuilder.Between(int min, int max, IEnumerable<RuleBuilder>)` for a list whose length is known only at run time (it is not folded, so an empty or too short list is rejected like the `params` form) |

`BETWEEN` has a real call form. The first two arguments are the integer bounds, written as literals, then the operands, so `min` and `max` come first, as in the table above. A call has no precedence (see [syntax](../specification/syntax.md#forms)). `BETWEEN` is a reserved word, so a predicate cannot be named `BETWEEN`. There is no symbol spelling, and every printer keeps the word.

## Aliases

None. The word is case-insensitive in the DSL (`between(1, 2, a, b)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

Let $c$ be the number of `True` operands, known only to lie in $[d, p]$. `BETWEEN` is the [AND](../gates/and.md) of the two cardinality conditions $c \ge \min$ and $c \le \max$, each decided over the interval ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). The combination equals the strongest extension of the Boolean range test, provided $\min \le \max$, so a definite answer needs the whole interval $[d, p]$ to sit inside `[min, max]` (`True`) or entirely outside it (`False`).

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), and $\min \le \max$:

$$\operatorname{BETWEEN}_{\min,\max}(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } d \ge \min \text{ and } p \le \max \\ \mathsf{F} & \text{if } p < \min \text{ or } d > \max \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, min = 1, max = 2

<!-- k3:eval BETWEEN n=3 min=1 max=2 -->
| Definitely true (d) | Possibly true (p) | BETWEEN(1, 2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 1 | 3 | U |
| 2 | 2 | T |
| 2 | 3 | U |
| 3 | 3 | F |

### 3 operands, min = 2, max = 2

<!-- k3:eval BETWEEN n=3 min=2 max=2 -->
| Definitely true (d) | Possibly true (p) | BETWEEN(2, 2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | F |
| 1 | 2 | U |
| 1 | 3 | U |
| 2 | 2 | T |
| 2 | 3 | U |
| 3 | 3 | F |

### 3 operands, min = 1, max = 3

When `max` equals `n` the upper bound is vacuous and the result is the one of `AtLeast(min, ...)`; here it is the [ANY](any.md) table.

<!-- k3:eval BETWEEN n=3 min=1 max=3 -->
| Definitely true (d) | Possibly true (p) | BETWEEN(1, 3, ...) |
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

### 4 operands, min = 1, max = 2

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval BETWEEN n=4 min=1 max=2 -->
| Definitely true (d) | Possibly true (p) | BETWEEN(1, 2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 0 | 4 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 1 | 3 | U |
| 1 | 4 | U |
| 2 | 2 | T |
| 2 | 3 | U |
| 2 | 4 | U |
| 3 | 3 | F |
| 3 | 4 | F |
| 4 | 4 | F |

</details>

## Canonical form

The definition in primitives, for every operand count and every valid pair of bounds:

<!-- k3:canonical BETWEEN n=2..4 -->
```text
AND(ATLEAST(min, ...), ATMOST(max, ...))
```

The valid ranges meet the primitives' at the edges: `AtLeast(min, ...)` needs `min >= 1`, so with `min = 0` the lower bound is vacuous and the result is `AtMost(max, ...)`; likewise `AtMost(max, ...)` needs `max <= n - 1`, so with `max = n` the result is `AtLeast(min, ...)`. `ExpandToPrimitives` produces the `AND` above.

## Equivalent forms

With the upper bound written as a negated lower bound the definition has one operator fewer:

<!-- k3:canonical BETWEEN n=2..4 -->
```text
AND(ATLEAST(min, ...), NOT(ATLEAST(max + 1, ...)))
```

When `min` equals `max` the range is a single count and `BETWEEN(k, k, ...)` is `Exactly(k, ...)`: the three-operand `min = 2, max = 2` table above is the three-operand `Exactly(2, ...)` table of [Exactly](exactly.md). `BETWEEN(1, 1, a, b)` is `ExactlyOne(a, b)`. `BETWEEN(0, 0, ...)` is [NONE](none.md), and `BETWEEN(min, n, ...)` with `min >= 1` is `AtLeast(min, ...)`, which for `min = 1` is [ANY](any.md) and for `min = n` is [ALL](all.md).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `BETWEEN(1, 2, a, b, c)` | `True`, `False`, `False` | `True` | One operand is `True`, inside `[1, 2]`. |
| `BETWEEN(1, 2, a, b, c)` | `True`, `True`, `False` | `True` | Two operands are `True`, inside `[1, 2]`. |
| `BETWEEN(1, 2, a, b, c)` | `True`, `True`, `True` | `False` | Three is above the maximum. |
| `BETWEEN(1, 2, a, b, c)` | `False`, `False`, `False` | `False` | Zero is below the minimum. |
| `BETWEEN(1, 2, a, b, c)` | `True`, `Unknown`, `False` | `True` | The count is one or two, both inside the range. |
| `BETWEEN(1, 2, a, b, c)` | `False`, `Unknown`, `False` | `Unknown` | The count is zero or one: the unknown operand decides whether the minimum is met. |
| `BETWEEN(1, 2, a, b, c)` | `True`, `True`, `Unknown` | `Unknown` | The count is two or three: the unknown operand decides whether the maximum is exceeded. |

## Edge cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `min` or `max` outside `0..n`, or `min > max` | `InvalidThresholdValue` (`TRE0008`) |
| The whole range, `min = 0` and `max = n` | `InvalidThresholdValue` (`TRE0008`) |
| Fewer than two operands | `MalformedTree` (`TRE0014`). |
| A bound missing, fractional or outside the `int` range in the DSL | `SyntaxError` (`TRE0001`). |
| `min` or `max` missing or not an integer in JSON or YAML | `MalformedTree` (`TRE0014`) |

- The bound rules. `min` must satisfy `0 <= min <= max <= n`. The whole range `0..n` is rejected because every count lies in it, so the node would be the constant `True`, the same structural-constant reason as for the threshold family.
- An empty range is rejected, not evaluated. `min > max` describes no count at all, so the right result would be `False` for every completion. The composition `AND(AtLeast(min), AtMost(max))` does not give that: with `min > max` and `Unknown` operands it can return `Unknown` where the correct answer is `False` (for `min = 2`, `max = 1` and two `Unknown` operands it gives `Unknown`). The bounds are therefore enforced, and the rewrites build a `BETWEEN` only when `min <= max`.
- `min = max` is allowed. It is the single count `Exactly(min, ...)`.
- One vacuous bound is allowed. `max = n` with `min >= 1`, or `min = 0` with `max < n`, makes `BETWEEN` the same function as `AtLeast(min, ...)` or `AtMost(max, ...)`; only the whole range is rejected.
- Empty and single-operand conventions. `RuleBuilder.Between` has no folding: an empty or one-operand list is rejected like the `params` form.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `BETWEEN(1, 2, isOn, isOff, boom)` | `True` | 1 |
| `BETWEEN(1, 1, isOn, isOn, boom)` | `False` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `BETWEEN(min, max)`, for example `BETWEEN(1, 2)`. Operand order is kept.
- `ExpandToPrimitives` rewrites `BETWEEN(1, 2, a, b, c)` to `AtLeast(1, a, b, c) AND AtMost(2, a, b, c)`. `Simplify` and `Canonicalize` keep `BETWEEN`. The canonical printer writes `BETWEEN(1, 2, a, b, c)`, and every tree-printer style keeps the word.
- JSON and YAML print `{"op": "between", "operands": [...], "min": 1, "max": 2}`.

## Related operations

- [AtLeast](atleast.md) and [AtMost](atmost.md) are its two bounds; [Exactly](exactly.md) is the case `min = max`.
- [ANY](any.md), [ALL](all.md) and [NONE](none.md) are the other named forms of the family.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
