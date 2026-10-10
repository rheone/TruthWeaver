# 26: Regex predicate options, timeout and cache

**What to build:** `Matches` uses `RegexOptions.None`, a fixed one second timeout and a process-wide pattern cache with no size bound; an invalid literal pattern is found only at evaluation. Decide. Recommended: a bounded cache, a compile-time check for literal patterns, and either a `MatchesIgnoreCase` twin or a documented `(?i)` rule.

**Blocked by:** None (can start immediately)

**Status:** done

**Decision (owner, 2026-10-10):** Bounded pattern cache; a compile-time diagnostic for an invalid literal pattern; a `MatchesIgnoreCase` twin.

- [x] The owner picked the scope
- [x] The cache is bounded and tested if the owner agrees
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
