# 01: DSL threshold k that is not a whole number is a compile error

**What to build:** A rule author who writes `AtLeast(1.5, a, b)` or `AtLeast(99999999999, a, b)` gets a syntax diagnostic instead of a rule that silently means `AtLeast(0, ...)`. `k`, `min` and `max` share one integer-bound parser with invariant culture and no fallback to 0.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A failing test first: each bad `k` (fraction, overflow, negative text) is rejected with the same diagnostic `BETWEEN` gives
- [x] `k`, `min` and `max` use one parser; no `: 0` fallback remains
- [x] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
