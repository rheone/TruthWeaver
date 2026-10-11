# C1: Core: load, detect, convert, config, deterministic output

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** New projects `TruthWeaver.Tool.Core` (UI-free, not published as its own package) and `TruthWeaver.Tool.Core.Tests`. Behavior: load a rule or document from a file, standard input or a string; detect the format (text, JSON, YAML) with an explicit override; handle encoding and BOM; extract an embedded rule by path; convert between the three forms; deterministic output writer (stable order, LF, final newline) with a `--check`-style compare against an existing file; read the `truthweaver.json` configuration (defaults for operator style, constant style, dialect, limits, schema file); the stub-assignment and data-source file loaders from the ticket-16 scope. Every operation returns a result type with diagnostics; no exceptions for expected failures, and a `Try…` sibling for any throwing member. No dependency beyond the existing TruthWeaver packages.

**Blocked by:** L3

**Status:** ready-for-agent

- [ ] Each load, detect and convert operation is covered in tests with fixtures, including a BOM file and an embedded rule
- [ ] The deterministic writer and the compare are tested (differing and identical files)
- [ ] The config file has a JSON schema and a documented precedence (command line over config over defaults)
- [ ] Packaging notes: not packed as its own package; the CI build includes it
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
