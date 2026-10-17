# FORALL

`True` when the body is `True` for every element of a collection, `False` when it is `False` for at least one element, and `Unknown` otherwise. The generalized `AND` over a collection. Back to the [Quantifiers index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Canonical name: `FORALL`
- DSL: `FORALL(variable IN collection, body)`
- JSON and YAML `op`: `forall`
- `RuleBuilder` member: `RuleBuilder.ForAll`

## Classification

- Category: Quantifiers
- Category index: [Quantifiers](README.md)
- A Strong Kleene connective: it is monotone in the [information order](../specification/values.md#information-order). It is not an external operator.

## Kind

Quantifier. `FORALL` is neither a primitive nor a derived Operation: it has no definition in a fixed number of primitives, because the number of operands is the size of the collection at evaluation time. It is not in the [inventory of Operations](../specification/operations.md).

## Arity

One body. The head names one bound variable and one collection. The body is a full expression. The number of values that the quantifier folds is the number of elements of the collection.

## Input domain

The collection is a registered collection binding. The body gives a value in `{T, F, U}` for each element.

## Output domain

`{T, F, U}`.

## Definition

For a collection of elements `e1, ..., en`, with `vi` the value of the body for `ei`, `FORALL` is `AND(v1, ..., vn)`. It is `True` when every `vi` is `True`, `False` when at least one `vi` is `False`, and `Unknown` otherwise. For an empty collection it is `True`. For a `null` collection, or a collection whose selector throws, it is `Unknown`.

## Syntax

| Form | Spelling |
| --- | --- |
| DSL, canonical | `FORALL(line IN lines, inStock)` |
| JSON | `{"op": "forall", "var": "line", "in": "lines", "operands": [ <body> ]}` |
| YAML | `op: forall`, `var: line`, `in: lines` and an `operands:` list of exactly one item |
| `RuleBuilder` | `RuleBuilder.ForAll(string variable, string collection, RuleBuilder body)` |

`FORALL` is a call form with no precedence, like `PARITY` and `If`. `FORALL` is a reserved word, so a predicate cannot be named `FORALL`. `IN` is reserved only at the head of a quantifier, so a predicate named `In` is not affected. The tree printers write `FORALL line IN lines`, or `∀ line ∈ lines` in the symbolic style. The symbols `∀` and `∃` are not accepted as input.

## Aliases

None. The word is case-insensitive in the DSL (`forall(...)`), and the JSON and YAML `op` is case-insensitive on read.

## Formal semantics

For known, finite values `v1, ..., vn`, `FORALL` is the minimum of the values in the truth order, which is `AND`. The minimum of no values is `True`, so an empty collection gives `True`. A `null` collection is not a collection of no elements: the collection itself is not known, so the result is `Unknown`.

## Formula

$$\operatorname{FORALL}(v_1, \dots, v_n) = \bigwedge_{i=1}^{n} v_i = \min(v_1, \dots, v_n), \qquad \operatorname{FORALL}() = \mathsf{T}$$

## Equivalent forms

`FORALL` is the dual of [EXISTS](exists.md). A negation moves through the quantifier, for every collection, including an empty and a `null` one:

<!-- doctest:skip equivalence notation, not a rule to compile -->
```text
NOT FORALL(x IN c, b)  is  EXISTS(x IN c, NOT b)
```

For a collection of fixed size, `FORALL(x IN c, b)` is the same value as `AND(b1, ..., bn)`, where `bi` is the body for element `i`.

## Examples

Here `c` is the collection, and the values are the body values for its elements, in order.

| Collection | Body values | `FORALL` | Why |
| --- | --- | --- | --- |
| `c` | `True`, `True`, `True` | `True` | Every element is `True`. |
| `c` | `True`, `False`, `True` | `False` | One element is `False`. |
| `c` | `True`, `Unknown`, `True` | `Unknown` | No element is `False`, and one is `Unknown`. |
| `c` | `False`, `Unknown` | `False` | A `False` element settles the result. |
| Empty | none | `True` | The identity element of `AND`. |
| `null` | none | `Unknown` | The collection is not known. |

## Edge cases

- `FORALL(x IN c, True)` is not the constant `True`: it is `Unknown` for a `null` collection. No rewrite folds it.
- A selector that throws gives `Unknown` and one fault for the collection. A body that faults for one element gives `Unknown` for that element and one fault with the element in `Fault.Scope`.
- A collection name that is not registered is `UnknownCollection` (`TRE0042`). A nested quantifier that reuses an enclosing variable name is `ShadowedVariable` (`TRE0044`). Nesting deeper than `MaxQuantifierDepth` is `MaxQuantifierDepthExceeded` (`TRE0043`).
- A JSON or YAML node with a number of `operands` other than one is `MalformedTree` (`TRE0014`).

## Evaluation behavior

- Elements run in selector order. In `ShortCircuit` mode `FORALL` stops after the first `False` element, and the remaining elements are `NotEvaluated`. In `Exhaustive` mode every element runs and every fault is collected.
- The trace labels the node `FORALL(line IN lines)`. It has one child for each element, labelled `line[0]`, `line[1]` and so on, and each child holds the trace of the body.
- The rewrites do not expand `FORALL`. They rewrite its body in its own scope. `ToNnf` turns `NOT FORALL` into `EXISTS` of the negated body.
- JSON and YAML print `{"op": "forall", "var": ..., "in": ..., "operands": [...]}`.

## Related operations

- [EXISTS](exists.md) is its dual.
- [AND](../gates/and.md) is the operation that `FORALL` folds over the elements.
- [ALL](../cardinality/all.md) is the same function over operands that are written in the rule, not over a collection.
- The [Quantifiers index](README.md) has the rules that both quantifiers share.
