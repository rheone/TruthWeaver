# 19: Web playground

**Group:** D tooling and integration

**What to build:** A static page (Blazor WebAssembly or a JavaScript build of the engine) to try rules, switch printers, see diagrams and assign term values. New project and dependency decisions; AOT and trimming constraints matter for WebAssembly.

**Blocked by:** 16 (shared stub-assignment engine)

**Status:** needs-triage (owner 2026-10-17: hold for now)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
