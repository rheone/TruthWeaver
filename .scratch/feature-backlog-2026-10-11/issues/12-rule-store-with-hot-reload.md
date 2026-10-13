# 12: Rule store with hot reload

**Group:** C author and manage rules

**What to build:** An abstraction for loading named rules from files, `IConfiguration`, a database or a remote source, compiling once, caching by hash and swapping atomically on change (`IOptionsMonitor` style). Probably a separate package; no heavy dependency in the core.

**Blocked by:** None (can start immediately)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
