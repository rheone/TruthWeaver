# t1: Terminal UI workbench

**Group:** D tooling and integration

**What to build:** A terminal UI in the tool project: edit rule text live with diagnostics, assign `True`/`False`/`Unknown` to each term and see the decision and trace change, view a live tree, equation or Mermaid pane, and load a JSON or YAML file for `from(...)` variables. Uses the stub-assignment engine from ticket 20. Needs a terminal UI library (Spectre.Console or Terminal.Gui): the owner confirms the dependency first.

**Blocked by:** 16, 20

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
