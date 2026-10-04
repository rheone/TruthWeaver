# Strong Kleene (K3) reference

A navigable reference for the Strong Kleene (K3) operations of TruthWeaver: one document per operation, an index in every directory, and a specification section for the shared rules. It is a human-readable semantic reference and an implementation reference for a parser, AST, evaluator, simplifier or truth-table generator.

The value set is `{True, False, Unknown}`, written T, F and U in tables.

> [!NOTE]
> Every Strong Kleene operation has a document, except the predicates, which are on hold until they are implemented. The model, categories and document template are fixed in [PROPOSAL.md](PROPOSAL.md), approved by the owner on 2026-10-03. The latest check of the whole reference is in [VALIDATION.md](VALIDATION.md).

## Navigation

| Section | What it holds | Index |
| --- | --- | --- |
| Specification | Values, semantics, terminology and notation | [specification/](specification/README.md) |
| Gates / Operators | The primitive connectives `NOT`, `AND`, `OR` | [gates/](gates/README.md) |
| Derived Logical Operations | Logical connectives defined by composing gates | [derived/](derived/README.md) |
| Cardinality Functions | Operations over the count of true operands | [cardinality/](cardinality/README.md) |
| Functions | `COALESCE`, `If` and the four inspections | [functions/](functions/README.md) |
| Result Transformations | `Project` and `Collapse`, methods on an evaluated decision | [result-transformations/](result-transformations/README.md) |
| Predicates | On hold until the predicates are implemented | [predicates/](predicates/README.md) |

## How the reference is organised

- Every operation has exactly one primary category and an explicit Kind, `Primitive` or `Derived`; see [PROPOSAL.md](PROPOSAL.md) section 4 for the full inventory.
- Directory indexes list their documents with a one-line summary and link to them. The definition lives in the operation's own document, never in an index.
- Tables, formulas and canonical forms are checked on every `dotnet test` against an independent Strong Kleene oracle; see [docs/doc-examples.md](../doc-examples.md).
