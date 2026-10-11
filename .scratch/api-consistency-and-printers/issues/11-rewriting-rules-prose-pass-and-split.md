# 11: Prose pass and split of rewriting-rules.md

**What to build:** `docs/rewriting-rules.md` grew to about 1450 lines and was written without the `humanizer` and `voice-of-robert` passes. Run both over the new sections, then split the page: keep `rewriting-rules.md` as the overview and reference (caps, diagnostics, thresholds, the "which rewrite" table), and move the worked examples to a linked page. Use the `github-markdown` skill for the dialect and the `mermaid-diagram-generator` skill only where a diagram adds something. Update the README and glossary links. After ticket 04, examples use the `Try` form first.

**Blocked by:** 04

**Status:** ready-for-agent

- [ ] Neither page exceeds about 700 lines
- [ ] Doc example markers still pass after the move; no anchor link is broken (`DocumentationLint`)
- [ ] `humanizer` and `voice-of-robert` were applied and the AI-tell patterns they list are gone
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
