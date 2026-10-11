# 16: The truthweaver dotnet tool

**Group:** D tooling and integration

**Scope decisions (owner, 2026-10-11):**
- Command-line parsing uses `System.CommandLine`, in the tool project only. The worker first checks its current stable and AOT status and reports before adding it.
- Stubs only in this ticket: `evaluate` and `test` use a value per term (`True`/`False`/`Unknown`) plus JSON or YAML data for `from(...)` variables. No `--registry <assembly>` here; that is a later on-hold ticket (16b).
- No host registry, so the tool infers predicates permissively from the rule (every predicate call accepted, argument kinds taken from the literals). An optional YAML schema file (predicate name, arguments, kinds) turns on strict schema checking and doubles as documentation.

**What to build:** A new project `TruthWeaver.Tool` (a `dotnet tool`, command `truthweaver`) with test project `TruthWeaver.Tool.Tests`, and no UI dependency. Commands: `validate <file>` (compile, print diagnostics with spans, non-zero exit on error), `format <file>` (canonical and pretty text, `--check`), `print <file> --as text|json|yaml|mermaid|dot|html|equation[:dialect]`, `diff <a> <b>` (using rule comparison from ticket 13 when it lands, otherwise equivalence), `evaluate <file> --context <json|yaml>` using stub predicates (a value per term). Input from a file or standard input, text, JSON or YAML. Stable exit codes documented.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Scope above is confirmed by the owner (done 2026-10-11); the worker adds nothing beyond it without asking
- [ ] The optional schema file format is documented with a JSON schema
- [ ] The commands above work against the sample rules and are covered by tests that run the tool in process
- [ ] Exit codes and output formats are documented in a new `docs/cli.md` linked from the README
- [ ] The package is configured for packing as a tool (not published); `docs/releasing.md` and the CI pack check include it
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
