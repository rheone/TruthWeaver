# 04: Public RuleFuzzer

**What to build:** A consumer fuzzes their own predicate registry against the K3 oracle and gets a reproducible failure report.

**Blocked by:** 01 (Testing references TruthWeaver)

**Status:** done

- [x] `RuleFuzzer` takes a `PredicateRegistry` and a seed and generates valid rules over its predicates
- [x] It checks that the evaluator matches brute-force Strong Kleene, that `Simplify` and `Canonicalize` preserve meaning, and that the DSL and JSON forms round-trip
- [x] A failure reports the seed and the rule text, and the same seed reproduces it
- [x] The generator and oracle move out of the test-support folder, decoupled from the test predicates, and the repository tests use the public types
- [x] The fuzzer needs only the schemas of the supplied registry
- [x] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
