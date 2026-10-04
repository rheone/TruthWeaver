# 23: Clean CONTEXT.md, docs/data-sources.md and AGENTS.md

**What to build:** The remaining in-scope files meet the standard. `CONTEXT.md` loses the "superseded" note and the ADR citations that explain history, keeps its vocabulary and conceptual model, and links to `docs/glossary.md` instead of repeating the vocabulary. Its navigation list keeps its ADR links. `docs/data-sources.md` states its rule without the ADR link. `AGENTS.md` loses its two em dashes.

**Blocked by:** 13

**Status:** ready-for-agent

- [ ] `CONTEXT.md`, `docs/data-sources.md` and `AGENTS.md` pass the lint and leave the baseline
- [ ] The two `CONTEXT.md` doctests and the four `docs/data-sources.md` doctests still pass
- [ ] No statement that `CONTEXT.md` carries only as history remains, and no rule is lost
- [ ] No link is broken
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
