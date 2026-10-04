# 13: Check links and anchors in every in-scope file

**What to build:** A move of documentation cannot leave a broken link unnoticed. `DocumentationLint` verifies every relative link and every heading anchor in each in-scope Markdown file, with the same logic that the K3 checker uses for `docs/strong-k3/`. A broken link fails `dotnet test` with a `file:line` message.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every in-scope file has its relative links and heading anchors checked
- [x] Fixture tests fail on a missing file and on a missing anchor, each with a `file:line` message
- [x] The current repository passes, or each broken link found is fixed or listed in the baseline
- [x] The K3 checker and the lint share the link code, so the rules cannot drift
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

- 2026-10-04: `DocumentationLint.Check` now calls the K3 checker's `CheckLinks` (made `internal`), so both share one rule set. `CheckTree` reads the real repository and reports every broken relative link and missing heading anchor as `file:line`. Three fixture tests cover a missing file, a missing anchor and valid links. The repository had no broken link in a non-baseline file; baseline files are still skipped. The two-argument `Check` assumes every link resolves, for the rule tests that need no repository.
