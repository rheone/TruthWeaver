# 14: Document ordinal string comparison and the Unicode normalization limit

**What to build:** State in the string predicate documentation that comparisons are ordinal and do not normalise Unicode, so `é` as one code point and as `e` plus a combining accent compare unequal. Callers normalise input to NFC or NFKC before it reaches the predicate. No new API.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The XML docs on the string predicate factories state the limit and the remedy
- [x] The matching pages under `docs/strong-k3/` state it, and the K3 reference sync test passes
- [x] One test pins the behaviour (precomposed and decomposed forms compare unequal)
- [x] The full validation set from CLAUDE.md passes

Source: library-roadmap, grilled 2026-10-09 (decision: document only).
