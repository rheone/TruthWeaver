# LessThan

`True` when fewer than `k` of the operands are `True`, `False` when `k` or more are already `True`, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `LessThan`
- DSL: `LessThan(k, a, b, ...)`
- JSON and YAML `op`: `lessThan`, with an integer `k`
- `RuleBuilder` member: `RuleBuilder.LessThan`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `LessThan(k, ...)` is defined as `AtMost(k - 1, ...)` (see [Canonical form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

One or more operands after the integer `k`, with 1 <= k <= n for n operands. These are exactly the values for which `AtMost(k - 1, ...)` is valid, so the definition never leaves the valid range.

## Input domain

Each operand is a value in `{T, F, U}`. The parameter `k` is an integer, with 1 <= k <= n.

## Output domain

`{T, F, U}`.

## Definition

`LessThan(k, x1, ..., xn)` is `True` when fewer than `k` operands can be `True` (fewer than `k` are `True` or `Unknown`), `False` when at least `k` operands are `True`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `LessThan(k, a, b, ...)` |
| JSON | `{"op": "lessThan", "k": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: lessThan` with `k:` and an `operands:` list |
| `RuleBuilder` | `RuleBuilder.LessThan(int k, params RuleBuilder[])`; there is no `IEnumerable` overload |

`LessThan` has a real call form. The first argument is the integer `k`, written as a literal; the rest are the operands, so A call has no precedence (see [syntax](../specification/syntax.md#forms)). There is no symbol spelling. `RuleBuilder` has only the `params` overload for `LessThan`; there is no `IEnumerable` overload.

## Aliases

None. The word is case-insensitive in the DSL (`lessthan(1, a)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

Let $c$ be the number of `True` operands, known only to lie in $[d, p]$. `LessThan` answers `True` when every count in the interval satisfies $c < k$, which holds exactly when $p < k$, and `False` when none does, which holds exactly when $d \ge k$. Anything else is `Unknown`. Since counts are integers, $c < k$ is $c \le k - 1$, which is why the operation equals [AtMost](atmost.md) with the threshold lowered by one.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), the true count lies in $[d, p]$:

$$\operatorname{LessThan}_k(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } p < k \\ \mathsf{F} & \text{if } d \ge k \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, k = 1

<!-- k3:eval LessThan n=3 k=1 -->
| Definitely true (d) | Possibly true (p) | LessThan(1, ...) |
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

### 3 operands, k = 2

<!-- k3:eval LessThan n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | LessThan(2, ...) |
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

### 3 operands, k = 3

<!-- k3:eval LessThan n=3 k=3 -->
| Definitely true (d) | Possibly true (p) | LessThan(3, ...) |
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

### 4 operands, k = 2

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval LessThan n=4 k=2 -->
| Definitely true (d) | Possibly true (p) | LessThan(2, ...) |
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

## Canonical form

The definition in primitives, for every operand count and every valid `k`:

<!-- k3:canonical LessThan n=1..4 -->
```text
ATMOST(k - 1, ...)
```

The compiler's valid ranges map exactly: `1 <= k <= n` for `LessThan` is `0 <= k - 1 <= n - 1` for `AtMost`.

## Equivalent forms

`LessThan(k, ...)` is the negation of `AtLeast(k, ...)`, over the same operands:

<!-- k3:canonical LessThan n=1..4 -->
```text
NOT(ATLEAST(k, ...))
```

`LessThan(1, ...)` is `NONE`, the same function, `Unknown` cases included, and with one operand it is the negation of that operand:

<!-- k3:canonical NONE n=2..4 -->
```text
LESSTHAN(1, ...)
```

<!-- k3:canonical LessThan vars=a -->
```text
NOT(a)
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `LessThan(2, a, b, c)` | `True`, `False`, `False` | `True` | One operand is `True`, which is fewer than two. |
| `LessThan(2, a, b, c)` | `True`, `True`, `False` | `False` | Two operands are already `True`. |
| `LessThan(2, a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand decides whether the count reaches two. |
| `LessThan(2, a, b, c)` | `False`, `Unknown`, `False` | `True` | At most one operand could be `True`. |
| `LessThan(1, a, b)` | `Unknown`, `False` | `Unknown` | The same value as `NOT (a OR b)`. |

## Edge cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `k` below 1 or above n | `InvalidThresholdValue` (`TRE0008`, see [diagnostics](../specification/diagnostics.md)) |
| No operands | `InfixArityViolation` (`TRE0006`) |
| `k` missing, fractional or outside the `int` range | `InvalidThresholdValue` (`TRE0008`) |

- The valid `k` range. `k` must satisfy 1 <= k <= n. `LessThan(0, ...)` would always be `False` and `k > n` always `True`, so the compiler rejects both instead of folding them.
- One operand. The compiler accepts `LessThan(1, a)`, which is `NOT a`. `LessThan(2, a)` is rejected.
- `LessThan(1)` is `NONE`. Fewer than one `True` operand is none.
- Off-by-one against `AtMost`. `LessThan(k)` allows one fewer true operand than `AtMost(k)`, so `LessThan(2, a, b, c)` is `AtMost(1, a, b, c)`.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `LessThan(2, isOn, isOff, boom)` | `Unknown` | 1 |
| `LessThan(1, isOn, boom, isOff)` | `False` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The trace labels the node `LessThan(k)`, for example `LessThan(2)`.
- `ExpandToPrimitives` rewrites `LessThan(k, ...)` to `AtMost(k - 1, ...)`, and the simplifier does the same.

## Related operations

- [AtMost](atmost.md) is the non-strict form; `LessThan(k, ...)` is `AtMost(k - 1, ...)`.
- [GreaterThan](greaterthan.md) is the strict lower bound, and [AtLeast](atleast.md) is its non-strict form.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
