# Architecture deepening, round 2

**Status:** grilled 2026-10-10, ready for implementation

Source: the 2026-10-10 architecture review (`/improve-codebase-architecture`) over the files with the most churn since 2026-09-20. It follows [architecture-deepening](../architecture-deepening/spec.md), which is done.

## Problem

Six places repeat knowledge that belongs in one module. A bug or a new operator touches several files in lockstep, and the repeats can drift apart.

## Decisions

| Candidate | Decision |
| --- | --- |
| Threshold family | One module in two parts. A core in `Ast` works on `(comparison, k, operand count)` and has no `Expression` in its interface: normalise, negate, bound-check, Between split. `Analysis` and `Rewriting` both call it. An expansion module in `Rewriting` owns the shared C(n,k) subset enumeration and a budgeted expansion with a pluggable builder. It takes a node budget and returns `null` over it, like `ToNand` and `ToNor`. |
| Derived operator forms | Co-locate `Build` and `TryMatch` per derived operator in one internal type, with a round-trip property test. No pattern language. |
| Tree walks | Delete `Analyzer.CollectTerms`. Then derive `MapChildren`'s child list from `ExpressionShape`. The closed "rebuild-with-operands" decision is not reopened. |
| Operator table arity | `RuleNodeCompiler` validates operand counts from `OperatorDefinition.MinOperands` and `MaxOperands`. Parsers and `RuleBuilder` are not changed here. |
| K3 scalar truth functions | Move all scalar functions (`Not`, `And`, `Or`, `Xor`, `Equivalent`, `Nand`, `Nor`, `Implies`, `Parity`, `Inspect`) to one internal type. `K3Oracle` stays independent on purpose. This closes the parked candidate: its reopen condition (a second real caller) is met by `Simplifier` and `NormalForms`. |
| Tree-format writers | A writer seam mirroring `ITreeNodeCursor`. One module owns the structure and emits through a small writer interface. JSON and YAML are two adapters. |

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-threshold-semantics-core.md) | Threshold core; Canonicalizer and Analyzer migrated | None |
| [02](issues/02-threshold-expansion-module.md) | Expansion module; PrimitiveExpander migrated | 01 |
| [03](issues/03-nandnor-on-threshold-expansion.md) | NandNorExpander migrated | 02 |
| [04](issues/04-normal-forms-simplifier-compressor-thresholds.md) | NormalForms, Simplifier, Compressor threshold rules | 02 |
| [05](issues/05-colocate-if-xor-equivalent-forms.md) | `If`, `Xor`, `Equivalent` forms | 02, 04 |
| [06](issues/06-colocate-parity-and-inspection-forms.md) | `Parity` and inspection-pair forms | 05 |
| [07](issues/07-delete-collectterms.md) | Delete `Analyzer.CollectTerms` | 01 |
| [08](issues/08-mapchildren-from-expression-shape.md) | `MapChildren` from `ExpressionShape` | None |
| [09](issues/09-compiler-arity-from-operator-table.md) | Compiler arity from the operator table | None |
| [10](issues/10-k3-scalar-truth-functions-module.md) | K3 scalar truth functions module | 04 |
| [11](issues/11-tree-writer-seam-json-adapter.md) | Tree-writer seam, JSON adapter | None |
| [12](issues/12-yaml-adapter-on-tree-writer-seam.md) | YAML adapter | 11 |

## Out of scope

- Parsers and `RuleBuilder` looking operators up from the table.
- Public API changes. Every ticket keeps behavior unchanged; the existing tests are the safety net.
- Replacing the `List<List<Expression>>` clause lists in `NormalForms` with a `Clause` type.
