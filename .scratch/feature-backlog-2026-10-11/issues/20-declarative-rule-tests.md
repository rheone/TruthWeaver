# 20: Declarative YAML rule tests and runner

**Group:** D tooling and integration

**What to build:** A test file format (YAML, with a JSON schema) for rule authors who do not write C#: a rule (inline or by path), named cases with `given` (stub values per term, as `True`/`False`/`Unknown`, plus optional variables for data sources) and `expect` (`True`/`False`/`Unknown`, optional fault count, optional trace assertion). A runner library in `TruthWeaver.Testing` or the tool project (decide in the scope step) and a `truthweaver test <file>` command in ticket 16's tool. Output: pass/fail list, exit code, optional JUnit XML for CI. The stub-assignment engine is shared with the later TUI ticket.

**Blocked by:** 16

**Status:** ready-for-agent

- [ ] Before any file is created, the worker confirms the scope with the owner (standing rule: new packages, new projects and heavy dependencies need explicit confirmation); the confirmed scope is written at the top of this ticket
- [ ] The format is documented with the schema; cases for all three expected values pass and fail correctly in tests
- [ ] `truthweaver test` returns non-zero on any failing case; JUnit output validates
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
