# 04: Per-node style callback

**What to build:** A caller highlights or mutes any node or subtree without an evaluation.

**Blocked by:** 01 (MermaidOptions with direction and node shapes), 03 (Palettes)

**Status:** ready-for-agent

- [ ] `MermaidOptions` accepts `Func<OutlineNode, NodeStyle?>` returning `Highlight`, `Mute` or a custom class name
- [ ] The callback runs after `Decision` coloring and wins on conflict
- [ ] The custom class name appears as a Mermaid class the caller can define
- [ ] Tests cover highlighting an operator node and muting a subtree
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
