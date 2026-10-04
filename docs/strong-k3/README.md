# Strong Kleene (K3) reference

The reference for the Strong Kleene (K3) logic of TruthWeaver. It defines the three values, the semantics of every Operation and the rules for writing them. Each Operation has one document, and each directory has an index.

The value set is `{True, False, Unknown}`. Tables write the values as T, F and U.

## Reading order

| Section | What it holds | Index |
| --- | --- | --- |
| Specification | Values, semantics, the list of Operations, syntax, diagnostics, terminology and notation | [specification/](specification/README.md) |
| Gates / Operators | The primitive connectives `NOT`, `AND` and `OR` | [gates/](gates/README.md) |
| Derived Logical Operations | The connectives defined by composing gates | [derived/](derived/README.md) |
| Cardinality Functions | The operations over the count of true operands | [cardinality/](cardinality/README.md) |
| Functions | `COALESCE`, `If` and the four inspections | [functions/](functions/README.md) |
| Result Transformations | `Project` and `Collapse`, the methods on an evaluated decision | [result-transformations/](result-transformations/README.md) |
| Predicates | The placeholder for predicate documentation | [predicates/](predicates/README.md) |

Start with [values](specification/values.md) and [semantics](specification/semantics.md). [operations](specification/operations.md) lists all 27 Operations.

## How an Operation document is organized

Every Operation has one primary category and one Kind, `Primitive` or `Derived`. A directory index lists its documents with a one-line summary. The definition of an Operation is in its own document, never in an index.

An Operation document has these sections, in this order:

- Name, Classification, Kind, Arity, Input domain, Output domain, Definition, Syntax, Aliases and Formal semantics.
- Where they apply: Formula, Truth table or Evaluation table, Canonical form, Equivalent forms, Examples, Edge cases, a Mermaid diagram, Evaluation behavior and Related operations.

A truth table lists every assignment for an Operation with a fixed number of operands. An evaluation table lists the definitely true and possibly true counts for a parameterised or variadic Operation.
