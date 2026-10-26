# K3 external verification: prove every operator against Strong Kleene sources

**Status:** grilled 2026-10-26, ready for implementation (tickets 01-05, all unblocked)

Source: a `/grilling` session scoped directly by the owner, who framed it as foundational: "the fundamental reason this library exists" is that the K3 expression language's truth-seeking be provably correct, cross-checked against external Strong Kleene Logic (K3) sources, with a check for any standard operator missing from the implementation.

## Scope (settled)

- **In scope:** the core Strong Kleene connectives (`NOT`/`AND`/`OR`/`IMPLIES`/`EQUIVALENT`/`XOR`/`NAND`/`NOR`/`PARITY`), the derived/cardinality operators (`AtLeast`/`AtMost`/`Exactly`/`ExactlyOne`/`GreaterThan`/`LessThan`/`Any`/`All`/`None`/`Between`/`If`), the external operators (`COALESCE`, `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown`), and the quantifiers (`FORALL`/`EXISTS`).
- **Out of scope:** the domain predicate library (`TruthWeaver.Predicates` — regex, GUID, numeric/collection/calendar predicates). Those test values, not K3 logic itself, and are already the subject of the in-flight `ultra-review-2026-10-21`/`coverage-gaps-2026-10-25` triage.

## Deliverable (settled)

Test-coverage audit only. No new "External verification" sections are added to `docs/strong-k3/*.md` pages — documentation is touched only where the audit finds it factually wrong (the existing pattern already used by other tickets, e.g. `ultra-review-2026-10-21` ticket 19's `Coalesce` fold fix). External-source citations and derivation notes live as durable code comments on the relevant oracle/test files, not as reference-doc sections.

## Facts already established (do not re-derive)

- `src/TruthWeaver.Testing/Fuzzing/K3Oracle.cs` is an independent reference implementation that deliberately shares no code with the production `Evaluator`. Every in-scope operator already has at least one test comparing the evaluator against this oracle, exhaustively over `{True, False, Unknown}` assignments at small arities (`K3LogicTests.cs`, `K3ConformanceTests.cs`, `ParityTests.cs`, `AnyAllNoneTests.cs`, `BetweenTests.cs`, `CoalesceTests.cs`, `IfTests.cs`, `QuantifierLayerAgreementTests.cs`). **There is no "write tests from scratch" work here** — this is an audit (confirm the existing exhaustiveness is real, not just present) plus one concrete gap to close.
- **The one confirmed gap:** `QuantifierLayerAgreementTests.cs` tests a fixed list of composed expressions (a quantifier combined with AND/OR/XOR/IMPLIES/IF/ANY/threshold/COALESCE/NAND) rather than a true combinatorial sweep over quantifier kind, outer operator, and element count, unlike every other family's genuinely exhaustive `Assignments(arity)`-style loop.
- **Comparison baseline:** Strong Kleene only. No cross-checking against Łukasiewicz, Bochvar, or Priest's LP — ADR-0005 already commits to Strong Kleene specifically, and comparing against a different three-valued system would produce "different system, different answer" noise, not a correctness finding.
- **External citations researched and verified:**
  - `NOT`/`AND`/`OR`: Kleene, *Introduction to Metamathematics* (North-Holland/Van Nostrand, 1952), pp. 332-335 — the original three-valued truth tables. Do **not** cite "1938" — that date belongs to a different Kleene paper (ordinal notation), unrelated to three-valued logic.
  - Secondary confirmation: Stanford Encyclopedia of Philosophy, "Many-Valued Logic" (<https://plato.stanford.edu/entries/logic-manyvalued/>) — gives K3's tables and explicitly distinguishes K3 (only `True` designated) from weak Kleene/Bochvar (Unknown is "infectious") and from Priest's LP (same tables, both `True` and `Unknown` designated).
  - Third independent source: Priest, *An Introduction to Non-Classical Logic* (2nd ed., Cambridge University Press, 2008) — confirmed via secondary reference only; verify page numbers directly before citing a specific page.
  - `XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`/`PARITY`: **no independent external name or table exists for these in K3 literature.** Their correctness must rest on derivation from the cited `NOT`/`AND`/`OR` (e.g. `NAND(a,b) = NOT(AND(a,b))`), not on a citation that doesn't exist. Do not search for or imply one.
  - `COALESCE`: not treated as a logic connective in any three-valued-logic source found. Traces to Codd's 1979 paper (the origin of SQL's NULL-propagation semantics) as a programming/SQL idiom, not logic literature. Its empty-input identity (`Unknown`) is proven by the monoid-fold theorem: an associative operation with a two-sided identity element must fold an empty input to that identity (Haskell's `Data.Monoid.First`, `mconcat [] == mempty`, is a clean, directly citable statement of this).
  - The project's own "information order" framing for `Unknown` (domain-theory style) has **no found external source** bridging it to Scott's bottom element. Treat as the project's own analogy, not an attested citation, if it comes up.
- **Missing-operator audit, already answered:** comparing the current operator set against Strong Kleene literature finds nothing missing. Only `¬`/`∧`/`∨` are literature-attested K3 connectives; every other operator in the set (including the cardinality family, `If`, `COALESCE`, the inspections, and the quantifiers) is the library's own derived/named convenience, verified by derivation rather than citation. This was a sanity re-check of ADR-0005's already-closed operator-set decision, not expected to reopen it.

## Severity (settled)

Every ticket in this effort carries a **critical/high floor**, regardless of how narrow or academic the trigger — both for an actual behavioral discrepancy from Strong Kleene's definition, and for a genuinely unproven/under-tested operator. This is a deliberate departure from normal severity triage (contrast the `coverage-gaps-2026-10-25` effort, which triaged by blast radius): the owner's framing is that this correctness story is the reason the library exists, so "unproven" is treated the same as "wrong" for prioritization purposes.

## Tickets

01-05, all unblocked (no ticket in this set depends on another). See `issues/`.
