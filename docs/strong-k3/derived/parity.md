# PARITY

N-ary parity: `True` when an odd number of operands are `True` and none is `Unknown`, `False` when an even number are `True` and none is `Unknown`, and `Unknown` whenever any operand is `Unknown`. Back to the [Derived Logical Operations index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `PARITY`
- DSL: `PARITY(a, b, ...)`
- JSON and YAML `op`: `parity`
- `RuleBuilder` member: `RuleBuilder.Parity`

## Classification

- Category: Derived Logical Operations
- Category index: [Derived Logical Operations](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Derived. `PARITY` is defined as the disjunction of `Exactly(k, ...)` over every odd count `k` (see [Canonical form](#canonical-form)). It sits with the `XOR` family rather than the [Cardinality Functions](../cardinality/README.md), although its definition borrows from them. It stays a first-class node in the engine and is not rewritten to its definition unless a rewrite such as `ExpandToPrimitives` asks for it.

## Arity

Two or more operands. Fewer than two operands is a compile error, `MalformedTree` (`TRE0014`, see [diagnostics](../specification/diagnostics.md)). This holds for `PARITY(a)` and `PARITY()` in the DSL and for JSON, YAML and `RuleBuilder.Parity(params RuleBuilder[])`. See [Edge cases](#edge-cases) for the empty and single-operand conventions.

## Input domain

Each operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`PARITY(x1, ..., xn)` is `Unknown` if any operand is `Unknown`; otherwise it is `True` when an odd number of operands are `True` and `False` when an even number are.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `PARITY(a, b, c)` |
| JSON | `{"op": "parity", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: parity` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.Parity(params RuleBuilder[])` for two or more operands; `RuleBuilder.Parity(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`PARITY` is the one operation of this category with a real call form: `PARITY(a, b, c)` is DSL input, and the word is case-insensitive (`parity(a, b)`). A call has no precedence (see [syntax](../specification/syntax.md#forms)). There is no symbol spelling.

## Aliases

None.

`NXOR` is not an alias. It conventionally means negated XOR, that is `XNOR`, which is [EQUIVALENT](equivalent.md) here. That is the opposite of n-ary parity. Rule text, JSON or YAML that writes `NXOR` is rejected with a diagnostic that names `PARITY`. The word is reserved, so a predicate cannot take it.

## Formal semantics

`PARITY` is the strongest extension of the Boolean parity function: refining any `Unknown` operand to `True` or `False` flips the parity, so the result is definite only when every operand is. With no `Unknown` operand it is the exclusive or of all operands, equivalently the parity of the number of `True` operands.

## Formula

With $d$ the number of operands equal to $\mathsf{T}$ and $p$ the number equal to $\mathsf{T}$ or $\mathsf{U}$ ([notation](../specification/notation.md#counts-and-intervals)):

$$\operatorname{PARITY}(x_1, \dots, x_n) = \begin{cases} \mathsf{U} & \text{if } d < p \\ \mathsf{T} & \text{if } d = p \text{ and } d \bmod 2 = 1 \\ \mathsf{F} & \text{if } d = p \text{ and } d \bmod 2 = 0 \end{cases}$$

For two operands this is $a \oplus b$.

## Truth table

### Two operands

<!-- k3:truth PARITY -->
| a | b | PARITY(a, b) |
| --- | --- | --- |
| T | T | F |
| T | U | U |
| T | F | T |
| U | T | U |
| U | U | U |
| U | F | U |
| F | T | T |
| F | U | U |
| F | F | F |

### Three operands

<!-- k3:truth PARITY -->
| a | b | c | PARITY(a, b, c) |
| --- | --- | --- | --- |
| T | T | T | T |
| T | T | U | U |
| T | T | F | F |
| T | U | T | U |
| T | U | U | U |
| T | U | F | U |
| T | F | T | F |
| T | F | U | U |
| T | F | F | T |
| U | T | T | U |
| U | T | U | U |
| U | T | F | U |
| U | U | T | U |
| U | U | U | U |
| U | U | F | U |
| U | F | T | U |
| U | F | U | U |
| U | F | F | U |
| F | T | T | F |
| F | T | U | U |
| F | T | F | T |
| F | U | T | U |
| F | U | U | U |
| F | U | F | U |
| F | F | T | T |
| F | F | U | U |
| F | F | F | F |

### Four operands

Eighty-one rows, one for each assignment of `T`, `U` and `F` to four operands.

<details>
<summary>Show the 81-row table</summary>

<!-- k3:truth PARITY -->
| a | b | c | d | PARITY(a, b, c, d) |
| --- | --- | --- | --- | --- |
| T | T | T | T | F |
| T | T | T | U | U |
| T | T | T | F | T |
| T | T | U | T | U |
| T | T | U | U | U |
| T | T | U | F | U |
| T | T | F | T | T |
| T | T | F | U | U |
| T | T | F | F | F |
| T | U | T | T | U |
| T | U | T | U | U |
| T | U | T | F | U |
| T | U | U | T | U |
| T | U | U | U | U |
| T | U | U | F | U |
| T | U | F | T | U |
| T | U | F | U | U |
| T | U | F | F | U |
| T | F | T | T | T |
| T | F | T | U | U |
| T | F | T | F | F |
| T | F | U | T | U |
| T | F | U | U | U |
| T | F | U | F | U |
| T | F | F | T | F |
| T | F | F | U | U |
| T | F | F | F | T |
| U | T | T | T | U |
| U | T | T | U | U |
| U | T | T | F | U |
| U | T | U | T | U |
| U | T | U | U | U |
| U | T | U | F | U |
| U | T | F | T | U |
| U | T | F | U | U |
| U | T | F | F | U |
| U | U | T | T | U |
| U | U | T | U | U |
| U | U | T | F | U |
| U | U | U | T | U |
| U | U | U | U | U |
| U | U | U | F | U |
| U | U | F | T | U |
| U | U | F | U | U |
| U | U | F | F | U |
| U | F | T | T | U |
| U | F | T | U | U |
| U | F | T | F | U |
| U | F | U | T | U |
| U | F | U | U | U |
| U | F | U | F | U |
| U | F | F | T | U |
| U | F | F | U | U |
| U | F | F | F | U |
| F | T | T | T | T |
| F | T | T | U | U |
| F | T | T | F | F |
| F | T | U | T | U |
| F | T | U | U | U |
| F | T | U | F | U |
| F | T | F | T | F |
| F | T | F | U | U |
| F | T | F | F | T |
| F | U | T | T | U |
| F | U | T | U | U |
| F | U | T | F | U |
| F | U | U | T | U |
| F | U | U | U | U |
| F | U | U | F | U |
| F | U | F | T | U |
| F | U | F | U | U |
| F | U | F | F | U |
| F | F | T | T | F |
| F | F | T | U | U |
| F | F | T | F | T |
| F | F | U | T | U |
| F | F | U | U | U |
| F | F | U | F | U |
| F | F | F | T | T |
| F | F | F | U | U |
| F | F | F | F | F |

</details>

## Canonical form

The definition in primitives is the disjunction of `Exactly(k, ...)` over every odd count `k` up to the operand count. The form depends on the operand count, so it is shown for three and four operands:

<!-- k3:canonical PARITY vars=a,b,c -->
```text
OR(EXACTLY(1, a, b, c), EXACTLY(3, a, b, c))
```

<!-- k3:canonical PARITY vars=a,b,c,d -->
```text
OR(EXACTLY(1, a, b, c, d), EXACTLY(3, a, b, c, d))
```

With no `Unknown` operand the true count is a single number and the disjunction is `True` exactly when that number is odd. With an `Unknown` operand the true count lies in an interval of two or more consecutive numbers, so every `Exactly(k, ...)` is `Unknown` or `False`, at least one odd count lies in the interval, and the disjunction is `Unknown`. The form has linear size; a fold of `XOR` repeats its accumulator at every step and grows exponentially.

## Equivalent forms

`PARITY` equals the left fold of [XOR](xor.md), and for two operands it is `XOR`:

<!-- k3:canonical PARITY vars=a,b,c -->
```text
XOR(XOR(a, b), c)
```

<!-- k3:canonical PARITY vars=a,b -->
```text
XOR(a, b)
```

For two operands it is also `ExactlyOne`:

<!-- k3:canonical PARITY vars=a,b -->
```text
EXACTLYONE(a, b)
```

`PARITY` is commutative and associative: nesting `PARITY` calls or regrouping a fold gives the same value. Adding a `False` operand does not change it, and adding a `True` operand negates it.

### PARITY versus ExactlyOne versus XOR

Three operations are easy to confuse because they agree on two operands:

| | `XOR` | `PARITY` | `ExactlyOne` |
| --- | --- | --- | --- |
| Operands | exactly 2 | 2 or more | 2 or more |
| `True` when | the operands differ | an odd number are `True` | exactly one is `True` |
| An `Unknown` operand | always gives `Unknown` | always gives `Unknown` | can still give a definite value |

For two operands all three are the same function. From three operands `XOR` is not available (a chain is `TRE0006`, whose message names the other two), and `PARITY` and `ExactlyOne` part ways:

| `True` operands out of three, none unknown | `PARITY` | `ExactlyOne` |
| --- | --- | --- |
| 0 | `False` | `False` |
| 1 | `True` | `True` |
| 2 | `False` | `False` |
| 3 | `True` | `False` |

`PARITY(T, T, T)` is `True` and `ExactlyOne(T, T, T)` is `False`. The `Unknown` rule differs too: `PARITY(U, T, T)` is `Unknown`, because the unknown operand could make the count two or three, while `ExactlyOne(U, T, T)` is `False`, because two operands are already `True` and no refinement makes the count one. Choose `PARITY` for "an odd number", `ExactlyOne` for "exactly one".

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `PARITY(a, b, c)` | `True`, `True`, `True` | `True` | Three `True`, an odd number. |
| `PARITY(a, b, c)` | `True`, `True`, `False` | `False` | Two `True`, an even number. |
| `PARITY(a, b, c)` | `True`, `False`, `False` | `True` | One `True`, an odd number. |
| `PARITY(a, b, c)` | `True`, `True`, `Unknown` | `Unknown` | The unknown operand decides the parity. |
| `PARITY(a, b, c)` | `Unknown`, `Unknown`, `Unknown` | `Unknown` | Nothing is known. |

## Edge cases

- Unknown is never absorbed. Unlike `AND`, `OR` and the cardinality operations, no definite operand settles `PARITY`. One `Unknown` operand gives `Unknown`.
- Two operands give the same value as `XOR` but a different node, which prints as `PARITY(a, b)` rather than `a XOR b`.
- Empty and single-operand conventions. Mathematically the empty parity is `False`, the identity of the exclusive or, and the parity of one operand is that operand. The rule languages accept neither: `PARITY(a)` and `PARITY()` in the DSL, and JSON, YAML and `RuleBuilder.Parity(params RuleBuilder[])` with fewer than two operands compile to `MalformedTree`. Only `RuleBuilder.Parity(IEnumerable<RuleBuilder>)` applies the convention, at build time: an empty sequence becomes the constant `False` and a single operand is returned unchanged.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `PARITY(boom, isOn, isOn)` | `Unknown` | 1 |
| `PARITY(isOn, isOff, isOn, boom)` | `Unknown` | 1 |

## Evaluation behavior

- Every operand runs in both evaluation modes, and none is recorded as `NotEvaluated`. A faulting operand always records its fault, even when the result is already settled (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- `ExpandToPrimitives` expands `PARITY` to the odd-count disjunction rather than a fold, so the expansion grows linearly with the operand count.

## Related operations

- [XOR](xor.md) is the binary case.
- `ExactlyOne` in the [Cardinality Functions](../cardinality/README.md) is the other n-ary reading; see [PARITY versus ExactlyOne versus XOR](#parity-versus-exactlyone-versus-xor).
- [EQUIVALENT](equivalent.md) is the negation of `XOR`, not of `PARITY`; that difference is why the old name `NXOR` was removed.
