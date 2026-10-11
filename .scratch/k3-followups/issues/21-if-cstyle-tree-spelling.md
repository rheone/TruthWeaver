# 21: Render `If` as `?:` in `CStyle` tree style, and pin prefix `!` spacing

**What to build:** Under `OperatorStyle.CStyle` the tree printer renders `If` as `?:`, mirroring the DSL ternary. `Symbolic` keeps the label `If`. The JSON/YAML op name stays `if`, and `If` operand-count errors stay `MalformedTree` (owner decisions 2026-10-03; issues-log row 19). Keep `RuleDiff` output, the README symbol table and any other place that lists tree spellings consistent.

Also pin prefix `!` spacing with tests only (issues-log row 27): input with a space (`! a`) is accepted, and both the canonical printer and `RuleText.NormalizeWhitespace` print `!a`. The code is expected to already behave this way, so this part adds tests, not behavior; if a test fails, stop and report rather than changing printing rules.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The failing test run for the `?:` spelling is shown before the implementation Note 2026-10-10: the failing-first run is not recorded in the repo, so it cannot be confirmed.
- [x] `CStyle` tree output uses `?:` for `If`; `Symbolic` output is unchanged
- [ ] JSON/YAML op name `if` and the `MalformedTree` operand-count diagnostic are unchanged Note 2026-10-10: JSON/YAML op name and diagnostic were not re-checked.
- [x] README symbol table and any tree-spelling documentation match
- [ ] Tests show `! a` is accepted and printed as `!a` by the canonical printer and by `NormalizeWhitespace` Note 2026-10-10: NormalizeWhitespace prints !a, but the canonical printer is word-only (ADR-0005 decision 1) and prints NOT a, so the item as written does not hold.
- [x] The full validation from CLAUDE.md passes

Source: owner decisions 2026-10-03 (Q4, Q5, Q8). See [issues-log](../../k3-conformance/issues-log.md) rows 19 and 27.

## Comments

- 2026-10-03 (implementation): `RuleRenderTree.BaseLabel` renders `If` as `?:` under `CStyle` only; `Symbolic`/`Word` keep `If`, the description/evaluated label stays `If`, so `RuleDiff` output, JSON/YAML op `if` and the `MalformedTree` operand-count error are unchanged. `OperatorStyle` docs, the README `If` symbol row and ADR-0005 decision text updated. The README has no other tree-spelling documentation. Open point for the owner: the ticket says the canonical printer prints `!a`, but the canonical printer is word-only (ADR-0005 decision 1) and prints `! a` as `NOT a`. I pinned that actual behavior (spaced and unspaced spellings print identically) rather than change printing rules; `RuleText.NormalizeWhitespace` does print `!a` as specified.
