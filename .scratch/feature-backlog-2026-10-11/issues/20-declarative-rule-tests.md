# 20: Declarative YAML rule tests and runner

**Group:** D tooling and integration

**Scope decisions (owner, 2026-10-11):**
- The runner library lives in `TruthWeaver.Testing` (a helper such as `RuleTestFile.RunAsync`), so C# tests can run the same YAML cases and the tool references it. This adds a reference from `TruthWeaver.Testing` to `TruthWeaver.Yaml`, confirmed by the owner: update the package-boundary rules in `TruthWeaver.Architecture.Tests` and `docs/packages.md` in the same change, and record the new dependency in the package metadata and CHANGELOG.
- Stubs only, as in ticket 16 (a value per term, JSON or YAML data for variables).

**What to build:** A test file format (YAML, with a JSON schema) for rule authors who do not write C#: a rule (inline or by path), named cases with `given` (stub values per term, as `True`/`False`/`Unknown`, plus optional variables for data sources) and `expect` (`True`/`False`/`Unknown`, optional fault count, optional trace assertion). A runner library in `TruthWeaver.Testing` and a `truthweaver test <file>` command in ticket 16's tool. Output: pass/fail list, exit code, optional JUnit XML for CI. The stub-assignment engine is shared with the later TUI ticket.

**Blocked by:** 16

**Status:** ready-for-agent

- [ ] Scope above is confirmed by the owner (done 2026-10-11); the architecture rules and `docs/packages.md` reflect the new reference
- [ ] The format is documented with the schema; cases for all three expected values pass and fail correctly in tests
- [ ] `truthweaver test` returns non-zero on any failing case; JUnit output validates
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
