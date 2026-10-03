# OR

Disjunction: `True` as soon as any operand is `True`, and `False` only when every operand is `False`. Back to the [Gates / Operators index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `OR`
- DSL: `OR` (also `||` and `∨`, see [Aliases](#aliases))
- JSON and YAML `op`: `or`
- `RuleBuilder` member: `RuleBuilder.Or`

## Classification

- Category: Gates / Operators
- Category index: [Gates / Operators](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Primitive. `OR` has no definition in other Operations. It is one of the three connectives from which the others are defined ([semantics](../specification/semantics.md#the-three-primitive-connectives)).

## Arity

Two or more operands. The engine builds one flat node holding all operands, so `a OR b OR c` is a single three-operand `OR`, not two nested ones. Fewer than two operands is a compile error, `MalformedTree` (`BRE0014`): "This operator requires at least 2 operands but found N." See [Edge Cases](#edge-cases) for the empty and single-operand conventions.

## Input Domain

Each operand is a value in `{T, F, U}`.

## Output Domain

`{T, F, U}`.

## Definition

`OR` is `True` when at least one operand is `True`, `False` when every operand is `False`, and `Unknown` otherwise.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `a OR b OR c` |
| DSL, symbols | `a \|\| b`, `a ∨ b` |
| JSON | `{"op": "or", "operands": [ <expression>, <expression>, ... ]}` |
| YAML | `op: or` with an `operands:` list of two or more items |
| `RuleBuilder` | `RuleBuilder.Or(params RuleBuilder[])` for two or more operands; `RuleBuilder.Or(IEnumerable<RuleBuilder>)` for a list whose length is known only at run time |

`OR` is an infix operator with no call form: `OR(a, b)` is a syntax error in the DSL. It binds looser than `AND` and `NOT`, so `a OR b AND c` is `a OR (b AND c)`. Mixing `OR` with `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `??` or the ternary at one level without parentheses is the compile error `AmbiguousOperatorMixing`. The canonical printer writes the word form and a flat chain.

In this reference the function-call spelling `OR(a, b)` is only a plain-text convention for tables and canonical forms ([notation](../specification/notation.md#code-conventions)). It is not DSL input.

## Aliases

| Alias | Kind |
| --- | --- |
| `\|\|` | Symbol |
| `∨` | Symbol |

Symbols compile to the same node as the word, so notation never changes meaning. The word is case-insensitive (`or`, `Or`, `OR`). A lone `|` is a syntax error.

## Formal Semantics

Under the truth order $\mathsf{F} < \mathsf{U} < \mathsf{T}$, `OR` is the maximum of its operands. The maximum is the most true operand, so a single `True` sets the result and a single `Unknown` lifts it at least to `Unknown`.

## Formula

$$x_1 \lor x_2 \lor \dots \lor x_n = \max(x_1, x_2, \dots, x_n) = \begin{cases} \mathsf{T} & \text{if some } x_i = \mathsf{T} \\ \mathsf{F} & \text{if every } x_i = \mathsf{F} \\ \mathsf{U} & \text{otherwise} \end{cases}$$

For two operands this is $a \lor b = \max(a, b)$.

## Truth Table

`True` dominates: every row with a `T` in any column has result `T`, whatever the other columns hold, `U` included. A result of `F` needs every column `F`. Every other row is `U`.

### Two operands

<!-- k3:truth OR -->
| a | b | OR(a, b) |
| --- | --- | --- |
| T | T | T |
| T | U | T |
| T | F | T |
| U | T | T |
| U | U | U |
| U | F | U |
| F | T | T |
| F | U | U |
| F | F | F |

### Three operands

<!-- k3:truth OR -->
| a | b | c | OR(a, b, c) |
| --- | --- | --- | --- |
| T | T | T | T |
| T | T | U | T |
| T | T | F | T |
| T | U | T | T |
| T | U | U | T |
| T | U | F | T |
| T | F | T | T |
| T | F | U | T |
| T | F | F | T |
| U | T | T | T |
| U | T | U | T |
| U | T | F | T |
| U | U | T | T |
| U | U | U | U |
| U | U | F | U |
| U | F | T | T |
| U | F | U | U |
| U | F | F | U |
| F | T | T | T |
| F | T | U | T |
| F | T | F | T |
| F | U | T | T |
| F | U | U | U |
| F | U | F | U |
| F | F | T | T |
| F | F | U | U |
| F | F | F | F |

### Four operands

Eighty-one rows, one for each assignment of `T`, `U` and `F` to four operands.

<details>
<summary>Show the 81-row table</summary>

<!-- k3:truth OR -->
| a | b | c | d | OR(a, b, c, d) |
| --- | --- | --- | --- | --- |
| T | T | T | T | T |
| T | T | T | U | T |
| T | T | T | F | T |
| T | T | U | T | T |
| T | T | U | U | T |
| T | T | U | F | T |
| T | T | F | T | T |
| T | T | F | U | T |
| T | T | F | F | T |
| T | U | T | T | T |
| T | U | T | U | T |
| T | U | T | F | T |
| T | U | U | T | T |
| T | U | U | U | T |
| T | U | U | F | T |
| T | U | F | T | T |
| T | U | F | U | T |
| T | U | F | F | T |
| T | F | T | T | T |
| T | F | T | U | T |
| T | F | T | F | T |
| T | F | U | T | T |
| T | F | U | U | T |
| T | F | U | F | T |
| T | F | F | T | T |
| T | F | F | U | T |
| T | F | F | F | T |
| U | T | T | T | T |
| U | T | T | U | T |
| U | T | T | F | T |
| U | T | U | T | T |
| U | T | U | U | T |
| U | T | U | F | T |
| U | T | F | T | T |
| U | T | F | U | T |
| U | T | F | F | T |
| U | U | T | T | T |
| U | U | T | U | T |
| U | U | T | F | T |
| U | U | U | T | T |
| U | U | U | U | U |
| U | U | U | F | U |
| U | U | F | T | T |
| U | U | F | U | U |
| U | U | F | F | U |
| U | F | T | T | T |
| U | F | T | U | T |
| U | F | T | F | T |
| U | F | U | T | T |
| U | F | U | U | U |
| U | F | U | F | U |
| U | F | F | T | T |
| U | F | F | U | U |
| U | F | F | F | U |
| F | T | T | T | T |
| F | T | T | U | T |
| F | T | T | F | T |
| F | T | U | T | T |
| F | T | U | U | T |
| F | T | U | F | T |
| F | T | F | T | T |
| F | T | F | U | T |
| F | T | F | F | T |
| F | U | T | T | T |
| F | U | T | U | T |
| F | U | T | F | T |
| F | U | U | T | T |
| F | U | U | U | U |
| F | U | U | F | U |
| F | U | F | T | T |
| F | U | F | U | U |
| F | U | F | F | U |
| F | F | T | T | T |
| F | F | T | U | T |
| F | F | T | F | T |
| F | F | U | T | T |
| F | F | U | U | U |
| F | F | U | F | U |
| F | F | F | T | T |
| F | F | F | U | U |
| F | F | F | F | F |

</details>

## Equivalent Forms

De Morgan's law defines `OR` from `AND` and `NOT`:

<!-- k3:canonical OR vars=a,b -->
```text
NOT(AND(NOT(a), NOT(b)))
```

`OR` is associative, so an n-ary node equals its binary nesting:

<!-- k3:canonical OR vars=a,b,c -->
```text
OR(OR(a, b), c)
```

As a value, `OR` of two to four operands equals `ANY` of the same operands, and equals `AtLeast(1, ...)`. They stay distinct Operations, because `ANY` is a cardinality operation and keeps its own node when a rule round-trips:

<!-- k3:canonical OR n=2..4 -->
```text
ANY(...)
```

<!-- k3:canonical OR n=2..4 -->
```text
ATLEAST(1, ...)
```

The other laws that hold for `OR` (commutativity, idempotence, distributivity over `AND`, absorption, the identity `a OR False = a`, the annihilator `a OR True = True`) are listed in [laws that hold](../specification/semantics.md#laws-that-hold). The law `a OR NOT a = True` (excluded middle) fails at `a = U`; see [laws that fail](../specification/semantics.md#laws-that-fail).

## Examples

| Rule | Operand values | Result | Why |
| --- | --- | --- | --- |
| `isOwner OR isAdmin` | `False`, `False` | `False` | Every operand is `False`. |
| `isOwner OR isAdmin` | `False`, `Unknown` | `Unknown` | No `True`, so the `Unknown` decides. |
| `isOwner OR isAdmin` | `Unknown`, `True` | `True` | One `True` is enough. |
| `isOwner OR isAdmin OR isAuditor` | `False`, `Unknown`, `False` | `Unknown` | Three operands, one `Unknown`. |
| `isOwner OR isAdmin OR isAuditor` | `False`, `Unknown`, `True` | `True` | `True` dominates the `Unknown`. |

An `Unknown` result is not satisfied: `Decision.IsSatisfied` is `True` only for `True`.

## Edge Cases

- **T dominates, U does not.** `OR(T, x)` is `T` for every `x`, including `U` and a faulted term. `OR(U, x)` is `T` only if `x` is `T`; otherwise it is `U` or `F` per the table. `U` is therefore not a short-circuit value.
- **N-ary evaluation.** The result is the maximum over all operands, in any order and any grouping. The two-operand table and associativity determine every larger table.
- **Short-circuit does not change the value.** In the default mode the operands are evaluated left to right and evaluation stops after the first `True`. The remaining operands are recorded as `NotEvaluated` and their predicates are not invoked. Because `True` settles `OR`, the stopped result equals what a full evaluation gives. `EvaluationMode.Exhaustive` evaluates every operand; it changes the trace and which faults are recorded, never `Decision.Result`.
- **Faults are Unknown.** A predicate that throws, times out or is cancelled contributes `Unknown` and records a `Fault` ([ADR-0001](../../adr/0001-kleene-failure-model.md)). The fault does not change the dominance rule. In the table below `boom` is a term that faults, `isOn` is `True` and `isOff` is `False`:

| Rule | Result | Faults recorded |
| --- | --- | --- |
| `boom OR isOff` | `Unknown` | 1 |
| `boom OR isOn` | `True` | 1 |
| `isOn OR boom` | `True` | 0, because `boom` is never run |

- **Empty and single-operand conventions.** Mathematically the empty disjunction is `False`, the identity of the maximum, and the disjunction of one operand is that operand. The rule languages accept neither: the DSL has no one-operand chain (`(a)` is just `a`), and JSON, YAML and `RuleBuilder.Or(params RuleBuilder[])` with fewer than two operands compile to `MalformedTree`. Only `RuleBuilder.Or(IEnumerable<RuleBuilder>)` applies the convention, at build time: an empty sequence becomes the constant `False` and a single operand is returned unchanged. Two or more operands build the same node as the `params` overload.

## Implementation Notes

- The evaluator reports an `OR` node as `OR` and folds its operands left to right, starting from the identity `False`.
- The no-tautology theorem covers `OR`: an expression built only from variables and Strong Kleene connectives is `Unknown` when every variable is, so `a OR NOT a` is not reported as a tautology ([ADR-0005](../../adr/0005-strong-k3-language-surface.md) decision 17).

## Related Operations

- [AND](and.md) is the dual: De Morgan's laws swap `AND` and `OR` through [NOT](not.md).
- `NOR` is `NOT(OR(a, b))`, and `IMPLIES` is `OR(NOT(a), b)`; see the [derived logical operations](../derived/README.md).
- `ANY` equals `OR` as a value; see the [Cardinality Functions](../cardinality/README.md).
- `COALESCE` in [Functions](../functions/README.md) is an external operator that, unlike `OR`, can turn `Unknown` into a definite value.
