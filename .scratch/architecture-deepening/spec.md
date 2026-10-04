# Deepen the tree-format reader and the operator definitions

**Status:** ready-for-agent

Source: the 2026-10-03 `/improve-codebase-architecture` review and its grilling round (owner answers Q1 a, Q2 a, Q3 a, Q4 a, Q5 a). Uses the `/codebase-design` vocabulary (module, interface, depth, seam, adapter, leverage, locality).

## Problem Statement

**Tree-format reading is duplicated.** `JsonTreeParser` (about 600 lines) and `YamlTreeParser` (about 650 lines) carry the same `const` / `predicate` / `op` dispatch, the same op-name to `RuleNode` switch, the same k / min / max / arity validation and the same `Collapse` and `Parity` rejections. The only real variation is how a node's properties and source span are read. A behaviour change lands twice or drifts. Yaml records a real span per node; Json always uses `SourceSpan.None`.

**Operator facts are scattered.** An operator's canonical name, tree-format name, label and description are re-derived by string-keyed switches in `OperatorInfo`, `TreeFormatOpNames`, `Evaluator.Describe` and `RuleBuilder`. A missed operator surfaces as a runtime exception rather than a failing test. CLAUDE.md lists many places to touch for a new operator.

Both build on earlier, finished seams: [expression-node-shape-seam](../expression-node-shape-seam/spec.md) (`ExpressionShape.Of`) and [rule-tree-format-op-name-table](../rule-tree-format-op-name-table/spec.md) (`TreeFormatOpNames`). This effort deepens them; it does not contradict ADR-0004.

## Solution

1. **One tree-format reader (module) behind a node-cursor seam.** The cursor is a small `internal` interface: kind, string and integer property access, children, optional span. The reader owns dispatch, op-name mapping, parameter and arity validation and rejections. JSON and YAML become two adapters, which makes the seam real (two adapters). Decided: the cursor and reader stay `internal` to `TruthWeaver`, and `TruthWeaver.Yaml` reaches them through the existing `InternalsVisibleTo`. No new public surface (ADR-0004: closed operator set, small public API).
2. **One operator definition table.** One definition per operator holding descriptive facts only: canonical name, tree-format name, arity, label, description. Decided: `Evaluator`, `Analyzer` and the rewriters keep switching on node type, since per-operator behaviour differs between them and moving it would make the interface as wide as the implementations. `OperatorInfo.Describe` stays public with an unchanged signature and reads the table.

## Out of scope (decided)

- **Rebuild-with-operands on `Expression` nodes: closed.** `ExpressionTools.MapChildren` already gives shape-preserving child mapping and four rewriters use it.
- **Extracting the Strong K3 truth functions from `Evaluator`: parked** until a second real caller needs them (one adapter is a hypothetical seam; the `Simplifier` overlap is unconfirmed). Recorded by ticket 05, currently blocked on the owner.
- A public node cursor or third-party tree formats.
- Any change to rule semantics, diagnostic codes or messages, or the JSON Schema.

## User Stories

1. As a maintainer changing how an operator node is validated, I want to change one reader, so JSON and YAML cannot drift.
2. As a maintainer adding an operator, I want to add one definition and one reader case, so I do not hunt through parallel switches.
3. As a library consumer, I want diagnostics, paths and spans to behave exactly as before, so nothing observable changes.
4. As a test author, I want one conformance suite run against both adapters, so format parity is checked by construction.

## Implementation Decisions

- Behaviour-preserving refactor. Existing tests pass without edits to their expectations; this is the main regression guard.
- JSON adapter reports `SourceSpan.None`; YAML adapter keeps its spans.
- The definition table is added first and the old switches are removed afterwards (expand, then contract), so each ticket lands green.
- Docs: update the CLAUDE.md "adding an operator" note and the ADR-0004 node-shape amendment when the table lands (ticket 04).

## Tickets

| # | Ticket | Blocked by |
|---|---|---|
| 01 | [Shared tree-format reader, JSON as first adapter](issues/01-shared-tree-format-reader-json-adapter.md) | None |
| 02 | [YAML adapter on the shared reader](issues/02-yaml-adapter-on-shared-reader.md) | 01 |
| 03 | [Operator definition table (expand)](issues/03-operator-definition-table-expand.md) | 02 |
| 04 | [Migrate consumers to the table (contract)](issues/04-migrate-consumers-to-operator-table-contract.md) | 03 |
| 05 | [Record closed and parked candidates](issues/05-record-closed-and-parked-candidates.md) | Owner go-ahead (blocked) |

## Testing Decisions

- Test through the interfaces: the reader through an in-memory cursor, both adapters through one conformance suite, the table through a completeness test (every operator has every field, names unique).
- No tests of private helpers.

## Further Notes

Facts verified during the review: `ExpressionTools.MapChildren` is used by `Canonicalizer`, `Compressor`, `PrimitiveExpander` and `Simplifier`; `YamlTreeParser` has per-node spans while `JsonTreeParser` does not.
