# Exactly

`True` when exactly `k` of the operands are `True` and none is `Unknown`, `False` when `k` is outside the possible counts, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `Exactly`
- DSL: `Exactly(k, a, b, ...)`
- JSON and YAML `op`: `exactly`, with an integer `k`
- `RuleBuilder` member: `RuleBuilder.Exactly`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `Exactly` has no definition in other Operations. It is one of the three counting primitives with [AtLeast](atleast.md) and [AtMost](atmost.md), although it equals `AND(AtLeast(k), AtMost(k))` over the same operands. [ExactlyOne](exactlyone.md) is its special case `Exactly(1, ...)`.

## Arity

One or more operands after the integer `k`, with 0 <= k <= n for n operands. Unlike [AtLeast](atleast.md) and [AtMost](atmost.md), both ends of the count range are allowed: `Exactly(0, ...)` and `Exactly(n, ...)` are meaningful, not constants.

## Input domain

Each operand is a value in `{T, F, U}`. The parameter `k` is an integer, with 0 <= k <= n.

## Output domain

`{T, F, U}`.

## Definition

`Exactly(k, x1, ..., xn)` is `True` when exactly `k` operands are `True` and no operand is `Unknown`, `False` when no refinement of the `Unknown` operands makes the count `k`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `Exactly(k, a, b, ...)` |
| JSON | `{"op": "exactly", "k": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: exactly` with `k:` and an `operands:` list |
| `RuleBuilder` | `RuleBuilder.Exactly(int k, params RuleBuilder[])`; `RuleBuilder.Exactly(int k, IEnumerable<RuleBuilder>)` for a list whose length is known only at run time (it is not folded, so an empty or too short list is rejected like the `params` form) |

