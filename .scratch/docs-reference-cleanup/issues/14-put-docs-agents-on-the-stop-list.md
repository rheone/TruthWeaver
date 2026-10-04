# 14: Put docs/agents/ on the stop list

**What to build:** The agent files under `docs/agents/` are exempt from the standard by rule, not by accident. `DocumentationLint` treats `docs/agents/` as part of its stop list, and the `CLAUDE.md` Documentation section names it next to `docs/adr/`, `.scratch/`, `.agents/` and `CHANGELOG.md`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `DocumentationLint` does not follow a link into `docs/agents/` and does not check a file there
- [ ] A fixture test proves a root file that links to a `docs/agents/` file does not pull it into scope
- [ ] `CLAUDE.md` lists `docs/agents/` in the stop-list sentence
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
