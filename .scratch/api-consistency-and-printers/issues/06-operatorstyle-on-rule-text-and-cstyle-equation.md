# 06: OperatorStyle on PrintRuleText and a CStyle equation dialect

**What to build:** `OperatorStyle` (`Word`, `Symbolic`, `CStyle`) changes only `PrintPlainText` and `PrintMermaid` labels today. Add an `OperatorStyle` choice to `PrintRuleText` for the styles the parser accepts (`Word`, and `Symbolic` where the parser reads the symbols), and reject `CStyle` there with a documented `ArgumentException` (the parser does not accept `^` and `==`). Add a `CStyle` dialect to `PrintEquation` for code-like output. The Try-first convention (ticket 04) applies to any new throwing member.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `PrintRuleText` with `Symbolic` round-trips through the compiler to an equal tree, proven by a property test
- [ ] `CStyle` on `PrintRuleText` throws as documented
- [ ] `PrintEquation` with the `CStyle` dialect prints `&&`, `||`, `!` and the other C-family forms, with the caveat that it is a rendering aid
- [ ] `docs/equations.md`, `docs/rule-text.md` and `docs/examples.md` describe both
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
