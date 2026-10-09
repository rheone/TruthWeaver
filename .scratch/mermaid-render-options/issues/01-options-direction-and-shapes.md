# 01: MermaidOptions with direction and node shapes

**What to build:** A caller chooses the diagram direction and gets a distinct shape per node role.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `MermaidOptions` carries `Direction` (TD, LR, BT, RL), a node-shape setting, the existing `OperatorStyle` and `ShowArgumentValues`
- [ ] `MermaidTreePrinter.Print` and `CompiledRule.PrintMermaid` accept `MermaidOptions`
- [ ] With default options the output equals today's output
- [ ] Operators, terms and constants each get a distinct shape, and the shapes can be switched off
- [ ] The output passes the Mermaid validator and examples carry `doctest` markers
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
