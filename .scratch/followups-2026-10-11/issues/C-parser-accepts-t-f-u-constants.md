# C: The parser accepts T, F and U

**What to build:** Make the DSL parser read the bare words `T`, `F` and `U` (any case) as the constants `True`, `False` and `Unknown`, so `PrintRuleText` can accept `ConstantStyle.Letters` and the output compiles back to an equal tree. These three names become reserved: registering a predicate named `T`, `F` or `U`, or using one as a term name, is an error with a clear message (reuse the reserved-name route from least-surprise ticket 15). `LogicSymbols` is not added to the parser; `PrintRuleText` keeps rejecting it. Record the grammar change in the rule-text docs, the glossary and `CHANGELOG.md`, and add a short note to the ADR that owns the operator and constant spellings (amend it in place, dated).

**Blocked by:** B

**Status:** done

- [x] `T`, `F`, `U` (any case) parse as constants; a property test shows `PrintRuleText` with `Letters` compiles back to an equal tree
- [x] Registering a predicate named `T`, `F` or `U` fails with a clear message; a term of that name is a compile diagnostic
- [x] `PrintRuleText` no longer rejects `Letters`; `TryPrintRuleText` returns true for it; `LogicSymbols` is still rejected
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11 (dispatch run): Decisions made without the owner. (1) A letter followed by `(` (for example `T(x: 1)`) stays a term, so the compiler reports the reserved name instead of a stray-parenthesis syntax error. (2) The "term of that name is a compile diagnostic" reuses `UnknownPredicate` (TRE0002) with the message "'T' is a reserved word and cannot be a predicate name." for any reserved word, not only the three letters; no new code. (3) JSON and YAML still read the constant words only; the letters work in rule text. (4) An override such as `TrueText = "t"` is accepted by `PrintRuleText`. (5) `tests/TruthWeaver.Tests/TestSupport/K3Rule.cs` now names its inputs `a b c d e g h ...` (skips f, t, u), and tests that used `f`, `t` as predicate names in rule text were renamed. (6) ADR-0005 decision 16 carries a dated amendment.
