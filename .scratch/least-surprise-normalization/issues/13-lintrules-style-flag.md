# 13: LintRules.Style holds NotCanonical

**What to build:** A user who tries `Lints: LintRules.All` gets findings that point at likely mistakes. `NotCanonical` moves to a new `Style` flag. `All` covers the logic and structure lints, and `All | Style` gives everything.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `LintRules.All` no longer includes `NotCanonical`; `Style` does
- [ ] The lint docs and the example in `docs/diagnostics.md` show a quiet first run and name the flag
- [ ] The test that excluded `NotCanonical` from `All` is simplified
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
