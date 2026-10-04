# 11: Validate Mermaid syntax

**What to build:** Every Mermaid diagram in `docs/strong-k3/` (10 in total) has been syntax-checked and each diagram's edges match the formula or prose it illustrates. A diagram that fails is fixed. Rendering stays with [k3-reference 16](../../k3-reference/issues/16-validate-and-render-mermaid-diagrams.md), which needs the Mermaid connector authorized.

**Blocked by:** 06, 07, 08

**Status:** done

- [x] All 10 diagrams pass the `mermaid-diagram-generator` validator
- [x] Each operation diagram is compared with its canonical form and matches
- [x] The operation map, evaluation flow and cardinality interval agree with the pages that hold them
- [x] Any diagram that the validator cannot check is listed in a comment on this ticket

## Comments

- 2026-10-04: Done. All 10 diagrams pass `mermaid.parse` on Mermaid 11.16.1, run through a throwaway jsdom script because the skill's validator covers only its own examples. The five operation diagrams (`XOR`, `EQUIVALENT`, `If`, `Project`, `Collapse`) were compared by hand with their canonical forms or policy tables and match. The operation map matches the canonical forms in `operations.md`. Rendering stays with k3-reference 16.
