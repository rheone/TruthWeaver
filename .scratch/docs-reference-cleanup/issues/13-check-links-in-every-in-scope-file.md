# 13: Check links and anchors in every in-scope file

**What to build:** A move of documentation cannot leave a broken link unnoticed. `DocumentationLint` verifies every relative link and every heading anchor in each in-scope Markdown file, with the same logic that the K3 checker uses for `docs/strong-k3/`. A broken link fails `dotnet test` with a `file:line` message.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Every in-scope file has its relative links and heading anchors checked
- [ ] Fixture tests fail on a missing file and on a missing anchor, each with a `file:line` message
- [ ] The current repository passes, or each broken link found is fixed or listed in the baseline
- [ ] The K3 checker and the lint share the link code, so the rules cannot drift
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
