# 26 - NormalForms and rewriting-rules.md treat If as not information-monotone; the K3 reference says it is monotone

Status: needs-triage
Severity: medium
Lens: F5 Analysis+Rewriting; L3 Externals+If+constants
Verdict: PLAUSIBLE
Blocked by: none

## Finding F5.4: NormalForms keeps If as an atom as if it were not monotone, but the K3 reference says it is a monotone connective

Location: `src/TruthWeaver/Rewriting/NormalForms.cs:269` (PLAUSIBLE, medium)

NormalForms groups IfExpression with COALESCE and the inspections as "not information-monotone". So it pushes no NOT into an If and never distributes an If in CNF or DNF. docs/strong-k3/functions/if.md:16, the glossary and CLAUDE.md say If is a Strong Kleene connective (monotone), and IfForm.Build gives an exact AND/OR/NOT definition. NandNorExpander already uses that definition. docs/rewriting-rules.md:320 repeats the wrong classification, so the code and that page disagree with the K3 reference.

### Failure scenario

`ToNnf("NOT If(c, a, b)")` returns `NOT If(c, a, b)` unchanged. Under the K3 reference this is not negation normal form, because NOT sits above a connective. The NNF is `(NOT c OR NOT a) AND (c OR NOT b) AND (NOT a OR NOT b)`. ToCnf and ToDnf likewise return a tree with an If inside a clause, so the result is not CNF or DNF.

### Expected behavior

Expand If through IfForm (via the default PrimitiveExpander.ExpandTop arm) like the other derived connectives. Keep only COALESCE, the inspections and quantifiers as atoms, and correct docs/rewriting-rules.md:320.

### Verification

not verified (medium or low)

## Finding L3.8: rewriting-rules.md and NormalForms.cs call `If` not information-monotone, contradicting the K3 reference

Location: `docs/rewriting-rules.md:320` (PLAUSIBLE, low)

docs/rewriting-rules.md:320 ('COALESCE, the inspections ... and If are not information-monotone, so each is an atom') and :375, and the comment at NormalForms.cs:268, classify If as non-monotone. docs/strong-k3/functions/if.md:16 and docs/strong-k3/specification/semantics.md:184 state If is a Strong Kleene connective, monotone in the information order. Keeping If as an NNF atom is a sound design choice, but the stated reason is false and contradicts the reference.

### Failure scenario

A reader of rewriting-rules.md concludes a rule containing If is outside the no-tautology theorem and NAND/NOR expressiveness, which semantics.md says applies only to COALESCE and the inspections; ExpandToNand actually does expand If fully.

### Expected behavior

rewriting-rules.md and the NormalForms.cs comment give the real reason If stays an atom in NNF (a design choice to keep the node) and do not call it non-monotone.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
