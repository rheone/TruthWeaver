# 05: Unknown consistency audit (read-only)

**What to build:** Answer: does `Unknown` follow the principle of least surprise everywhere, with one meaning and no unexpected deviation? Change no behavior. Sweep these areas and build a matrix of "input condition, result, Fault recorded or not, where it is documented": the evaluator and each operator; every predicate family in `TruthWeaver.Predicates` (null input, empty collection, missing key, out-of-range value, parse failure, bad regex, culture); variable references and data sources (no match, several matches); quantifiers over a null or empty collection; the readers and printers (the constant `Unknown`, the letter `U`); the analyzer and `BddManager`; `Decision.IsSatisfied`, `Project` and `Collapse`; and the `TruthWeaver.Testing` assertions.

Judge with these rules: an `Unknown` answer from a predicate is normal and records no Fault; a thrown exception, a predicate's own timeout or a data failure is `Unknown` plus a Fault; `Unknown` never converts to `True` or `False` outside `COALESCE`, `Project` or `Collapse`.

Fold in the open items of `.scratch/main-review-2026-10-17` (05, 06, 07). Link, do not redo, `.scratch/least-surprise-normalization` tickets 07 and 08. Write up each deviation with options and a recommendation. The owner decides each one before any fix ticket is written.

**Blocked by:** None

**Status:** ready-for-agent

- [ ] The matrix is complete for every area above, with a code reference for each row
- [ ] Each deviation lists the cases, the options and a recommendation
- [ ] No source file is changed
- [ ] The result is in `## Answer`, and the owner has been asked about each deviation, one at a time
