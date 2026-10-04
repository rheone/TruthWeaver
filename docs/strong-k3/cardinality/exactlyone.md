# ExactlyOne

`True` when exactly one operand is `True` and none is `Unknown`, `False` when two operands are already `True` or none can be, and `Unknown` otherwise. Back to the [Cardinality Functions index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `ExactlyOne`
- DSL: `ExactlyOne(a, b, ...)`
- JSON and YAML `op`: `exactlyOne`
- `RuleBuilder` member: `RuleBuilder.ExactlyOne`

## Classification

- Category: Cardinality Functions
- Category index: [Cardinality Functions](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `ExactlyOne(...)` is defined as `Exactly(1, ...)` (see [Canonical Form](#canonical-form)). It stays a first-class node in the engine and is rewritten to its definition only when a rewrite such as `ExpandToPrimitives` asks for it ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 3).

## Arity

Two or more operands, and no parameter. Fewer than two operands is a compile error, `MalformedTree` (`TRE0014`): "This operator requires at least 2 operands but found N." This holds for `ExactlyOne(a)` and `ExactlyOne()` in the DSL and for JSON, YAML and `RuleBuilder.ExactlyOne(params RuleBuilder[])`. The threshold operations are different: `Exactly(1, a)` compiles. See [Edge Cases](#edge-cases) for the empty and single-operand conventions.

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`ExactlyOne(x1, ..., xn)` is `True` when exactly one operand is `True` and none is `Unknown`, `False` when two or more operands are `True` or when no operand is `True` or `Unknown`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `ExactlyOne(a, b, c)` |
| JSON | `{"op": "exactlyOne", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: exactlyOne` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.ExactlyOne(params RuleBuilder[])` for two or more operands; `RuleBuilder.ExactlyOne(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`ExactlyOne` has a real call form with no parameter: every argument is an operand. A call has no precedence, so it needs no parentheses when mixed with `AND`, `OR` or the infix operators, and the word is case-insensitive. There is no symbol spelling.

## Aliases

None. The word is case-insensitive in the DSL (`exactlyone(a, b)`) and the JSON and YAML `op` is case-insensitive on read.

## Formal Semantics

`ExactlyOne` is `Exactly` with `k = 1`: it answers `True` when every count in the interval $[d, p]$ of possible true counts is one, `False` when one is not in the interval, and `Unknown` otherwise ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). One lies outside the interval when $d \ge 2$ (too many `True` already) or $p = 0$ (none can be `True`).

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)):

$$\operatorname{ExactlyOne}(x_1, \dots, x_n) = \begin{cases} \mathsf{F} & \text{if } d \ge 2 \text{ or } p = 0 \\ \mathsf{T} & \text{if } d = p = 1 \\ \mathsf{U} & \text{otherwise} \end{cases}$$

## Evaluation Table

Cardinality operations are parameterised and variadic, so a truth table is impractical. Each row gives the number of operands that are definitely `True` (d), the number that are `True` or `Unknown` (p) and the result; the operands that make up the rest are `False`. The result depends on the operands only through this pair ([semantics](../specification/semantics.md#cardinality-uses-an-interval)). Every pair with 0 <= d <= p <= n appears once.

### 2 operands

<!-- k3:eval ExactlyOne n=2 -->
| Definitely true (d) | Possibly true (p) | ExactlyOne(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
| 0 | 2 | U |
| 1 | 1 | T |
| 1 | 2 | U |
| 2 | 2 | F |

### 3 operands

<!-- k3:eval ExactlyOne n=3 -->
| Definitely true (d) | Possibly true (p) | ExactlyOne(...) |
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

### 4 operands

<!-- k3:eval ExactlyOne n=4 -->
| Definitely true (d) | Possibly true (p) | ExactlyOne(...) |
| --- | --- | --- |
| 0 | 0 | F |
| 0 | 1 | U |
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

## Canonical Form

The definition in primitives, for two or more operands:

<!-- k3:canonical ExactlyOne n=2..4 -->
```text
EXACTLY(1, ...)
```

## Equivalent Forms

For two operands `ExactlyOne` is [XOR](../derived/xor.md):

<!-- k3:canonical ExactlyOne vars=a,b -->
```text
XOR(a, b)
```

For three or more operands there is no `XOR` form: a chain of `XOR` is a compile error, and the chain would mean something else anyway.

### ExactlyOne versus PARITY versus XOR

These three agree on two operands and part ways from three. `ExactlyOne` is `True` for one `True` operand and only one; [PARITY](../derived/parity.md) is `True` for any odd number. With no `Unknown` operand:

| `True` operands out of three | `ExactlyOne` | `PARITY` |
| --- | --- | --- |
| 0 | `False` | `False` |
| 1 | `True` | `True` |
| 2 | `False` | `False` |
| 3 | `False` | `True` |

The `Unknown` rule differs too. `PARITY` is `Unknown` whenever any operand is. `ExactlyOne` can still be definite: `ExactlyOne(U, T, T)` is `False`, because two operands are already `True` and no refinement makes the count one, while `PARITY(U, T, T)` is `Unknown`. The full comparison, including `XOR`, is in [PARITY versus ExactlyOne versus XOR](../derived/parity.md#parity-versus-exactlyone-versus-xor). Choose `ExactlyOne` for "exactly one", `PARITY` for "an odd number".

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `ExactlyOne(a, b, c)` | `True`, `False`, `False` | `True` | One operand is `True`. |
| `ExactlyOne(a, b, c)` | `True`, `True`, `True` | `False` | Three operands are `True`. |
| `ExactlyOne(a, b, c)` | `False`, `False`, `False` | `False` | No operand is `True`. |
| `ExactlyOne(a, b, c)` | `True`, `Unknown`, `False` | `Unknown` | The unknown operand decides whether the count is one or two. |
| `ExactlyOne(a, b, c)` | `True`, `True`, `Unknown` | `False` | Two are already `True`; the unknown operand cannot lower the count. |
| `ExactlyOne(a, b, c)` | `False`, `Unknown`, `False` | `Unknown` | The unknown operand is the only way to reach one. |

## Edge Cases

- **Two or more operands.** `ExactlyOne` rejects one or no operands at compile time, unlike `Exactly(1, a)`. A single operand is not "exactly one of one" in the rule languages; write the operand itself.
- **Empty and single-operand conventions.** Only `RuleBuilder.ExactlyOne(IEnumerable<RuleBuilder>)` applies a convention, at build time: an empty sequence becomes the constant `False` and a single operand is returned unchanged. The rule languages and `RuleBuilder.ExactlyOne(params RuleBuilder[])` reject fewer than two operands.
- **`Unknown` does not always win.** A definite `False` needs either two `True` operands (`d >= 2`) or no operand that could be `True` (`p = 0`). Otherwise any `Unknown` operand leaves the result `Unknown`.
- **No short-circuit.** Every operand is evaluated, in the default mode as well as in `EvaluationMode.Exhaustive`, and none is recorded as `NotEvaluated`, so a faulting operand always records its fault, even when the result is already `False`.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `ExactlyOne(isOn, isOff, boom)` | `Unknown` | 1 |
| `ExactlyOne(isOn, isOn, boom)` | `False` | 1 |

## Implementation Notes

- The evaluator reports the node as `ExactlyOne` in the trace and the trace tree.
- `ExpandToPrimitives` rewrites `ExactlyOne(...)` to `Exactly(1, ...)`, and the simplifier does the same ([ADR-0005](../../adr/0005-strong-k3-language-surface.md)).

## Related Operations

- [Exactly](exactly.md) is the general form with a chosen `k`.
- [PARITY](../derived/parity.md) is the odd-count reading and [XOR](../derived/xor.md) the binary exclusive or; see [ExactlyOne versus PARITY versus XOR](#exactlyone-versus-parity-versus-xor).
- The [Cardinality Functions index](README.md) lists the remaining operations of the family.
