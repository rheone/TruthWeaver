# 09: Analyzer lint rules for the new operators

**What to build:** Extend the compile-time analysis to flag useless or suspicious constructs the simplifier already understands: IsKnown/IsUnknown/IsTrue/IsFalse over an operand that can never be Unknown (or never be known), a COALESCE whose first operand is definite, an If whose branches are equal or condition is constant, a threshold or BETWEEN made vacuous by constants, duplicate operands, and double negation. Each finding is an informational or warning diagnostic with a code, a structured suggestion and a K3-sound justification, opt-in or configurable so existing rules do not start producing new warnings unexpectedly.

**Blocked by:** 08

**Status:** done

- [x] Each lint finding has a code, message, span or path and suggestion, and is verified against the oracle (the flagged construct really is redundant)
- [x] No new diagnostics appear for rules that were clean before unless the option is enabled
- [x] README lists the lints and how to configure them
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Implemented as `Linter` (`src/TruthWeaver/Analysis/Linter.cs`), run by `RuleCompiler` after `Analyzer` only when `CompilerOptions.Lints` is not `LintRules.None` (new flags enum, default `None`, so existing rules get no new diagnostics). Seven lints, one code each, `BRE0017` to `BRE0023`, all `Info` with a `Replacement` suggestion and the K3 justification in the message. Owner-approved design: flags option, one code per lint, Info severity, existing `DiagnosticSuggestion`, no source span (expression nodes carry none), structural equality for duplicates.
- Semantic lints use a new internal `Analyzer.Profile` (which of True/False/Unknown a tree can take, from the dual rails). `VacuousCardinality` swaps non-constant operands for fresh terms and asks the same question. `DuplicateOperands` is limited to the operators where K3 makes dropping a repeat exact (`AND`, `OR`, `ANY`, `ALL`, `COALESCE`); `XOR`, `PARITY` and the counting operators are never flagged.
- Oracle: `LintTests` compiles every suggested replacement and checks it with `RuleEquivalence` (equivalent), and checks K3-only subtleties are not flagged. README has a "Lint rules (opt-in)" section.
- Not done: findings have no span/path (tree nodes do not keep one); nested findings (e.g. an `If` and its inspection) are each reported rather than collapsed.

