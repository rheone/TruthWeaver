# AtMost

`True` when at most `k` of the operands are `True`, `False` when more than `k` are already `True`, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `AtMost`
- DSL: `AtMost(k, a, b, ...)`
- JSON and YAML `op`: `atMost`, with an integer `k`
- `RuleBuilder` member: `RuleBuilder.AtMost`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `AtMost` has no definition in other Operations. It is one of the three counting primitives with [AtLeast](atleast.md) and [Exactly](exactly.md). It equals `NOT AtLeast(k + 1, ...)`, but it is a kernel node, not a rewrite of it. `NONE` is its special case `AtMost(0, ...)`.

## Arity

One or more operands after the integer `k`, with 0 <= k <= n - 1 for n operands. The upper end is n - 1, not n: `AtMost(n, ...)` could never be `False`, so it is rejected.

## Input domain

Each operand is a value in `{T, F, U}`. The parameter `k` is an integer, with 0 <= k <= n - 1.

## Output domain

`{T, F, U}`.

## Definition

`AtMost(k, x1, ..., xn)` is `True` when at most `k` operands can be `True` (no more than `k` are `True` or `Unknown`), `False` when more than `k` operands are `True`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `AtMost(k, a, b, ...)` |
| JSON | `{"op": "atMost", "k": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: atMost` with `k:` and an `operands:` list |
| `RuleBuilder` | `RuleBuilder.AtMost(int k, params RuleBuilder[])`; `RuleBuilder.AtMost(int k, IEnumerable<RuleBuilder>)` for a list whose length is known only at run time (it is not folded, so an empty or too short list is rejected like the `params` form) |

`AtMost` has a real call form. The first argument is the integer `k`, written as a literal; the rest are the operands, so A call has no precedence (see [syntax](../specification/syntax.md#forms)). There is no symbol spelling.

## Aliases

None. The word is case-insensitive in the DSL (`atmost(1, a)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

Let $c$ be the number of `True` operands, known only to lie in $[d, p]$. `AtMost` answers `True` when every count in the interval satisfies $c \le k$, which holds exactly when $p \le k$, and `False` when none does, which holds exactly when $d > k$. Anything else is `Unknown`. The `Unknown` operands are the ones that count against the bound, so they are included in $p$: an operand that might be `True` can still push the count over `k`.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), the true count lies in $[d, p]$:

$$\operatorname{AtMost}_k(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } p \le k \\ \mathsf{F} & \text{if } d > k \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, k = 0

<!-- k3:eval AtMost n=3 k=0 -->
| Definitely true (d) | Possibly true (p) | AtMost(0, ...) |
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

### 3 operands, k = 1

<!-- k3:eval AtMost n=3 k=1 -->
| Definitely true (d) | Possibly true (p) | AtMost(1, ...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | T |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | T |
| 1 | 2 | U |
| 1 | 3 | U |
| 2 | 2 | F |
| 2 | 3 | F |
| 3 | 3 | F |

### 3 operands, k = 2

<!-- k3:eval AtMost n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | AtMost(2, ...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | T |
| 0 | 2 | T |
| 0 | 3 | U |
| 1 | 1 | T |
| 1 | 2 | T |
| 1 | 3 | U |
| 2 | 2 | T |
| 2 | 3 | U |
| 3 | 3 | F |

### 4 operands, k = 1

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval AtMost n=4 k=1 -->
| Definitely true (d) | Possibly true (p) | AtMost(1, ...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | T |
| 0 | 2 | U |
| 0 | 3 | U |
| 0 | 4 | U |
| 1 | 1 | T |
| 1 | 2 | U |
| 1 | 3 | U |
| 1 | 4 | U |
| 2 | 2 | F |
| 2 | 3 | F |
| 2 | 4 | F |
| 3 | 3 | F |
| 3 | 4 | F |
| 4 | 4 | F |

</details>

## Equivalent forms

`AtMost` is a primitive, so it has no canonical form. These forms hold for every assignment.

`AtMost(k, ...)` is the negation of `AtLeast(k + 1, ...)`:

<!-- k3:canonical AtMost n=1..4 -->
```text
NOT(ATLEAST(k + 1, ...))
```

`AtMost` and [AtLeast](atleast.md) are duals through negation: at most `k` `True` operands is at least `n - k` `False` operands. For three operands:

<!-- k3:canonical AtMost vars=a,b,c -->
```text
ATLEAST(3 - k, NOT(a), NOT(b), NOT(c))
```

`AtMost(0, ...)` is `NONE`, the same function, `Unknown` cases included. With one operand it is the negation of that operand:

<!-- k3:canonical NONE n=2..4 -->
```text
ATMOST(0, ...)
```

<!-- k3:canonical AtMost vars=a -->
```text
NOT(a)
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `AtMost(1, a, b, c)` | `True`, `False`, `False` | `True` | One operand is `True`, which is within the limit. |
| `AtMost(1, a, b, c)` | `True`, `True`, `False` | `False` | Two operands are already `True`. |
| `AtMost(1, a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand could make the count two. |
| `AtMost(1, a, b, c)` | `False`, `Unknown`, `False` | `True` | At most one operand could be `True`. |
| `AtMost(0, a, b)` | `Unknown`, `False` | `Unknown` | The same value as `NOT (a OR b)`. |

## Edge cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `k` below 0 or above n - 1 | `InvalidThresholdValue` (`TRE0008`, see [diagnostics](../specification/diagnostics.md)) |
| No operands | `MalformedTree` (`TRE0014`) |
| `k` missing, fractional or outside the `int` range in the DSL | `SyntaxError` (`TRE0001`) |
| `k` missing or not a number in JSON or YAML | `MalformedTree` (`TRE0014`) |

- The valid `k` range. `k` must satisfy 0 <= k <= n - 1. `AtMost(n, ...)` would always be `True` and a negative `k` always `False`, so the compiler rejects both instead of folding them.
- One operand. The compiler accepts `AtMost(0, a)`, which is `NOT a`. `AtMost(1, a)` is rejected because `k` may not reach the operand count.
- `AtMost(0)` is `NONE`. The values are identical. `NONE` is a named operation of the same family; see the [index](README.md).
- `Unknown` counts against the bound. A single `Unknown` operand is enough to keep `AtMost(k)` from being `True` when `d = k`, because that operand might be `True`.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `AtMost(1, isOn, isOff, boom)` | `Unknown` | 1 |
| `AtMost(0, isOn, boom)` | `False` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `AtMost(k)`, for example `AtMost(1)`.
- `ExpandToPrimitives` leaves `AtMost` unchanged, because it is a primitive.

## Related operations

- [AtLeast](atleast.md) is the lower-bound dual, and [Exactly](exactly.md) combines both bounds.
- [LessThan](lessthan.md) is the strict form: `LessThan(k, ...)` is `AtMost(k - 1, ...)`.
- [NOR](../derived/nor.md) is the binary `NOT OR`; `AtMost(0, ...)` is its n-ary counterpart.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
