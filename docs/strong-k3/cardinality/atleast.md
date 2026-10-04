# AtLeast

`True` when at least `k` of the operands are `True`, `False` when so few operands can still be `True` that `k` is out of reach, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `AtLeast`
- DSL: `AtLeast(k, a, b, ...)`
- JSON and YAML `op`: `atLeast`, with an integer `k`
- `RuleBuilder` member: `RuleBuilder.AtLeast`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `AtLeast` has no definition in other Operations. It is one of the three counting primitives, with [AtMost](atmost.md) and [Exactly](exactly.md), from which the other cardinality operations are defined. `ANY` and `ALL` are its special cases `AtLeast(1, ...)` and `AtLeast(n, ...)`.

## Arity

One or more operands after the integer `k`, with 1 <= k <= n for n operands. The valid range of `k` depends on the operand count, so the same `k` can be valid for one rule and rejected for another.

## Input domain

Each operand is a value in `{T, F, U}`. The parameter `k` is an integer, with 1 <= k <= n.

## Output domain

`{T, F, U}`.

## Definition

`AtLeast(k, x1, ..., xn)` is `True` when at least `k` operands are `True`, `False` when fewer than `k` operands are `True` or `Unknown`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `AtLeast(k, a, b, ...)` |
| JSON | `{"op": "atLeast", "k": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: atLeast` with `k:` and an `operands:` list |
| `RuleBuilder` | `RuleBuilder.AtLeast(int k, params RuleBuilder[])`; `RuleBuilder.AtLeast(int k, IEnumerable<RuleBuilder>)` for a list whose length is known only at run time (it is not folded, so an empty or too short list is rejected like the `params` form) |