`Exactly` has a real call form. The first argument is the integer `k`, written as a literal; the rest are the operands, so A call has no precedence (see [syntax](../specification/syntax.md#forms)). There is no symbol spelling.

## Aliases

None. The word is case-insensitive in the DSL (`exactly(1, a)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

Let $c$ be the number of `True` operands, known only to lie in $[d, p]$. `Exactly` answers `True` when every count in the interval equals $k$, which needs the interval to be the single count $d = p = k$, and `False` when no count does, which holds exactly when $k \notin [d, p]$. Anything else is `Unknown`. The condition $c = k$ is not monotone in $c$, which is why this operation, unlike `AtLeast` and `AtMost`, can be `False` while operands are still `Unknown`, and can never be `True` while any is.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), the true count lies in $[d, p]$:

$$\operatorname{Exactly}_k(x_1, \dots, x_n) = \begin{cases} \mathsf{F} & \text{if } k < d \text{ or } k > p \\ \mathsf{T} & \text{if } d = p = k \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, k = 0

<!-- k3:eval Exactly n=3 k=0 -->
| Definitely true (d) | Possibly true (p) | Exactly(0, ...) |
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

<!-- k3:eval Exactly n=3 k=1 -->
| Definitely true (d) | Possibly true (p) | Exactly(1, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 0 | 3 | U |
| 1 | 1 | T |
| 1 | 2 | U |
| 1 | 3 | U |
| 2 | 2 | F |
| 2 | 3 | F |
| 3 | 3 | F |

### 3 operands, k = 2

<!-- k3:eval Exactly n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | Exactly(2, ...) |
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

### 3 operands, k = 3

<!-- k3:eval Exactly n=3 k=3 -->
| Definitely true (d) | Possibly true (p) | Exactly(3, ...) |
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

<!-- k3:eval Exactly n=4 k=2 -->
| Definitely true (d) | Possibly true (p) | Exactly(2, ...) |
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
| 2 | 3 | U |
| 2 | 4 | U |
| 3 | 3 | F |
| 3 | 4 | F |
| 4 | 4 | F |

</details>

### 1 operand, k = 0

<!-- k3:eval Exactly n=1 k=0 -->
| Definitely true (d) | Possibly true (p) | Exactly(0, ...) |
| --- | --- | --- |
| 0 | 0 | T |
| 0 | 1 | U |
| 1 | 1 | F |

### 1 operand, k = 1

<!-- k3:eval Exactly n=1 k=1 -->
| Definitely true (d) | Possibly true (p) | Exactly(1, ...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 1 | 1 | T |

## Equivalent forms

`Exactly` is a primitive, so it has no canonical form. These forms hold for every assignment.

`Exactly(k, ...)` is the conjunction of a lower and an upper bound on the same operands:

<!-- k3:canonical Exactly n=2..4 -->
```text
AND(ATLEAST(k, ...), ATMOST(k, ...))
```

The identity holds for every `k` from 0 to n as a statement about values. The compiler, however, rejects `AtLeast(0, ...)` and `AtMost(n, ...)`, so a rule that spells `Exactly(0, ...)` or `Exactly(n, ...)` out of its parts drops the vacuous bound: `Exactly(0, ...)` is `AtMost(0, ...)` and `Exactly(n, ...)` is `AtLeast(n, ...)`. The tables above show it, since the k = 0 rows equal [AtMost](atmost.md) and the k = n rows equal [AtLeast](atleast.md) for the same counts.

`Exactly(1, ...)` is [ExactlyOne](exactlyone.md) for two or more operands, and with one operand `Exactly(1, a)` is that operand and `Exactly(0, a)` is its negation (the one-operand tables above).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `Exactly(2, a, b, c)` | `True`, `True`, `False` | `True` | Exactly two operands are `True`. |
| `Exactly(2, a, b, c)` | `True`, `True`, `True` | `False` | Three operands are `True`, not two. |
| `Exactly(2, a, b, c)` | `True`, `False`, `False` | `False` | One `True` and no `Unknown`: two is out of reach. |
| `Exactly(2, a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand decides whether the count is two. |
| `Exactly(2, a, b, c)` | `True`, `True`, `Unknown` | `Unknown` | The unknown operand would make the count three if `True`. |
| `Exactly(1, a, b, c)` | `True`, `True`, `Unknown` | `False` | Two operands are already `True`, so the count cannot be one. |

## Edge cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `k` below 0 or above n | `InvalidThresholdValue` (`TRE0008`, see [diagnostics](../specification/diagnostics.md)) |
| No operands | `MalformedTree` (`TRE0014`) |
| `k` missing or not an integer in the DSL | `SyntaxError` (`TRE0001`) |
| `k` missing or not a number in JSON or YAML | `MalformedTree` (`TRE0014`) |

- The valid `k` range. `k` must satisfy 0 <= k <= n. A negative `k` or one above n would always be `False`, so the compiler rejects it instead of folding it.
- One operand. The compiler accepts `Exactly(1, a)` (which is `a`) and `Exactly(0, a)` (which is `NOT a`).
- `Unknown` blocks `True`. `Exactly` is `True` only when the count is certain. A single `Unknown` operand with `k` inside the interval always gives `Unknown`, whatever the other operands are.
- `Unknown` can still allow `False`. When `k` lies outside $[d, p]$ the result is `False` even with `Unknown` operands. This is the difference from [PARITY](../derived/parity.md), which is `Unknown` whenever any operand is.
- `Exactly(k)` is not `ExactlyOne`. [ExactlyOne](exactlyone.md) fixes `k = 1` and requires two or more operands.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `Exactly(1, isOn, isOff, boom)` | `Unknown` | 1 |
| `Exactly(2, isOn, isOff, boom)` | `Unknown` | 1 |
| `Exactly(0, isOn, isOff, boom)` | `False` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `Exactly(k)`, for example `Exactly(2)`.
- `ExpandToPrimitives` leaves `Exactly` unchanged, because it is a primitive, and expands [ExactlyOne](exactlyone.md) and `PARITY` into `Exactly` nodes.

## Related operations

- [AtLeast](atleast.md) and [AtMost](atmost.md) are the two bounds that `Exactly` combines.
- [ExactlyOne](exactlyone.md) is `Exactly(1, ...)`; [PARITY](../derived/parity.md) is the disjunction of `Exactly(k, ...)` over every odd `k`.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
