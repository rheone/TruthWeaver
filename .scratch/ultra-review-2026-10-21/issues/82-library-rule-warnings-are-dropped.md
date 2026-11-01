# 82 - Warnings and information from a library rule's text never reach the caller

Status: done
Category: bug
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: 83

## Finding

When a library rule is referenced, the expander keeps only the errors the rule raises in the caller's compile and drops warnings and information (for example a deprecated predicate or a lint). A rule that would warn on its own compiles clean when referenced, in any mode, and there is no other way to see the warning. A comment in the expander says this is intended.

## Open question for the owner

Should a warning or information diagnostic from a library rule reach the caller's result (at the reference's span, naming the library rule), or are library rules expected to be checked on their own when they are added to the library?

## Next step

Decide; if surfaced, pick the code or reuse the original code, and decide the span (see ticket 83).

## Decision (owner, 2026-11-01)

Surface a library rule's warnings and information to the caller under their original codes, at the reference span, with the library locator from ticket 83 in the message. Errors stay wrapped as `TRE0050`.

## Agent Brief

**Category:** bug
**Summary:** Stop dropping warning and information diagnostics that a library rule raises when it is referenced.

**Current behavior:** The expander keeps only errors; a lint or a deprecated-predicate warning in a library rule never reaches the caller.

**Desired behavior:** Each non-error diagnostic from a library rule appears in the caller's result with its original code and severity, at the reference's span, and its message carries the library rule's name and position (ticket 83). Compilation still succeeds when only warnings exist.

**Acceptance criteria:**
- [x] A test fails before and passes after: a library rule that uses a deprecated predicate produces the deprecation warning when referenced, with the library rule named.
- [x] A rule referenced from many sites reports a given warning once per site, not more (see ticket 80).
- [x] Errors still give `TRE0050`.
- [x] Docs state that library rule warnings surface; CHANGELOG entry; the validation list in CLAUDE.md passes.

**Blocked by:** 83 (the locator in the message).

## Resolution (2026-11-01)

Warnings and information came from two places, and each now reaches the caller under its original code and severity, at the reference span, with the ticket 83 locator (`In library rule 'x' at line L, column C.`) in the message:

- **Build of the caller.** The caller's build of the expanded library nodes already raised node warnings such as `TRE0027` at the site; ticket 83's `RuleNodeCompiler.Build` wrapper adds the locator. `RuleNodeCompiler.Compile` now keeps a located diagnostic once (value equality), so the uses of a definition that holds the reference, which share one expansion and one site, report it once. The check build in `LibraryRuleExpander.ExpandRule` still passes no warnings on, so nothing is reported twice.
- **Parse and expansion of the library rule.** `LibraryRuleExpander.ReportErrors` (still wraps errors as `TRE0050`) now passes a non-error on with the reference span, the locator from its library-text span, no path, a hint-only suggestion and no `EnclosedBy`. Example: the `TRE0041` unused definition lint in a library rule. A warning that a nested library rule passed on is recorded in the compile-wide `LibraryRuleParses.Surfaced` set (by reference) and passed through unchanged, so it is located once, at the inner rule.

Tests (`RuleLibraryTests`): `Compile_LibraryRuleUsesADeprecatedPredicate_WarnsAtTheReferenceNamingTheLibraryRule_Test`, `Compile_LibraryRuleWithAnUnusedDefinition_ReportsTheLintNamingTheLibraryRule_Test`, `Compile_LibraryRuleWarningReferencedFromManySites_ReportsItOncePerSite_Test`, `Compile_WarningInANestedLibraryRule_ReportsItOnceNamingTheInnerRule_Test`. Red first: the lint test failed (`Assert.Single() Failure: The collection was empty`) and the once-per-site test failed (`Expected: 2, Actual: 4`). The deprecated-predicate test passed on the ticket 83 base: the caller build already reported `TRE0027` at the site, and ticket 83 added the library rule name to it (before ticket 83 it had no name, so the first criterion is met by 83 plus this ticket). Errors still give `TRE0050` (the existing `TRE0050` tests pass). Docs: `docs/rule-text.md` (Library rules), `docs/strong-k3/specification/diagnostics.md` (`TRE0050`). CHANGELOG entry under Fixed.

Not changed: analyzer and lint findings computed on the whole compiled tree (`Analyzer`, `Linter`) have no span and no locator, also for nodes from a library rule.
