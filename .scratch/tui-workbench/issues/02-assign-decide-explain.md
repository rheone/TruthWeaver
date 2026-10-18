# 02: Assign term values, decision and trace

Scope decisions (owner, 2026-10-11) are in the spec; the worker adds no project, package or dependency beyond them without asking.

**What to build:** A term list built from the compiled rule; one key cycles a term through `True`, `False`, `Unknown`; a variables panel for `from(...)` values loaded from a JSON or YAML file and editable in place; the decision recomputed on every change (value, `IsSatisfied`, faults); a trace tree with each node's value colored and a plain reason line; a toggle for the `Project` and `Collapse` policies. Uses the stub-assignment engine from ticket 20.

**Blocked by:** 01

**Status:** done

- [x] Flipping any term updates the decision and trace at once; results equal `CompiledRule.EvaluateAsync` with the same stubs (tested)
- [x] Unknown handling is visible: an `Unknown` leaf shows how `AND`, `OR` and `NOT` respond
- [x] Variables load, edit and a missing or ambiguous variable shows as `Unknown` with its fault
- [x] The page for this feature is written or updated in the same change (`docs/tui.md`, linked from the README and `docs/cli.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
