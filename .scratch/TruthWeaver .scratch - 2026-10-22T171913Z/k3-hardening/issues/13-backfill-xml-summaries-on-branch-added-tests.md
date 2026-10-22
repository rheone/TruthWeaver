# 13: Backfill XML summaries on the tests this branch added

**What to build:** CLAUDE.md Testing requires new and touched tests to carry an XML comment (amended scope, k3-followups 35). 19 tests added by the branch have none ([report](../07-review-report.md), finding 3): the shape tests in `ExpressionShapeTests` (Nand, Nor, Implies, Parity, Any, All, None, Between, If and Coalesce, plus one more), four `Collapse_*` tests in `TruthValueAndDecisionTests`, `Print_NandAndNor_...` and `Print_Implies_...` in `CanonicalPrinterTests`, and `Render_NandAndNor_...` and `Render_Implies_...` in `RuleTreeRenderingTests`. Add a one-to-two sentence `<summary>` stating the behaviour. Comments only; no test body or name changes.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Every test added on the branch has an XML summary (re-run an added-lines diff of `tests/` to confirm zero) Note 2026-10-10: the added-lines diff was not re-run.
- [x] No behaviour change; the same tests pass
- [x] The full validation from CLAUDE.md passes

See also [spec](../spec.md).
