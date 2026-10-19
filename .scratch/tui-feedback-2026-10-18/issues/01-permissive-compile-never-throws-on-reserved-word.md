# 01: Permissive compile never throws on a reserved-word term

**What to build:** A rule such as `a AND OR b` shows a diagnostic in the workbench. Today the permissive compile registers the term `OR` as a predicate and `PredicateRegistryBuilder` throws `ArgumentException` ("A predicate cannot be named 'OR': the name is reserved"). Compile never throws for authoring errors (ADR-0003). Found in the 2026-10-18 real-terminal session.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] A rule that uses a reserved word as a term name compiles to a diagnostic in the workbench, the command line and `RuleConverter`; no exception escapes.
- [x] A test per entry point covers `a AND OR b` and a term named `T`, `F` or `U`.
- [x] The diagnostic names the reserved word.
- [x] `docs/tool-core.md` is updated if its description of the permissive compile changes.
