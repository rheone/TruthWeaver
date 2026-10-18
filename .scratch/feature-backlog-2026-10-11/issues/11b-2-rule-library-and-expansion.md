# 11b-2: RuleLibrary and rule("x") expansion in rule text

**Parent:** [11b](11b-rule-library.md)

**What to build:** An immutable `RuleLibrary` in the `TruthWeaver` core: named rules, each with its content hash. The compiler accepts a library. `rule("x")` and `rule("x", hash: "...")` in rule text expand inline at compile time with the definitions machinery, against the caller's predicate registry and data sources. Library rules take no parameters. Diagnostics: a rule missing from the library, a pin that does not match, a cycle across library rules (with the path), and a library rule whose own predicates or sources are missing (the diagnostic names the library rule). New diagnostic codes are never reused.

**Blocked by:** 11b-1

**Status:** on-hold (do not start or dispatch until the owner releases the sub-tickets)

- [ ] `rule("x")` equals the library rule's tree in the compiled result, and evaluates the same
- [ ] A matching pin compiles; a stale pin gives a diagnostic naming the rule and both hashes
- [ ] A cycle gives one diagnostic that lists the cycle path
- [ ] The core takes no new dependency
- [ ] The pages are updated in the same change (rule text, diagnostics table, glossary), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
