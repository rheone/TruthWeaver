# EXISTS

`True` when the body is `True` for at least one element of a collection, `False` when it is `False` for every element, and `Unknown` otherwise. The generalized `OR` over a collection. Back to the [Quantifiers index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `EXISTS`
- DSL: `EXISTS(variable IN collection, body)`
- JSON and YAML `op`: `exists`
- `RuleBuilder` member: `RuleBuilder.Exists`

## Classification

- Category: Quantifiers
- Category index: [Quantifiers](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Quantifier. `EXISTS` is neither a primitive nor a derived Operation: it has no definition in a fixed number of primitives, because the number of operands is the size of the collection at evaluation time. It is not in the [inventory of Operations](../specification/operations.md).

## Arity

One body. The head names one bound variable and one collection. The body is a full expression. The number of values that the quantifier folds is the number of elements of the collection.

## Input domain

The collection is a registered collection binding. The body gives a value in `{T, F, U}` for each element.

## Output domain

`{T, F, U}`.

## Definition

For a collection of elements `e1, ..., en`, with `vi` the value of the body for `ei`, `EXISTS` is `OR(v1, ..., vn)`. It is `True` when at least one `vi` is `True`, `False` when every `vi` is `False`, and `Unknown` otherwise. For an empty collection it is `False`. For a `null` collection, or a collection whose selector throws, it is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `EXISTS(line IN lines, backordered)` |
| JSON | `{"op": "exists", "var": "line", "in": "lines", "operands": [ <body> ]}` |
| YAML | `op: exists`, `var: line`, `in: lines` and an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.Exists(string variable, string collection, RuleBuilder body)` |

`EXISTS` is a call form with no precedence, like `PARITY` and `If`. `EXISTS` is a reserved word, so a predicate cannot be named `EXISTS`. `IN` is reserved only at the head of a quantifier, so a predicate named `In` is not affected. The tree printers write `EXISTS line IN lines`, or `∃ line ∈ lines` in the symbolic style. The symbols `∀` and `∃` are not accepted as input.

## Aliases

None. The word is case-insensitive in the DSL (`exists(...)`), and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

For known, finite values `v1, ..., vn`, `EXISTS` is the maximum of the values in the truth order, which is `OR`. The maximum of no values is `False`, so an empty collection gives `False`. A `null` collection is not a collection of no elements: the collection itself is not known, so the result is `Unknown`.

## Formula

$$\operatorname{EXISTS}(v_1, \dots, v_n) = \bigvee_{i=1}^{n} v_i = \max(v_1, \dots, v_n), \qquad \operatorname{EXISTS}() = \mathsf{F}$$

## Equivalent forms

`EXISTS` is the dual of [FORALL](forall.md). A negation moves through the quantifier, for every collection, including an empty and a `null` one:

<!-- doctest:skip equivalence notation, not a rule to compile -->
```text
NOT EXISTS(x IN c, b)  is  FORALL(x IN c, NOT b)
```

For a collection of fixed size, `EXISTS(x IN c, b)` is the same value as `OR(b1, ..., bn)`, where `bi` is the body for element `i`.

## Examples

Here `c` is the collection, and the values are the body values for its elements, in order.

| Collection | Body values | `EXISTS` | Why |
| --- | --- | --- | --- |
| `c` | `False`, `False`, `False` | `False` | Every element is `False`. |
| `c` | `False`, `True`, `False` | `True` | One element is `True`. |
| `c` | `False`, `Unknown`, `False` | `Unknown` | No element is `True`, and one is `Unknown`. |
| `c` | `True`, `Unknown` | `True` | A `True` element settles the result. |
| Empty | none | `False` | The identity element of `OR`. |
| `null` | none | `Unknown` | The collection is not known. |

## Edge cases

- `EXISTS(x IN c, False)` is not the constant `False`: it is `Unknown` for a `null` collection. No rewrite folds it.
- A selector that throws gives `Unknown` and one fault for the collection. A body that faults for one element gives `Unknown` for that element and one fault with the element in `Fault.Scope`.
- A collection name that is not registered is `UnknownCollection` (`TRE0042`). A nested quantifier that reuses an enclosing variable name is `ShadowedVariable` (`TRE0044`). Nesting deeper than `MaxQuantifierDepth` is `MaxQuantifierDepthExceeded` (`TRE0043`).
- A JSON or YAML node with a number of `operands` other than one is `MalformedTree` (`TRE0014`).

## Evaluation behavior

- Elements run in selector order. In `ShortCircuit` mode `EXISTS` stops after the first `True` element, and the remaining elements are `NotEvaluated`. In `Exhaustive` mode every element runs and every fault is collected.
- The trace labels the node `EXISTS(line IN lines)`. It has one child for each element, labelled `line[0]`, `line[1]` and so on, and each child holds the trace of the body.
- The rewrites do not expand `EXISTS`. They rewrite its body in its own scope. `ToNnf` turns `NOT EXISTS` into `FORALL` of the negated body.
- JSON and YAML print `{"op": "exists", "var": ..., "in": ..., "operands": [...]}`.

## Related operations

- [FORALL](forall.md) is its dual.
- [OR](../gates/or.md) is the operation that `EXISTS` folds over the elements.
- [ANY](../cardinality/any.md) is the same function over operands that are written in the rule, not over a collection.
- The [Quantifiers index](README.md) has the rules that both quantifiers share.
