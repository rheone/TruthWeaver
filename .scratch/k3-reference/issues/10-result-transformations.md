# 10: Result transformations: Project and Collapse

**What to build:** Documents for Project and Collapse as methods on an already-evaluated result (the original three-valued result is always preserved): Project maps Unknown to a chosen definite value and passes True and False through; Collapse applies UnknownAsFalse, UnknownAsTrue or UnknownIsError (a rejected-unresolved outcome, not a fault). Include the mapping tables, relation to the inspections and to SQL WHERE / CHECK, the fail-closed rule for IsSatisfied, and a mapping diagram. State plainly that these are TruthWeaver terms, not Strong K3 literature terms, and that inside a rule COALESCE(x, True|False) gives the Project effect.

**Blocked by:** 04, k3-followups 04, k3-followups 05

**Status:** done

- [x] Two documents conform to the template with mapping tables verified against the oracle
- [x] The documents describe the final method-on-the-result design, matching the code once the follow-up tickets have landed
- [x] The terminology note and the SQL precedents are cited
- [x] The original result remaining available is explicit
- [x] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [x] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [research findings](../../k3-conformance/research-findings.md) item 1. See also [spec](../spec.md).

## Comments

- 2026-10-03: Added `result-transformations/project.md` and `collapse.md` and linked them from the README (pending -> linked). Harness-checked: `k3:truth` tables (Project for `unknownAs` False and True, Collapse for each of the three policies, including `RejectedUnresolved`) and the `Project` canonical form `COALESCE(x, unknownAs)`. `Collapse` has no canonical form (Primitive, per the proposal). Prose, not harness-checked: the SQL `WHERE`/`CHECK` precedents (cited from the research findings), the `IsSatisfied` fail-closed rule, the undefined-policy `ArgumentOutOfRangeException` (k3-followups 29), the Project and Collapse rejection diagnostics. Verified with a throwaway probe against the real library (not committed): both methods on all three results, the undefined policy throwing for True, False and Unknown, `Result`/`IsSatisfied`/`Faults` unchanged after the call, `Collapse(...)` and `Project(...)` in rule text rejected with BRE0001 (DSL) and BRE0014 (JSON). Mermaid mapping diagrams for both. `K3Reference` tests pass (16); a deliberate wrong cell was caught, then restored. No source or test files changed.
