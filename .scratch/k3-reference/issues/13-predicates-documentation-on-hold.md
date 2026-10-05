# 13: Predicates documentation

**What to build:** Hold. Document the predicates category once the predicates have been implemented: how a predicate returns a Strong K3 value, the null-selected-value rule, argument schemas, and one document per predicate (equality, numeric, string, collection, date/time, type tests) following the approved template. Do not write any predicate documentation before the predicates exist, so semantics are never invented. Owner decision 2026-10-03.

**Blocked by:** predicate implementations (the predicate catalog track) and 12; ticket 14 maintains it afterwards

**Status:** done

- [x] Do not start until the predicate catalog implementation work has landed and the owner releases this hold (released 2026-10-04)
- [x] When released, each predicate document passes the harness and conforms to the template

Source: [predicate gap list](../../predicate-catalog/k3-gap-list.md). See also [spec](../spec.md).

## Comments

- 2026-10-04: The owner released the hold. Numeric (14) and scalar (8) families are documented and off the hold list; the other families remain. Convention: one page per factory method, named `<kind>-<factory>.md`, and every NotX twin has its own page (the sync check requires one document per factory stem). Each page links its twin. Shared rules (null selected values, twins, argument kinds) are stated once in `docs/strong-k3/predicates/README.md`. The harness checks predicate pages for links only, so the Answers and Examples tables were verified against the engine with a temporary generated test that is not kept.

- 2026-10-04: String (18) and regex (2) families are documented and off the hold list, with the same conventions. The new pages state `EqualsConfigurable` defaults (`ignoreCase` true, `trim` false), the definite null tests, the regex invalid-pattern fault and the one-second match timeout. The Answers and Examples tables were verified against the engine with a temporary generated test that is not kept.

- 2026-10-04: The collection family (24 pages: emptiness, `Contains`, `ContainsAny`, `ContainsAll`, `IsSubsetOf`, `SetEquals`, the scalar string `In`, the `Count` comparisons and their twins) is documented and off the hold list, with the same conventions. The pages state the string element type, ordinal comparison, the `Unknown` default for a null collection, the null-as-empty-set reading of `SetEquals` under `NullBehavior.False`, and the empty collection, empty argument, duplicate and null element behavior. The Answers and Examples tables were verified against the engine with a temporary generated test that is not kept.

- 2026-10-04: The date-time (10 pages: `After`, `Before`, `Between`, `Outside`, the clock predicates `AfterNow` and `BeforeNow`, and their twins), type-test (10 pages: `IsGuid`, `IsNumeric`, `IsUrl`, `IsString`, `IsDateTimeOffset` and their twins) and selected-value (`selectedvalue-create`, a factory page without a twin) pages are documented. `K3PredicateDocumentationHold.Stems` is empty, and the file stays so the sync check can still use it. The pages state the instant comparison, the inclusive bounds, the `TRE0026` reversed-bounds error, the fixed clock reading with a fake `TimeProvider`, the `Unknown` answer for a null selected value (type tests have no `nullBehavior` option) and the delegate rules of `Create`. The Answers and Examples tables were verified against the engine with a temporary generated test that is not kept. All 87 predicate pages now exist, one per factory method. The ticket is done.
