# 11: Validate Mermaid syntax

**What to build:** Every Mermaid diagram in `docs/strong-k3/` (10 in total) has been syntax-checked and each diagram's edges match the formula or prose it illustrates. A diagram that fails is fixed. Rendering stays with [k3-reference 16](../../k3-reference/issues/16-validate-and-render-mermaid-diagrams.md), which needs the Mermaid connector authorized.

**Blocked by:** 06, 07, 08

**Status:** ready-for-agent

- [ ] All 10 diagrams pass the `mermaid-diagram-generator` validator
- [ ] Each operation diagram is compared with its canonical form and matches
- [ ] The operation map, evaluation flow and cardinality interval agree with the pages that hold them
- [ ] Any diagram that the validator cannot check is listed in a comment on this ticket
