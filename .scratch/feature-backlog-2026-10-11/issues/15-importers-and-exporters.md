# 15: Importers and exporters for other rule formats

**Group:** C author and manage rules

**What to build:** JsonLogic importer first (small and well specified), then DMN decision tables, and Cedar or Rego export. Each is a separate package or tool command so the core has no new dependency.

**Blocked by:** 16 (tool commands)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
