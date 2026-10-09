# 05: Chain compaction as a subgraph

**What to build:** A flat AND or OR with many leaf terms draws inside a group box.

**Blocked by:** 01 (MermaidOptions with direction and node shapes)

**Status:** ready-for-agent

- [ ] `MermaidOptions` has a compaction threshold, off by default
- [ ] A flat AND or OR of only leaf terms above the threshold is wrapped in a Mermaid `subgraph`; no operand is hidden
- [ ] `Decision` coloring still applies inside the group
- [ ] Chains at or below the threshold and mixed chains are unchanged
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
