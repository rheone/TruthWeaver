# 12: Rule store with hot reload

**Group:** C author and manage rules

**What to build:** An abstraction for loading named rules from files, `IConfiguration`, a database or a remote source, compiling once, caching by hash and swapping atomically on change (`IOptionsMonitor` style). Probably a separate package; no heavy dependency in the core.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **Package:** new `TruthWeaver.Extensions.Configuration` (owner confirmed; depends on `Microsoft.Extensions.Configuration` and Options). Confirmed as the package and dependency scope; the core gains no dependency.
- **First-slice source:** an `IConfiguration` section holds the named rules; reload follows the `IConfiguration` change token. Directory watching and database or remote sources are later work.
- **Bad reload:** keep the last good compiled rule live and report the diagnostics through the logger and a callback. A first load that fails leaves the name missing, and `Get` fails closed.
- **Still open at build time:** the project and test project names and the rule format inside the section (rule text vs JSON vs YAML).
