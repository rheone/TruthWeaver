# 11 - Quantifier atoms keyed by text make RuleEquivalence call valid ToNnf rewrites NotEquivalent

Status: needs-triage
Severity: medium
Lens: F5 Analysis+Rewriting
Verdict: CONFIRMED
Blocked by: none

## Finding F5.3: Quantifier atoms keyed by text make RuleEquivalence call valid ToNnf rewrites NotEquivalent

Location: `src/TruthWeaver/Analysis/Analyzer.cs:808` (CONFIRMED, medium)

The analyzer gives each quantifier an independent, fully free pair of BDD variables, keyed by its canonical text. ToNnf rewrites `NOT FORALL(x IN c, b)` to `EXISTS(x IN c, NOT b)`, which ADR-0010 decision 9 states is an identity, but the two texts become two unrelated atoms. RuleEquivalence.Compare (RuleEquivalence.cs:65-72) then returns a definite NotEquivalent with a counterexample that no collection can produce. It should return Equivalent, or Undecided at least. The comment at Analyzer.cs:805-806 says "every K3 value of the atom is reachable". That is also false: `FORALL(x IN c, True)` is never False and `EXISTS(x IN c, False)` is never True. docs/rewriting-rules.md:402 calls the check "sound", but a NotEquivalent or DoesNotHold verdict across quantifiers is only a candidate.

### Failure scenario

Compile `r = NOT FORALL(x IN items, elementTrue)` and compare it with `r.ToNnf()`, which gives `EXISTS(x IN items, NOT elementTrue)`. RuleEquivalence.Compare returns NotEquivalent with a counterexample that makes one atom True and the other False. So one engine layer (ToNnf) and another (RuleEquivalence) disagree on the same rule. A Testing-package rewrite assertion built on RuleEquivalence would also fail for this valid rewrite.

### Expected behavior

When a difference depends only on quantifier atoms, report Undecided (or flag the counterexample as unverified) instead of NotEquivalent. Otherwise, normalize quantifier atoms (for example push negations to the NNF form) before keying them. Correct the soundness claim in the comment and in the docs.

### Verification

I traced the path in the code and the claim holds.

1. `NormalForms.cs:276-286` (NnfBuilder.VisitCore) rewrites `NOT FORALL(x IN c, b)` to `EXISTS(x IN c, NOT b)`. It flips the kind and negates the body. This matches ADR-0010 decision 9 and docs/rewriting-rules.md:390.

2. `Analyzer.cs:803-817` builds each QuantifierExpression as an atom with two free BDD variables. They are keyed by `QuantifierAtom.IdentityOf(q)` (`Ast/QuantifierAtom.cs:18-21`), which is `CanonicalPrinter.Print(node)`. That text includes the kind and the body. `FORALL(x IN c, b)` and `EXISTS(x IN c, NOT b)` therefore get different text and different, unrelated variable pairs.

3. `RuleEquivalence.Compare` (`RuleEquivalence.cs:55-72`) has no quantifier handling. It calls `Analyzer.FindDifference`. It then returns `NotEquivalent` with a counterexample keyed by the printed atom text. The atoms are independent, so setting A1=True and A2=True separates `NOT A1` from `A2`. The two rules are semantically identical.

4. The comment at `Analyzer.cs:805-806` ("every K3 value of the atom is reachable ... so this is sound") is false. `FORALL(x IN c, True)` is never False, and `EXISTS(x IN c, False)` is never True. The BDD still treats every value of each atom as reachable, and ignores the relation between atoms.

5. docs/rewriting-rules.md:402 and ADR-0010 line 219 say the check is "sound, incomplete". That is correct only for `Equivalent`. `NotEquivalent` or `DoesNotHold` across quantifier atoms is a false negative. The ADR admits incompleteness across differently written bodies. It does not admit that the verdict can be a definite wrong "NotEquivalent".

Impact: no crash, no wrong rule evaluation, and no security effect. The effect is a misleading analysis verdict. A Testing-package rewrite assertion built on RuleEquivalence would fail for a valid ToNnf rewrite. I did not run it. I only traced the code. The documented incompleteness lowers the severity.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
