# 23: Clean CONTEXT.md, docs/data-sources.md and AGENTS.md

**What to build:** The remaining in-scope files meet the standard. `CONTEXT.md` loses the "superseded" note and the ADR citations that explain history, keeps its vocabulary and conceptual model, and links to `docs/glossary.md` instead of repeating the vocabulary. Its navigation list keeps its ADR links. `docs/data-sources.md` states its rule without the ADR link. `AGENTS.md` loses its two em dashes.

**Blocked by:** 13

**Status:** done

- [x] `CONTEXT.md`, `docs/data-sources.md` and `AGENTS.md` pass the lint and leave the baseline
- [x] The two `CONTEXT.md` doctests and the four `docs/data-sources.md` doctests still pass
- [x] No statement that `CONTEXT.md` carries only as history remains, and no rule is lost
- [x] No link is broken
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: CONTEXT.md, docs/data-sources.md and AGENTS.md are clean and left the baseline, which is now empty. The baseline field is an empty set. All doctests, the lint and the full suite pass.
