# 18: Decide whether the engine exposes a tolerant span classifier

**Status:** ready-for-agent
**Type:** grilling
**Blocked by:** 17
**Source:** architecture review of 2026-10-19, candidate 7

**What to build:** Settle whether the engine or Core exposes classified spans for text that may have errors, so the terminal UI stops owning a second scanner. This decides a public interface and touches ADR-0004. Grill one question at a time.

- [ ] Decision recorded under an `## Answer` heading
- [ ] ADR written or amended if the answer is yes
- [ ] Build tickets created if the answer is yes
