# AND

Conjunction: `True` only when every operand is `True`, and `False` as soon as any operand is `False`. Back to the [Gates / Operators index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `AND`
- DSL: `AND` (also `&&` and `∧`, see [Aliases](#aliases))
- JSON and YAML `op`: `and`
- `RuleBuilder` member: `RuleBuilder.And`

## Classification

- Category: Gates / Operators
- Category index: [Gates / Operators](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `AND` has no definition in other Operations. It is one of the three connectives from which the others are defined ([semantics](../specification/semantics.md#the-three-primitive-connectives)).

## Arity

Two or more operands. The engine builds one flat node holding all operands, so `a AND b AND c` is a single three-operand `AND`, not two nested ones. Fewer than two operands is a compile error, `MalformedTree` (`TRE0014`, see [diagnostics](../specification/diagnostics.md)). See [Edge cases](#edge-cases) for the empty and single-operand conventions.

## Input domain

Each operand is a value in `{T, F, U}`.

## Output domain

`{T, F, U}`.

## Definition

`AND` is `True` when every operand is `True`, `False` when at least one operand is `False`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a AND b AND c` |
| DSL, symbols | `a && b`, `a ∧ b` |
| JSON | `{"op": "and", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: and` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.And(params RuleBuilder[])` for two or more operands; `RuleBuilder.And(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`AND` is an infix operator with no call form: `AND(a, b)` is a syntax error. It binds tighter than `OR` and looser than `NOT`, so `a OR b AND c` is `a OR (b AND c)` and `NOT a AND b` is `(NOT a) AND b`. The canonical printer writes the word form and a flat chain. The mixing rule is in [syntax](../specification/syntax.md#the-mixing-rule).

## Aliases

| Alias | Kind |
| --- | --- |
| `&&` | Symbol |
| `∧` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive (`and`, `And`, `AND`). A lone `&` is a syntax error.

## Formal semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `AND` is the minimum of its operands. The minimum is the least true operand, so a single `False` sets the result and a single `Unknown` caps it at `Unknown`.

## Formula

$$x_1 \land x_2 \land \dots \land x_n = \min(x_1, x_2, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if every } x_i = \mathsf{T} \\ \mathsf{F} & \text{if some } x_i = \mathsf{F} \\ \mathsf{U} & \text{otherwise} \end{cases}$$

For two operands this is $a \land b = \min(a, b)$.

## Truth table

`False` dominates: every row with an `F` in any column has result `F`, whatever the other columns hold, `U` included. A result of `T` needs every column `T`. Every other row is `U`.

### Two operands

<!-- k3:truth AND -->
| a | b | AND(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | U |
| T | F | F |
| U | T | U |
| U | U | U |
| U | F | F |
| F | T | F |
| F | U | F |
| F | F | F |

### Three operands

<!-- k3:truth AND -->
| a | b | c | AND(a, b, c) |
| --- | --- | --- | --- |
| T | T | T | T |
| T | T | U | U |
| T | T | F | F |
| T | U | T | U |
| T | U | U | U |
| T | U | F | F |
| T | F | T | F |
| T | F | U | F |
| T | F | F | F |
| U | T | T | U |
| U | T | U | U |
| U | T | F | F |
| U | U | T | U |
| U | U | U | U |
| U | U | F | F |
| U | F | T | F |
| U | F | U | F |
| U | F | F | F |
| F | T | T | F |
| F | T | U | F |
| F | T | F | F |
| F | U | T | F |
| F | U | U | F |
| F | U | F | F |
| F | F | T | F |
| F | F | U | F |
| F | F | F | F |

### Four operands

Eighty-one rows, one for each assignment of `T`, `U` and `F` to four operands.

<details>
<summary>Show the 81-row table</summary>

<!-- k3:truth AND -->
| a | b | c | d | AND(a, b, c, d) |
| --- | --- | --- | --- | --- |
| T | T | T | T | T |
| T | T | T | U | U |
| T | T | T | F | F |
| T | T | U | T | U |
| T | T | U | U | U |
| T | T | U | F | F |
| T | T | F | T | F |
| T | T | F | U | F |
| T | T | F | F | F |
| T | U | T | T | U |
| T | U | T | U | U |
| T | U | T | F | F |
| T | U | U | T | U |
| T | U | U | U | U |
| T | U | U | F | F |
| T | U | F | T | F |
| T | U | F | U | F |
| T | U | F | F | F |
| T | F | T | T | F |
| T | F | T | U | F |
| T | F | T | F | F |
| T | F | U | T | F |
| T | F | U | U | F |
| T | F | U | F | F |
| T | F | F | T | F |
| T | F | F | U | F |
| T | F | F | F | F |
| U | T | T | T | U |
| U | T | T | U | U |
| U | T | T | F | F |
| U | T | U | T | U |
| U | T | U | U | U |
| U | T | U | F | F |
| U | T | F | T | F |
| U | T | F | U | F |
| U | T | F | F | F |
| U | U | T | T | U |
| U | U | T | U | U |
| U | U | T | F | F |
| U | U | U | T | U |
| U | U | U | U | U |
| U | U | U | F | F |
| U | U | F | T | F |
| U | U | F | U | F |
| U | U | F | F | F |
| U | F | T | T | F |
| U | F | T | U | F |
| U | F | T | F | F |
| U | F | U | T | F |
| U | F | U | U | F |
| U | F | U | F | F |
| U | F | F | T | F |
| U | F | F | U | F |
| U | F | F | F | F |
| F | T | T | T | F |
| F | T | T | U | F |
| F | T | T | F | F |
| F | T | U | T | F |
| F | T | U | U | F |
| F | T | U | F | F |
| F | T | F | T | F |
| F | T | F | U | F |
| F | T | F | F | F |
| F | U | T | T | F |
| F | U | T | U | F |
| F | U | T | F | F |
| F | U | U | T | F |
| F | U | U | U | F |
| F | U | U | F | F |
| F | U | F | T | F |
| F | U | F | U | F |
| F | U | F | F | F |
| F | F | T | T | F |
| F | F | T | U | F |
| F | F | T | F | F |
| F | F | U | T | F |
| F | F | U | U | F |
| F | F | U | F | F |
| F | F | F | T | F |
| F | F | F | U | F |
| F | F | F | F | F |

</details>

## Equivalent forms

De Morgan's law defines `AND` from `OR` and `NOT`:

<!-- k3:canonical AND vars=a,b -->
```text
NOT(OR(NOT(a), NOT(b)))
```

`AND` is associative, so an n-ary node equals its binary nesting:

<!-- k3:canonical AND vars=a,b,c -->
```text
AND(AND(a, b), c)
```

As a value, `AND` of two to four operands equals `ALL` of the same operands. They stay distinct Operations, because `ALL` is a cardinality operation and keeps its own node when a rule round-trips:

<!-- k3:canonical AND n=2..4 -->
```text
ALL(...)
```

The other laws that hold for `AND` (commutativity, idempotence, distributivity over `OR`, absorption, the identity `a AND True = a`, the annihilator `a AND False = False`) are listed in [laws that hold](../specification/semantics.md#laws-that-hold). The law `a AND NOT a = False` fails at `a = U`; see [laws that fail](../specification/semantics.md#laws-that-fail).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isActive AND hasMfa` | `True`, `True` | `True` | Every operand is `True`. |
| `isActive AND hasMfa` | `True`, `Unknown` | `Unknown` | No `False`, so the `Unknown` decides. |
| `isActive AND hasMfa` | `Unknown`, `False` | `False` | One `False` is enough. |
| `isActive AND hasMfa AND notLocked` | `True`, `Unknown`, `True` | `Unknown` | Three operands, one `Unknown`. |
| `isActive AND hasMfa AND notLocked` | `True`, `Unknown`, `False` | `False` | `False` dominates the `Unknown`. |

An `Unknown` result is not satisfied: `Decision.IsSatisfied` is `True` only for `True`.

## Edge cases

- F dominates, U does not. `AND(F, x)` is `F` for every `x`, including `U` and a faulted term. `AND(U, x)` is `F` only if `x` is `F`; otherwise it is `U` or `T` per the table. `U` is therefore not a short-circuit value.
- N-ary evaluation. The result is the minimum over all operands, in any order and any grouping. The two-operand table and associativity determine every larger table.
- Faults are `Unknown`. A faulting predicate contributes `Unknown` and records a fault (see [evaluation](../specification/evaluation.md#predicates-and-faults)). The fault does not change the dominance rule. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom AND isOn` | `Unknown` | 1 |
| `boom AND isOff` | `False` | 1 |
| `isOff AND boom` | `False` | 0, because `boom` is never run |

- Empty and single-operand conventions. Mathematically the empty conjunction is `True`, the identity of the minimum, and the conjunction of one operand is that operand. The rule languages accept neither: the DSL has no one-operand chain (`(a)` is just `a`), and JSON, YAML and `RuleBuilder.And(params RuleBuilder[])` with fewer than two operands compile to `MalformedTree`. Only `RuleBuilder.And(IEnumerable<RuleBuilder>)` applies the convention, at build time: an empty sequence becomes the constant `True` and a single operand is returned unchanged. Two or more operands build the same node as the `params` overload.

## Evaluation behavior

- In the default mode the operands run from left to right and evaluation stops after the first `False`. The remaining operands are recorded as `NotEvaluated`, and their predicates do not run. `EvaluationMode.Exhaustive` runs every operand. Neither mode changes `Decision.Result`, because `False` settles `AND` (see [evaluation](../specification/evaluation.md#evaluation-modes)).
- The analyzer does not report `a AND NOT a` as a contradiction. The expression is `Unknown` when `a` is.

## Related operations

- [OR](or.md) is the dual: De Morgan's laws swap `AND` and `OR` through [NOT](not.md).
- `NAND` is `NOT(AND(a, b))`, and `IMPLIES` is built from `OR` and `NOT`; see the [derived logical operations](../derived/README.md).
- `ALL` equals `AND` as a value; see the [Cardinality Functions](../cardinality/README.md).
- `COALESCE` in [Functions](../functions/README.md) is an external operator that, unlike `AND`, can turn `Unknown` into a definite value.
