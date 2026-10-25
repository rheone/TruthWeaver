# Flaky wall-clock scaling tests

**Status:** needs-triage

A test that asserts on `Stopwatch`-measured wall-clock time (a ratio between a small and a large input, or an
absolute duration cap) is checking a real property: that an operation scales about linearly, not quadratically.
`NoteAttacher`'s fix for [ultra-review-2026-10-21 ticket 25](../ultra-review-2026-10-21/issues/25-comment-note-attachment-quadratic.md)
is the reason such a test exists at all; without one, a quadratic regression in a hot path has no regression test.

The problem is the measurement, not the property: on a shared or loaded CI runner, wall-clock time is noisy. The
existing test (`DslParserTests.Parse_CommentHeavyInput_ScalesLinearlyNotQuadratically_Test`) already does everything
reasonable to resist that noise: a JIT warm-up pass, nine interleaved trials keeping the fastest of each size, and a
generous 8x threshold for a 4x input increase. It still failed once in CI on 2026-10-25 (8.9x) and passed cleanly on
every other run before and after, including a full rerun on the next commit with no code change in between. That is
the signature of a measurement occasionally exceeding even a well-padded margin under contention, not a real
performance regression: the rewritten commit's content was diffed against the pre-rewrite tree and confirmed
identical for this file.

Tickets in `issues/` propose a fix for each affected test. The general direction: move from a wall-clock ratio to a
count-based assertion (nodes visited, comparisons made, or similar) wherever the code already exposes or could
cheaply expose such a count, since that removes the CI-noise dependency entirely rather than padding the margin
further. Where no such count is available, keep the wall-clock assertion but say so in the ticket, since widening the
margin further trades a flaky test for a less useful one.
