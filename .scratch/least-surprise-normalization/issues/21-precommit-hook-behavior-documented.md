# 21: Document what the pre-commit hook and format-all do

**What to build:** A contributor reads, in one place, that the hook formats and re-stages fully staged C# files, builds, and runs the whole test suite, and that `format-all.ps1` rewrites files unless `-CheckOnly` is passed.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A contributor note (the repo has no CONTRIBUTING file; add a short one or extend `docs/agents/`) lists the hook steps and the `format-all.ps1` behavior
- [ ] The note is linked from the README
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
