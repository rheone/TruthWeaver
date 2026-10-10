# 02: NNF, CNF and DNF

**What to build:** A consumer gets a rule in negation, conjunctive or disjunctive normal form, and each form is K3-sound.

**Blocked by:** [01](01-rewrite-soundness-assertion.md) (as the test safety net)

**Status:** ready

- [ ] `ToNnf()`, `ToCnf()` and `ToDnf()` on `CompiledRule<TContext>` return a `CompilationResult<TContext>`, like the other cap-checked rewrites
- [ ] NNF pushes `NOT` to the terms by K3 De Morgan and removes double negation; derived operators expand first
- [ ] CNF and DNF distribute `AND` over `OR` (and the reverse) after NNF; neither law uses a classical complement rule
- [ ] `COALESCE`, inspections and `If` are atoms: no `NOT` is pushed into them
- [ ] A rule over the node cap returns `TRE0016` and no rule
- [ ] Each form is checked with `AssertSound` and the fuzzer; idempotence holds for each
- [ ] Open question 1 (thresholds) is answered before coding
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
