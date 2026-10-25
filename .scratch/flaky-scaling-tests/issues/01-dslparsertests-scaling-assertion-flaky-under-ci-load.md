# 01 - DslParserTests' comment-scaling assertion is flaky under CI load

Status: needs-triage
Severity: low
Blocked by: none

## Finding: wall-clock ratio assertion failed once under CI contention, not reproducible on rerun

Location: `tests/TruthWeaver.Tests/DslParserTests.cs:270-304` (test), `:311` (the `Stopwatch` measurement it reads)

### Failure scenario

`Parse_CommentHeavyInput_ScalesLinearlyNotQuadratically_Test` parses a comment-heavy rule at 2,000 and 8,000
comments, takes the fastest of nine interleaved trials per size after a JIT warm-up, and asserts the 8,000-comment
parse takes under 8x the 2,000-comment parse (and under 3 seconds absolute). On 2026-10-25 it failed once in GitHub
Actions CI with a 8.9x ratio (16.15 ms -> 143.57 ms), on the push that landed TruthWeaver's history-cleanup rewrite
(commit `4911821`). The very next push (`6f4f032`, no change to this file or anything it touches) ran the full suite
again and passed. The rewritten commit's tree for this file was diffed against the pre-rewrite original and
confirmed byte-identical, so this is not a regression introduced by that rewrite or by any other code change: it is
the test's own measurement occasionally exceeding its margin under a noisy CI runner, despite the warm-up,
best-of-nine, and interleaving already in place to resist exactly that.

### Expected behavior

The test does not fail CI on measurement noise alone. Either the ratio assertion is replaced with a count-based one
(for example, asserting on a token- or node-visit counter that `DslParser`/`NoteAttacher` could expose, rather than
elapsed time), which removes the CI-noise dependency entirely, or, if no such counter is practical here, the margin
and retry strategy are revisited with that tradeoff made explicit, since widening the margin further mostly trades
one flakiness rate for a less sensitive regression test.

### Verification

Confirmed: the 2026-10-25 failure and its clean rerun are both visible in GitHub Actions run history for this repo
(`gh run list --branch main`), and the affected file's content was diffed unchanged across the history rewrite.

## Acceptance criteria

- The test no longer depends solely on wall-clock timing to catch a quadratic regression, or the decision to keep a
  timing-based assertion (and why a count-based one was not practical) is recorded here.
- The fix is itself tested: a change that reintroduces quadratic behavior in the code path this test covers must
  still fail the test.
- The validation list in CLAUDE.md passes.
