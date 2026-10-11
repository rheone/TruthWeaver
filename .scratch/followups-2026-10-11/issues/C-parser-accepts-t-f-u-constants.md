# C: The parser accepts T, F and U

**What to build:** Make the DSL parser read the bare words `T`, `F` and `U` (any case) as the constants `True`, `False` and `Unknown`, so `PrintRuleText` can accept `ConstantStyle.Letters` and the output compiles back to an equal tree. These three names become reserved: registering a predicate named `T`, `F` or `U`, or using one as a term name, is an error with a clear message (reuse the reserved-name route from least-surprise ticket 15). `LogicSymbols` is not added to the parser; `PrintRuleText` keeps rejecting it. Record the grammar change in the rule-text docs, the glossary and `CHANGELOG.md`, and add a short note to the ADR that owns the operator and constant spellings (amend it in place, dated).

**Blocked by:** B

**Status:** ready-for-agent

- [ ] `T`, `F`, `U` (any case) parse as constants; a property test shows `PrintRuleText` with `Letters` compiles back to an equal tree
- [ ] Registering a predicate named `T`, `F` or `U` fails with a clear message; a term of that name is a compile diagnostic
- [ ] `PrintRuleText` no longer rejects `Letters`; `TryPrintRuleText` returns true for it; `LogicSymbols` is still rejected
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
