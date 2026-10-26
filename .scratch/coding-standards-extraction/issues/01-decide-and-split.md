# 01 - Decide whether to split CODING_STANDARDS.md out of CLAUDE.md, and do it

Status: needs-triage
Category: enhancement
Severity: low
Blocked by: none

## Summary

`CLAUDE.md` mixes always-loaded navigation with judgement-call coding standards. The `writing-for-agents`/`retro` model this repo's skills already follow treats the two differently: navigation stays in `CLAUDE.md` (sparse, pointer-only), judgement calls move to a `CODING_STANDARDS.md` read at review time, not on every implementation turn. This ticket is open because the split itself is a judgement call (see `spec.md`), not because the destination is unclear once a decision is made.

## Before an agent implements this

Grill the owner (per CLAUDE.md cardinal rule 8, one question at a time) on the open decisions in `spec.md`:

1. Split at all, or leave `CLAUDE.md` as the single file?
2. If splitting: which sections move (the candidate split in `spec.md` is illustrative only).
3. Whether `code-review`'s existing standards-source discovery is sufficient, or needs a pointer added.

## Acceptance criteria (once triaged)

- [ ] The owner's decision on each question above is recorded in `spec.md`.
- [ ] If the decision is to split: `CODING_STANDARDS.md` exists at the repo root, holds the judgement-call content moved out of `CLAUDE.md`, and `CLAUDE.md` points to it; no content is duplicated in both.
- [ ] `code-review`'s Standards sub-agent is confirmed to read the new file (run the skill once against a trivial diff and check its sources list, or inspect its discovery step directly).
- [ ] The validation list in `CLAUDE.md` passes (this ticket only moves prose; no code or test should need to change).

## Comments

> *Filed from the 2026-10-26 retro on the Bundle D session - a structural candidate the retro flagged as "worth considering," not a specific recommendation. The other three retro findings from the same session were fixed directly (see `CLAUDE.md`'s Git/Architecture/Claude Code sections and the `dispatch-tasks` skill, both edited 2026-10-26) and needed no ticket.*
