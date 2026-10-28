# Split CODING_STANDARDS.md out of CLAUDE.md

**Status:** needs-triage (filed 2026-10-26 from the `/retro` on the Bundle D session, ultra-review tickets 09, 12, 51); a candidate the retro surfaced, not a decided scope.

Source: `CLAUDE.md` carries both navigation pointers and judgement-call coding standards in one file, loaded into every agent's context on every turn in this repo. The `writing-for-agents`/`retro` model treats judgement-call standards as review-time-only material (a separate `CODING_STANDARDS.md`, read by the reviewer agent, not the implementer), kept out of the always-loaded context. This session's own findings (where fault-recording helpers belong, the worktree-per-dispatched-agent rule) landed as new `CLAUDE.md` bullets for lack of anywhere else to put them, growing the always-loaded file further.

## Why this is a judgement call, not a mechanical fix

Unlike the other three findings from the same retro (all fixed directly: `CLAUDE.md` bullets, the `dispatch-tasks` skill), this one changes where content lives repo-wide, not what one rule says. It needs a decision on:

- Whether this repo wants a `CODING_STANDARDS.md` at all, given `CLAUDE.md` already works as the single steering file and a split adds a second file to keep in sync.
- Which existing `CLAUDE.md` content is a judgement call (review-time, movable) versus a navigation pointer or an always-needed fact (implementation-time, stays).
- Whether the review skills already in use (`code-review`) would read the new file, and how - `code-review`'s process currently discovers standards sources by searching the repo for files like `CODING_STANDARDS.md`/`CONTRIBUTING.md`, so creating the file is likely enough to wire it in, but confirm against that skill's own discovery step rather than assuming.

## Candidate split (illustrative, not a decision)

Content in today's `CLAUDE.md` that reads as a judgement call rather than a fact an implementer needs mid-task: most of "Development rules," "Formatting," "Testing" and "Documentation" are closer to review-time standards than navigation. "Architecture," "In-flight work" and "Required validation" read as facts/navigation an implementer needs while working, not only at review. The Cardinal Rules are the user's own standing instructions to the agent, not a code standard - those stay in `CLAUDE.md` regardless of the outcome here.
