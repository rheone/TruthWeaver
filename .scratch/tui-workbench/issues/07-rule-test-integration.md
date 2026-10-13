# 07: Save cases and run rule tests

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** Save the current assignment as a named case into a YAML rule-test file (ticket 20's format), edit cases, and run the file's cases against the current rule with a pass or fail list; failing cases can be loaded back into the assignment pane to debug.

**Blocked by:** 02, 20

**Status:** on-hold until tickets 16 and 20 (feature-backlog-2026-10-11) are done; then ready-for-agent

- [ ] Saving a case writes valid ticket-20 format; the file passes `truthweaver test`
- [ ] Running shows per-case pass or fail with expected and actual values; selecting a failure loads its assignment
- [ ] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
