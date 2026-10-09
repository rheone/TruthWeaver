# 01: MermaidOptions with direction and node shapes

**What to build:** A caller chooses the diagram direction and gets a distinct shape per node role.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `MermaidOptions` carries `Direction` (TD, LR, BT, RL), a node-shape setting, the existing `OperatorStyle` and `ShowArgumentValues`
- [x] `MermaidTreePrinter.Print` and `CompiledRule.PrintMermaid` accept `MermaidOptions`
- [x] With default options the output equals today's output
- [x] Operators, terms and constants each get a distinct shape, and the shapes can be switched off
- [x] The output passes the Mermaid validator and examples carry `doctest` markers
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
