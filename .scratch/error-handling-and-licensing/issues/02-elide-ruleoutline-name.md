# 02: Elide the `RuleOutline` name

**What to build:** `OutlineNode` is the complete outline and `CompiledRule.Outline()` returns its root, so the separate type name `RuleOutline` is dropped from the docs. "Outline" remains the concept; `OutlineNode` is the type. No code type is added.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] ADR-0007, the CHANGELOG rename table and `CONTEXT.md` say "outline" (concept) and `OutlineNode` (type) and never name a `RuleOutline` type
- [x] The naming-cleanup spec's rename table is left as historical; a note says `RuleOutline` was elided on 2026-10-04
- [x] The vocabulary guard still passes
- [x] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 2).