`AtLeast` has a real call form. The first argument is the integer `k`, written as a literal; the rest are the operands, so A call has no precedence (see [syntax](../specification/syntax.md#forms)). There is no symbol spelling.

## Aliases

None. The word is case-insensitive in the DSL (`atleast(1, a)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

Let $c$ be the number of operands that are `True`; for an `Unknown` operand $c$ is not yet fixed, only known to lie in $[d, p]$. `AtLeast` answers `True` when every count in that interval satisfies $c \ge k$, which holds exactly when $d \ge k$, and `False` when no count does, which holds exactly when $p < k$. Anything else is `Unknown`. This is the strongest extension of the Boolean threshold function, and the condition $c \ge k$ is monotone in $c$, so the two ends of the interval decide it.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), the true count lies in $[d, p]$:

$$\operatorname{AtLeast}_k(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } d \ge k \\ \mathsf{F} & \text{if } p < k \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, k = 1

<!-- k3:eval AtLeast n=3 k=1 -->
| Definitely true (d) | Possibly true (p) | AtLeast(1, ...) |
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

### 3 operands, k = 2

<!-- k3:eval AtLeast n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | AtLeast(2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | F |
| 1 | 2 | U |
| 1 | 3 | U |
| 2 | 2 | T |
| 2 | 3 | T |
| 3 | 3 | T |

### 3 operands, k = 3

<!-- k3:eval AtLeast n=3 k=3 -->
| Definitely true (d) | Possibly true (p) | AtLeast(3, ...) |
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

### 4 operands, k = 2

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval AtLeast n=4 k=2 -->
| Definitely true (d) | Possibly true (p) | AtLeast(2, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | F |
| 0 | 2 | U |
| 0 | 3 | U |
| 0 | 4 | U |
| 1 | 1 | F |
| 1 | 2 | U |
| 1 | 3 | U |
| 1 | 4 | U |
| 2 | 2 | T |
| 2 | 3 | T |
| 2 | 4 | T |
| 3 | 3 | T |
| 3 | 4 | T |
| 4 | 4 | T |

</details>

## Equivalent forms

`AtLeast` is a primitive, so it has no canonical form. These forms hold for every assignment.

`AtLeast(1, ...)` is `OR` and `AtLeast(n, ...)` is `AND`, over the same operands. They are the same function, including every `Unknown` case, and the same value at every operand count:

<!-- k3:canonical OR n=2..4 -->
```text
ATLEAST(1, ...)
```

<!-- k3:canonical AND vars=a,b -->
```text
ATLEAST(2, a, b)
```

<!-- k3:canonical AND vars=a,b,c -->
```text
ATLEAST(3, a, b, c)
```

<!-- k3:canonical AND vars=a,b,c,d -->
```text
ATLEAST(4, a, b, c, d)
```

With one operand `AtLeast(1, a)` is that operand:

<!-- k3:canonical AtLeast vars=a -->
```text
a
```

`AtLeast` and [AtMost](atmost.md) are duals through negation: counting `True` operands at least `k` is counting `False` operands at most `n - k`. For three operands:

<!-- k3:canonical AtLeast vars=a,b,c -->
```text
ATMOST(3 - k, NOT(a), NOT(b), NOT(c))
```

The strict form is the same thing shifted by one: `GreaterThan(k - 1, ...)` is `AtLeast(k, ...)` (see [GreaterThan](greaterthan.md)). `AtLeast` is not `PARITY` or `ExactlyOne`: it counts a lower bound and ignores how many operands go beyond it.

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `AtLeast(2, a, b, c)` | `True`, `True`, `False` | `True` | Two operands are `True`. |
| `AtLeast(2, a, b, c)` | `True`, `False`, `False` | `False` | One `True` and no `Unknown`, so two is out of reach. |
| `AtLeast(2, a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand decides whether the count reaches two. |
| `AtLeast(2, a, b, c)` | `True`, `True`, `Unknown` | `True` | Two are already `True`; the unknown operand cannot lower the count. |
| `AtLeast(2, a, b, c)` | `False`, `Unknown`, `False` | `False` | At most one operand could be `True`. |
| `AtLeast(1, a, b)` | `Unknown`, `Unknown` | `Unknown` | The same value as `a OR b`. |

## Edge cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `k` below 1 or above n | `InvalidThresholdValue` (`TRE0008`, see [diagnostics](../specification/diagnostics.md)) |
| No operands | `MalformedTree` (`TRE0014`) |
| `k` missing or not an integer in the DSL | `SyntaxError` (`TRE0001`) |
| `k` missing or not a number in JSON or YAML | `MalformedTree` (`TRE0014`) |

- The valid `k` range. `k` must satisfy 1 <= k <= n. `AtLeast(0, ...)` would always be `True` and `AtLeast(n + 1, ...)` always `False`, so the compiler rejects both instead of folding them.
- One operand. The compiler accepts `AtLeast(1, a)`, which is `a`. A larger `k` is out of range for one operand.
- `AtLeast(1)` and `AtLeast(n)` reduce to `OR` and `AND`. The values are identical, so `AtLeast(1, a, b)` and `a OR b` agree everywhere, `Unknown` included. They stay different nodes: the evaluator and printers keep the name that was written. `ANY` and `ALL` (see the [index](README.md)) are the named forms of the same two cases.
- `Unknown` is not absorbed in the middle. Only the two extremes settle early: `True` as soon as `d >= k`, `False` as soon as `p < k`. In between, every further `Unknown` operand keeps the result `Unknown`.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `AtLeast(2, isOn, boom, isOff)` | `Unknown` | 1 |
| `AtLeast(1, boom, isOn)` | `True` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `AtLeast(k)`, for example `AtLeast(2)`.
- `Simplify` collapses `AtLeast(1, ...)` to `OR` and `AtLeast(n, ...)` to `AND`. `ExpandToPrimitives` leaves `AtLeast` unchanged, because it is a primitive.

## Related operations

- [AtMost](atmost.md) is the upper-bound dual, and [Exactly](exactly.md) combines both bounds.
- [GreaterThan](greaterthan.md) is the strict form: `GreaterThan(k, ...)` is `AtLeast(k + 1, ...)`.
- [OR](../gates/or.md) is `AtLeast(1, ...)` and [AND](../gates/and.md) is `AtLeast(n, ...)`.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
