# 01: Rewrite soundness assertion

**What to build:** A test author checks that a rewrite keeps the meaning of a rule with one call, for the built-in rewrites and for their own.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] `RewriteAssertions.AssertSound(rule, rewrite, expectations)` lives in `TruthWeaver.Testing` and takes `Func<CompiledRule<TContext>, CompiledRule<TContext>>`
- [ ] It always asserts K3 equivalence by `RuleEquivalence.Compare`, with the same `Undecided` behavior as `AssertEquivalent` (it throws as inconclusive)
- [ ] `RewriteExpectations` flags select the extra checks: never larger (node count), and idempotent (rewriting the result changes nothing)
- [ ] A failure message names the failed check and shows the counter-example or the two sizes
- [ ] `RuleFuzzCheck` gains an entry for each new check, so the fuzzer runs them on `Simplify` and `Canonicalize` too
- [ ] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
