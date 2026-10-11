# 16: The truthweaver dotnet tool

**Group:** D tooling and integration

**What to build:** A new project `TruthWeaver.Tool` (a `dotnet tool`, command `truthweaver`) with test project `TruthWeaver.Tool.Tests`, and no UI dependency. Commands: `validate <file>` (compile, print diagnostics with spans, non-zero exit on error), `format <file>` (canonical and pretty text, `--check`), `print <file> --as text|json|yaml|mermaid|dot|html|equation[:dialect]`, `diff <a> <b>` (using rule comparison from ticket 13 when it lands, otherwise equivalence), `evaluate <file> --context <json|yaml>` using stub predicates (a value per term), and `--registry <assembly>` to load a host registry if feasible. Input from a file or standard input, text, JSON or YAML. Stable exit codes documented. Command-line parsing with `System.CommandLine` only if the owner confirms it as a dependency in the scope step.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Before any file is created, the worker confirms the scope with the owner (standing rule: new packages, new projects and heavy dependencies need explicit confirmation); the confirmed scope is written at the top of this ticket
- [ ] The commands above work against the sample rules and are covered by tests that run the tool in process
- [ ] Exit codes and output formats are documented in a new `docs/cli.md` linked from the README
- [ ] The package is configured for packing as a tool (not published); `docs/releasing.md` and the CI pack check include it
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
