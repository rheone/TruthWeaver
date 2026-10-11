# 16: The truthweaver command line over Core

**Group:** D tooling and integration

Scope decisions (owner, 2026-10-11): see [tool-platform](../../tool-platform/spec.md). This ticket supersedes the earlier 16 scope. The CLI is a real tool with roughly the same functionality as the TUI; it is a thin front end over `TruthWeaver.Tool.Core` (tickets C1 to C4). `System.CommandLine` goes in the CLI project only; the worker first checks its current stable and AOT status and reports before adding it. Stubs only (no host registry; ticket 16b is on hold); permissive predicate inference with an optional YAML schema file.

**What to build:** New projects `TruthWeaver.Tool` (a `dotnet tool`, command `truthweaver`) and `TruthWeaver.Tool.Tests`. Commands over Core: `validate` (all levels, statistics, `--strict`, `--json`, `--sarif`), `convert` (text, JSON, YAML in any direction, notes kept), `format` (canonical and pretty, `--check`), `print` and `export` (any artifact, `--all <folder>`, `--render svg|png`, `--check`), `report`, `diff` (equivalence now, looser or tighter when ticket 13 exists), `evaluate` (stub values and data files, decision and trace), and `test` (ticket 20). Input from files or standard input; format detection with override; embedded rules by `--path`; `truthweaver.json` configuration; `--json` output for every command; documented, stable exit codes; shell completion.

**Blocked by:** C1, C2, C3 (tool-platform)

**Status:** ready-for-agent

- [ ] Scope above is confirmed by the owner (done 2026-10-11); the worker adds nothing beyond it without asking
- [ ] Every command has in-process tests against the sample rules, including `--json` output and exit codes
- [ ] `docs/cli.md` documents every command, option, exit code and the config file; linked from the README
- [ ] The package is configured for packing as a tool (not published); `docs/releasing.md` and the CI pack check include it
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
