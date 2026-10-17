# 15: Importers and exporters for other rule formats

**Group:** C author and manage rules

**What to build:** JsonLogic importer first (small and well specified), then DMN decision tables, and Cedar or Rego export. Each is a separate package or tool command so the core has no new dependency.

**Blocked by:** 16 (tool commands)

**Status:** ready (grilled 2026-10-17; scope confirmed: no new package or dependency)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **First slice:** a JsonLogic importer as a `TruthWeaver.Tool` command, built on `System.Text.Json` with no new dependency or package. DMN decision tables and Cedar or Rego export are later tickets.
- **Semantics:** map `and`, `or`, `!`, `if` and the comparisons to K3 operators and catalog predicates. Report a diagnostic where two-valued JsonLogic behavior differs (truthy coercion, missing `var`). An unsupported operator fails the import with a clear message.
