# 14: Decide what to do about tests that already lacked XML summaries on main

**What to build:** About 280 test methods in changed files that already existed on main have no XML summary (largest: `RuleTreeRenderingTests`, `RuleBuilderTests`, `StringPredicatesTests`, `XorExactlyOneThresholdTests`, `JsonTreeTests`, `YamlTreeTests`, `LexerTests`, `DslParserTests`, `CanonicalPrinterTests`). This is pre-existing style, not a branch regression ([report](../07-review-report.md), finding 4). It sits next to [k3-followups 28](../../k3-followups/issues/28-decide-aaa-and-nsubstitute-rules.md), which asks whether the Testing rules are enforced or amended. Options: (1) amend CLAUDE.md so the XML-comment rule applies to new and touched tests only (recommended, matches how the branch has worked); (2) backfill all of them in batches sized to one context window; (3) leave as is. Resolve together with 28.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The owner picks 1, 2 or 3
- [ ] If 1, CLAUDE.md and AGENTS.md state the scope of the rule
- [ ] If 2, batches are listed per test file and tracked

See also [spec](../spec.md).

## Comments

- 2026-10-03: Owner chose option 1: the XML-summary rule applies to new and touched tests only. Work tracked by k3-followups 35.
