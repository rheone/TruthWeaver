# 01: Documentation standard and lint

**What to build:** The repository states one documentation standard and enforces it. `CLAUDE.md` gets a short Documentation section (about 25 lines) that covers scope, purpose, present-tense voice, ASD-STE100 Simplified Technical English (US), project terminology, normative versus explanatory content, `/github-markdown` structure, relative links, Mermaid use, the exclusion of project history from reference documents, and where developer-only detail belongs (code comments). A documentation lint test runs with `dotnet test` over the in-scope Markdown set: root `*.md`, every `README.md` outside `.claude/`, and every file those link to recursively. Recursion stops at `docs/adr/**`, `.scratch/**`, `CHANGELOG.md` and `.agents/**`; a marker comment at the top of a file opts it in or out. The test fails when an in-scope file links to a stop-list path, uses the words "ticket", "ADR-" or "open question", contains an em dash, or, for a file under `docs/strong-k3/`, links outside that folder. Files not yet cleaned sit on an explicit baseline list, so the test passes today and the list shrinks as the cleanup tickets land. Fixture tests prove the lint fails on each rule.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `CLAUDE.md` has a Documentation section that states the standard concisely and holds no reference material
- [x] The lint test finds the in-scope set by the stated rule, honors the stop list and the opt-in or opt-out marker
- [x] Each lint rule has a fixture test that fails with a `file:line` message
- [x] The baseline list holds every in-scope file that currently breaks a rule, and a test fails if a baseline entry no longer breaks any rule
- [x] `dotnet test` passes and the validation list in `CLAUDE.md` passes

## Comments

- 2026-10-04: Done. The lint is `tests/TruthWeaver.Tests/ReferenceDocs/DocumentationLint.cs`, with fixture tests and `DocumentationLintBaseline.cs` (39 files at the start). `CLAUDE.md` has the Documentation section. `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md` is in scope through the README link and is on the baseline; ticket 12 audits it. The full validation list runs in ticket 09.
