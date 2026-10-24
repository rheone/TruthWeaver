# 66 - BETWEEN and NONE pages say Simplify keeps the node; Simplify rewrites it when an operand is a constant

Status: needs-triage
Severity: low
Lens: L2 Cardinality+quantifiers
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a throwaway xUnit test compiling BETWEEN(1, 2, True, a, b) and NONE(True, a) and calling CompiledRule.Simplify() found the canonical text of both results no longer contained the word BETWEEN or NONE, confirming Simplifier.Rewrite's ViaExpansion(RewriteLaw.DerivedWithConstant) path folds/rewrites the node when an operand is constant, contrary to the pages' unqualified "Simplify and Canonicalize keep BETWEEN/NONE" statement.
Blocked by: none

## Finding L2.5: BETWEEN and NONE pages say Simplify keeps the node; Simplify rewrites either one when an operand is a constant

Location: `docs/strong-k3/cardinality/between.md:1171` (PLAUSIBLE, low)

between.md:1171 says '`Simplify` and `Canonicalize` keep `BETWEEN`' and none.md:2190/2244 say the same for `NONE`. `Simplifier.Rewrite` (Simplifier.cs:371-372) sends `AnyExpression or AllExpression or NoneExpression or ExactlyOneExpression or BetweenExpression` to `ViaExpansion`, which, whenever any operand is a `ConstantExpression`, expands the node one level (`PrimitiveExpander.ExpandTop`) and keeps the simplified result if it is no larger. The sentence is true only for the no-constant case; the docs are the imprecise side (ADR-0005 decision 10, k3-conformance 27, explicitly lists 'any other derived operator with a constant operand expanded one level').

### Failure scenario

`BETWEEN(1, 2, True, a, b)` simplifies to `AtMost(1, a, b)` (AtLeast side folds to True after the shift), and `NONE(True, a)` simplifies to the constant `False`; a reader relying on the page's 'keeps BETWEEN/NONE' round-trip expectation sees a different node kind after `Simplify()`.

### Expected behavior

The pages state that `Simplify` keeps `BETWEEN`/`NONE` unless an operand is a constant, in which case the derived-with-constant rule may expand it to thresholds or fold it, as ADR-0005 decision 10 describes.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
