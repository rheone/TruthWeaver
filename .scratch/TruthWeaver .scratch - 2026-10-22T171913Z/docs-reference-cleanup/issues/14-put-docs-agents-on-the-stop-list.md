# 14: Put docs/agents/ on the stop list

**What to build:** The agent files under `docs/agents/` are exempt from the standard by rule, not by accident. `DocumentationLint` treats `docs/agents/` as part of its stop list, and the `CLAUDE.md` Documentation section names it next to `docs/adr/`, `.scratch/`, `.agents/` and `CHANGELOG.md`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `DocumentationLint` does not follow a link into `docs/agents/` and does not check a file there
- [x] A fixture test proves a root file that links to a `docs/agents/` file does not pull it into scope
- [x] `CLAUDE.md` lists `docs/agents/` in the stop-list sentence
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

- 2026-10-04: Added `docs/agents/` to the `DocumentationLint` stop list and class remarks, to the `CLAUDE.md` Scope bullet, and added the fixture test `InScope_LinkIntoDocsAgents_DoesNotPullFileIntoScope_Test`.
