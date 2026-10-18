# 08: Voice pass and full validation

**What to build:** Run `/humanizer` and `/voice-of-robert` over all 27 READMEs, then the full validation set from `CLAUDE.md`, including the documentation lint and doc example tests.

**Blocked by:** 02, 03, 04, 05, 06, 07

**Status:** done

- [x] No README links to an ADR, `.scratch` or `CHANGELOG.md`
- [x] The drift test, `DocumentationLint` and the doc example tests pass
- [x] `dotnet run scripts/format-all.cs` is clean

## Comments

- Voice pass: all 27 READMEs were scanned for the humanizer and voice-of-robert tells (dashes, not-X-but-Y, stock words, bold decoration, curly quotes, change language). None remained. One edit: `tests/TruthWeaver.Architecture.Tests/README.md` named an ADR in text, which `DocumentationLint` rejects; it now says "the boundary rules".
- Passing: `ProjectReadmeTests` (34), `DocumentationLint`, `K3Reference` and `DocExample` (115), `csharpier check .`, `normalize-text` and `format-data` scoped to the READMEs. No README links to an ADR, `.scratch` or `CHANGELOG.md`.
- Fixed in `ProjectReadmeTests.cs`: two xUnit2033 findings (use the `Assert.Single` return value).
- Open, not from this ticket: `dotnet format --verify-no-changes --severity info` and Roslynator report SA1204 (static after instance) in `AssignmentPaneTests.cs` and `ThemeTests.cs`, and SA1623 in `Workbench.Theme.cs`. These are tui files edited by other tickets. `format-all` stays unticked until they are fixed. `dotnet format` also lists pre-existing CA1822/IDE0060 findings in unrelated test files.
- 2026-10-18: Full validation passed on the combined result 2026-10-18 (6660 tests; csharpier, dotnet format, roslynator and normalize-text clean). The SA1204/SA1623 findings listed above no longer reproduce, and the CA1822/IDE0060 findings did not reproduce (owner decision: dropped).
