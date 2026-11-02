# 18: Decide whether the engine exposes a tolerant span classifier

**Status:** needs-info
**Type:** grilling
**Blocked by:** 17
**Source:** architecture review of 2026-10-19, candidate 7

**What to build:** Settle whether the engine or Core exposes classified spans for text that may have errors, so the terminal UI stops owning a second scanner. This decides a public interface and touches ADR-0004. Grill one question at a time.

- [ ] Decision recorded under an `## Answer` heading
- [ ] ADR written or amended if the answer is yes
- [ ] Build tickets created if the answer is yes

## Open question (recorded 2026-11-02; the agent skipped this ticket)

This is a decision ticket: only the owner can answer it. The owner chose to grill it later, with the skipped `rule-text-language-server` effort in view, because ticket 01 of that effort ("shared token classifier") overlaps with it. Blocker 17 (memoize the syntax marks) is done.

Open choices, to settle one at a time:

1. Does the engine (or Core) expose classified spans for text that may have errors, or does the terminal UI keep its own scanner?
2. If it is exposed: which package owns the public type, and does it replace the language-server token classifier or feed it?
3. Does ADR-0004 need an amendment, or a new ADR?

When answered, set the status back to `ready-for-agent` (build tickets) or `wontfix`.
