# 11: Validation report and unresolved questions

**What to build:** Run the full Phase 8 validation and produce the report: Strong K3 semantics, Unknown propagation, multi-arity and variadic behaviour, cardinality, XOR and PARITY, coalescing, derived equivalences, projection and collapse; plus documentation validation (one primary category per operation, arity and domains present, Kind explicit, formulas agree with tables, canonical forms valid, Mermaid agrees with formulas, aliases unambiguous, links valid, no operation documented inconsistently, terminology consistent). Produce the list of missing operations and categories, ambiguous semantics and classifications, unverified equivalences and decisions needed in the underlying specification. Do not invent semantics to fill gaps.

**Blocked by:** 05, 06, 07, 08, 09, 10

**Status:** done

- [x] docs/strong-k3/VALIDATION.md contains the report with pass/fail per check and evidence
- [x] The unresolved semantic and classification questions are listed with a recommended resolution each
- [x] The harness passes on the full reference
- [x] Nothing was silently resolved: every gap is listed
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: Phase 8 of the brief. See also [spec](../spec.md).

## Comments

- 2026-10-03: Wrote `docs/strong-k3/VALIDATION.md`. Full repository validation passed (restore --locked-mode, build with 0 warnings, 2222 tests, csharpier, format, roslynator 0 diagnostics); `K3ReferenceTests` 16 of 16 (35 truth, 40 evaluation, 101 canonical markers, 27 operation documents, all links). A throwaway probe (not committed, deleted) compiled 49 rules and evaluated 7 against the real library: every probed claim held. Found two discrepancies outside the reference (README line 559 `AND(a, b, c)` call form; threshold-family arity table 2 vs compiler 1) and listed 12 unresolved questions with recommendations (U1 to U12). Mermaid diagrams were compared with formulas by hand but not rendered (connector unauthorised). No reference document, source or test file changed; tickets 12 to 14 untouched.
