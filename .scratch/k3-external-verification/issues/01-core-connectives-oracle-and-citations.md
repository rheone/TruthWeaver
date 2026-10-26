# 01 - Confirm core-connective oracle exhaustiveness and add external citations

Status: ready-for-agent
Category: enhancement
Severity: high
Slice: Core Strong Kleene connectives (NOT, AND, OR, XOR, EQUIVALENT, IMPLIES, NAND, NOR, PARITY)
Blocked by: none

## What to build

Confirmed, citable proof that every core connective's K3 behavior matches its external definition (for NOT/AND/OR) or is a sound derivation from it (for XOR/EQUIVALENT/IMPLIES/NAND/NOR/PARITY) — not just "a test exists," but a durable record of *why* each is correct, attached to the test/oracle code itself.

## Gap

`src/TruthWeaver.Testing/Fuzzing/K3Oracle.cs` independently defines all nine core connectives (sharing no code with the production `Evaluator`), and `K3LogicTests.cs`/`K3ConformanceTests.cs` compare the evaluator against it. This audit has not yet confirmed, operator by operator, that the comparison is genuinely exhaustive (every `{True, False, Unknown}` combination, at every arity the operator supports) rather than a representative sample. Separately, nothing in the codebase currently records *which* connectives are externally attested (NOT/AND/OR, per Kleene's own tables) versus which are the library's own derived conveniences with no independent external name (XOR/EQUIVALENT/IMPLIES/NAND/NOR/PARITY) — a reader has no way to tell "this matches a cited external source" from "this is proven correct by derivation" without re-deriving it themselves.

## Missing scenario

1. For NOT, AND, and OR: confirm `K3LogicTests`/`K3ConformanceTests` cover every value and value-pair (NOT) or every combination at each tested arity (AND/OR), and add a comment on `K3Oracle`'s definitions of these three citing Kleene, *Introduction to Metamathematics* (1952, pp. 332-335), with the Stanford Encyclopedia of Philosophy's "Many-Valued Logic" entry (<https://plato.stanford.edu/entries/logic-manyvalued/>) as secondary confirmation that these are Strong Kleene's own tables (not weak Kleene, Bochvar, Łukasiewicz, or Priest's LP, which share the carrier but differ in designated values or truth tables).
2. For XOR, EQUIVALENT, IMPLIES, NAND, NOR, and PARITY: confirm the oracle/test coverage is exhaustive at the tested arities, and add a comment stating plainly that these have no independent external name or table in K3 literature — their correctness rests on derivation from NOT/AND/OR (e.g. `NAND(a,b) = NOT(AND(a,b))`, `EQUIVALENT(a,b) = NOT(XOR(a,b))`), which the oracle's own definitions already encode. Do not search for or imply an external citation that doesn't exist for these.
3. If any connective's existing test coverage turns out not to be genuinely exhaustive at a tested arity, extend it before closing this ticket.

## Acceptance criteria

- [ ] NOT, AND, and OR's oracle-backed test coverage is confirmed exhaustive (every value/every combination at every tested arity); a code comment cites Kleene (1952, pp. 332-335) and the SEP entry.
- [ ] XOR, EQUIVALENT, IMPLIES, NAND, NOR, and PARITY's oracle-backed test coverage is confirmed exhaustive at every tested arity; a code comment states their correctness rests on derivation from NOT/AND/OR, not an external citation.
- [ ] Any coverage gap found while confirming the above is closed.
- [ ] No `docs/strong-k3/*.md` page is changed unless this audit finds one factually wrong (not expected, but if found, fix it the same way `ultra-review-2026-10-21` ticket 19 did).
- [ ] The validation list in CLAUDE.md passes.
