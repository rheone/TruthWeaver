# 02: Breaking-change notes and release readiness

**What to build:** A changelog and migration guide that lists every public break introduced by the Strong K3 work, so a consumer can upgrade: predicates return TruthValue instead of bool; constants are TruthValue and canonical text is True/False/Unknown instead of lowercase; XnorExpression became EquivalentExpression and the canonical label is EQUIVALENT; the shared infix arity diagnostic constant rename (code unchanged); NXOR became PARITY; Collapse and Project are methods on the result and are no longer rule-language features; Decision.Outcome removed; PrintText became PrintRuleText; JSON/YAML shape changes (new op names, equivalent/xnor/iff reading, collapse/project removal) and the JSON/YAML parsers now reporting a diagnostic instead of throwing for a malformed k. Also verify package versions, NuGet metadata, README install and quick-start sections, and that a pre-1.0 version bump policy is stated.

**Blocked by:** k3-followups 04, k3-followups 05, k3-followups 06

**Status:** done

- [x] CHANGELOG (or the repository's equivalent) lists each break with the old and new form and a migration step
- [x] README quick-start compiles against the current API
- [x] Package metadata and version policy checked and documented
- [x] The list is cross-checked against the git history and the k3-followups tickets

See also [spec](../spec.md).

## Comments

- Documentation only; no source changed, so the validation suite was not run. Owner decisions applied: the project was never published; the migration guide is written against `df8f4b9` (and, because the K3 effort is where nearly everything changed, against `57cf2c9` for the K3 section); the version stays `1.0.0-dev` with breaks allowed until 1.0.0 and SemVer after; the release notes are a root `CHANGELOG.md` in Keep a Changelog format, linked from the README; the README is only compile-checked (executable examples stay in ticket 05).
- `CHANGELOG.md` has an Unreleased section, a breaking-changes table and one migration subsection (old form, new form, migration step) per break. Cross-checked against `git log` and `git diff 57cf2c9 HEAD`, and by running the behaviour. The ticket's list was corrected: `NXOR`, `Collapse`/`Project` as rule operators, `Decision.Outcome`, `PrintText` and the `Expand*` `CompiledRule` return type only ever existed in unreleased K3 commits (not at `57cf2c9`), so they sit in an "interim names" table instead of the migration steps. Breaks the ticket did not list but the code shows: new reserved operator words (a predicate named `any` no longer parses), the `CompilerOptions` and `RuleDiffResult` record shape changes, `FakePredicates` answering `Unknown` without a fault, and the analyzer's K3 tautology semantics. Pre-K3 breaks since `df8f4b9` (the `BooleanRulesEngine` rename, required `Label`/`Description`, mixed `AND`/`OR` parenthesisation, string escaping and `BRE0015`) are listed.
- README quick-start (Getting started, steps 2 to 4) compiled and ran in a fresh project against the current API with no source change needed. Finding: the packages are built with `EnablePreviewFeatures`, so a consumer must set it too or every API use fails with `CA2252`; this is recorded in the CHANGELOG but the README does not say it (not changed, out of scope; suggest a one-line README note).
- Package metadata: `dotnet pack` on the solution produced all five packages and symbol packages at `1.0.0-dev`; license, repository URL, readme and SourceLink are set. Not changed: no `PackageIcon` (deliberate, documented in `Directory.Build.props`) and the copyright line still says 2024-2025.
