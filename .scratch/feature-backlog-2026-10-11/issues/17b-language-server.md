# 17b: Language server for rule text

**Group:** D tooling and integration

**What to build:** An LSP server giving diagnostics with spans (already produced for rule text), completion for predicate names and arguments from a registry, hover with predicate descriptions, and format-on-save. A new project and a dependency on an LSP library; decide after the grammar (17a) proves useful.

**Blocked by:** 17a

**Status:** needs-triage (owner 2026-10-17: decide after the 17a grammar proves useful)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
