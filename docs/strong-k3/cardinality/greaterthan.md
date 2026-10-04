# GreaterThan

`True` when more than `k` of the operands are `True`, `False` when `k` or fewer operands can be `True`, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `GreaterThan`
- DSL: `GreaterThan(k, a, b, ...)`
- JSON and YAML `op`: `greaterThan`, with an integer `k`
- `RuleBuilder` member: `RuleBuilder.GreaterThan`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `GreaterThan(k, ...)` is defined as `AtLeast(k + 1, ...)` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

One or more operands after the integer `k`, with 0 <= k <= n - 1 for n operands. These are exactly the values for which `AtLeast(k + 1, ...)` is valid, so the definition never leaves the valid range.

> [!NOTE]
> The engine accepts a single operand for this operation. `OperatorDefinitions` records a minimum of two operands for the threshold family, but the compiler does not enforce it. This reference follows the compiler; the mismatch is tracked in hardening ticket 11 ([PROPOSAL.md](../PROPOSAL.md), open question 10).

## Input Domain

Each operand is a value in `{T, F, U}`. The parameter `k` is an integer, with 0 <= k <= n - 1.

## Output Domain

`{T, F, U}`.

## Definition

`GreaterThan(k, x1, ..., xn)` is `True` when more than `k` operands are `True`, `False` when no more than `k` operands are `True` or `Unknown`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `GreaterThan(k, a, b, ...)` |
| JSON | `{"op": "greaterThan", "k": 2, "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: greaterThan` with `k:` and an `operands:` list |
| `RuleBuilder` | `RuleBuilder.GreaterThan(int k, params RuleBuilder[])`; there is no `IEnumerable` overload |

`GreaterThan` has a real call form. The first argument is the integer `k`, written as a literal; the rest are the operands, so `k` comes first, as in the table above. A call has no precedence, so it needs no parentheses when mixed with `AND`, `OR` or the infix operators, and the word is case-insensitive. There is no symbol spelling.

## Aliases

None. The word is case-insensitive in the DSL (`greaterthan(1, a)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal Semantics

Let $c$ be the number of `True` operands, known only to lie in $[d, p]$. `GreaterThan` answers `True` when every count in the interval satisfies $c > k$, which holds exactly when $d > k$, and `False` when none does, which holds exactly when $p \le k$. Anything else is `Unknown`. Since counts are integers, $c > k$ is $c \ge k + 1$, which is why the operation equals [AtLeast](atleast.md) with the threshold raised by one.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)), the true count lies in $[d, p]$:

$$\operatorname{GreaterThan}_k(x_1, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if } d > k \\ \mathsf{F} & \text{if } p \le k \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation Table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 3 operands, k = 0

<!-- k3:eval GreaterThan n=3 k=0 -->
| Definitely true (d) | Possibly true (p) | GreaterThan(0, ...) |
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

### 3 operands, k = 1

<!-- k3:eval GreaterThan n=3 k=1 -->
| Definitely true (d) | Possibly true (p) | GreaterThan(1, ...) |
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

### 3 operands, k = 2

<!-- k3:eval GreaterThan n=3 k=2 -->
| Definitely true (d) | Possibly true (p) | GreaterThan(2, ...) |
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

### 4 operands, k = 1

The same table for four operands: 15 rows, one for each pair of counts.

<details>
<summary>Show the 15-row table</summary>

<!-- k3:eval GreaterThan n=4 k=1 -->
| Definitely true (d) | Possibly true (p) | GreaterThan(1, ...) |
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

## Canonical Form

The definition in primitives, for every operand count and every valid `k`:

<!-- k3:canonical GreaterThan n=1..4 -->
```text
ATLEAST(k + 1, ...)
```

The compiler's valid ranges map exactly: `0 <= k <= n - 1` for `GreaterThan` is `1 <= k + 1 <= n` for `AtLeast`.

## Equivalent Forms

`GreaterThan(k, ...)` is the negation of `AtMost(k, ...)`, over the same operands:

<!-- k3:canonical GreaterThan n=1..4 -->
```text
NOT(ATMOST(k, ...))
```

`GreaterThan(0, ...)` is `OR`, the same function, `Unknown` cases included, and with one operand it is that operand:

<!-- k3:canonical OR n=2..4 -->
```text
GREATERTHAN(0, ...)
```

<!-- k3:canonical GreaterThan vars=a -->
```text
a
```

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `GreaterThan(1, a, b, c)` | `True`, `True`, `False` | `True` | Two operands are `True`, which is more than one. |
| `GreaterThan(1, a, b, c)` | `True`, `False`, `False` | `False` | One operand is `True`, not more than one. |
| `GreaterThan(1, a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand decides whether the count passes one. |
| `GreaterThan(1, a, b, c)` | `False`, `Unknown`, `False` | `False` | At most one operand could be `True`. |
| `GreaterThan(0, a, b)` | `Unknown`, `False` | `Unknown` | The same value as `a OR b`. |

## Edge Cases

Rejected at compile time, in the DSL, JSON, YAML and `RuleBuilder` alike:

| Input | Diagnostic |
| --- | --- |
| `k` below 0 or above n - 1 | `InvalidThresholdValue` (`TRE0008`). For two operands and `k` = 2: "GreaterThan's threshold k=2 must satisfy 0 <= k <= 1 for 2 operand(s) (any value outside that range makes the result a structural constant)." |
| No operands | `MalformedTree` (`TRE0014`): "GreaterThan requires at least one operand." |
| `k` missing or not an integer in the DSL | `SyntaxError` (`TRE0001`): "Expected an integer threshold as GreaterThan's first argument." |
| `k` missing or not a number in JSON or YAML | `MalformedTree` (`TRE0014`): "'greaterThan' requires a numeric 'k'." |

The out-of-range values are rejected because they make the result a constant: a negative `k` is always `True`; `k` of n or more is always `False`.

- **The valid `k` range.** `k` must satisfy 0 <= k <= n - 1. A negative `k` would always be `True` and `k >= n` always `False`, so the compiler rejects both instead of folding them.
- **One operand.** The compiler accepts `GreaterThan(0, a)`, which is `a`. `GreaterThan(1, a)` is rejected.
- **`GreaterThan(0)` is `OR`.** Fewer than one `True` operand is none, so "more than zero" is "at least one".
- **Off-by-one against `AtLeast`.** `GreaterThan(k)` needs `k + 1` operands, so `GreaterThan(2, a, b, c)` is `a AND b AND c`, not "at least two".
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault, even when the result is already settled.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). An `Unknown` operand counts as possibly true, never as definitely true. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `GreaterThan(1, isOn, isOff, boom)` | `Unknown` | 1 |
| `GreaterThan(0, isOn, boom, isOff)` | `True` | 1 |

## Implementation Notes

- The evaluator reports the node as `GreaterThan(k)` in the trace and the trace tree, for example `GreaterThan(1)`.
- `ExpandToPrimitives` rewrites `GreaterThan(k, ...)` to `AtLeast(k + 1, ...)`, and the simplifier collapses it to `AtLeast(k + 1, ...)` as well ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)).
- `RuleBuilder` has only the `params` overload for `GreaterThan`; there is no `IEnumerable` overload.

## Related Operations

- [AtLeast](atleast.md) is the non-strict form; `GreaterThan(k, ...)` is `AtLeast(k + 1, ...)`.
- [LessThan](lessthan.md) is the strict upper bound, and [AtMost](atmost.md) is its non-strict form.
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
