# 39 - ADR-0005 cites a Project(bool) signature, diagnostic codes and a decision number that the code and docs do not use

Status: needs-triage
Severity: medium
Lens: F1 Ast+Abstractions; L3 Externals+If+constants; L5 Least surprise; L1 Connectives; L2 Cardinality+quantifiers
Verdict: PLAUSIBLE
Blocked by: none

## Finding F1.4: ADR-0005 still says Decision.Project(bool); the docs and code use Project(TruthValue) and reject Unknown at run time

Location: `src/TruthWeaver.Abstractions/Decision.cs:82` (PLAUSIBLE, low)

ADR-0005 decision 12 (line 335) and decision 19 (line 625) say the method is `Decision.Project(bool unknownAs)`, with a bool 'so an Unknown replacement cannot be requested'. The code, docs/strong-k3/result-transformations/project.md and CONTEXT.md all use Project(TruthValue) and throw ArgumentOutOfRangeException for Unknown. The docs and code agree, so the ADR is the stale side: it records a compile-time guarantee that no longer exists, and no amendment records the change.

### Failure scenario

A reader who trusts the ADR expects decision.Project(TruthValue.Unknown) not to compile. It compiles and throws ArgumentOutOfRangeException at run time.

### Expected behavior

ADR-0005 gets an amendment that records the TruthValue parameter and the run-time check, so the ADR matches project.md.

### Verification

not verified (medium or low)

## Finding L3.7: ADR-0005 decision 12 documents Decision.Project(bool unknownAs); the API and project.md use a TruthValue parameter

Location: `docs/adr/0005-strong-k3-language-surface.md:334` (PLAUSIBLE, low)

Decision 12 (lines 334-337) and decision 19 (line 626) say `Decision.Project(bool unknownAs)` with the rationale that a bool makes an Unknown replacement unrequestable. Decision.cs:82 declares `Project(TruthValue unknownAs)` and throws ArgumentOutOfRangeException for Unknown; docs/strong-k3/result-transformations/project.md:11 and :27 document the TruthValue form. Code and reference agree; the ADR is stale.

### Failure scenario

A caller following the ADR writes `decision.Project(false)`; it does not compile (no implicit bool to TruthValue conversion).

### Expected behavior

ADR-0005 decisions 12 and 19 are amended in place to the TruthValue signature and the throw-on-Unknown rule.

### Verification

not verified (medium or low)

## Finding L5.1: ADR-0005 decisions 12 and 14 describe a Project signature and rejection codes the code does not have

Location: `docs/adr/0005-strong-k3-language-surface.md:335` (PLAUSIBLE, medium)

ADR-0005 decision 12 says `Decision.Project(bool unknownAs)` ("the parameter is a bool so an Unknown replacement cannot be requested") and decisions 12/14 say a rule that spells Project/Collapse is rejected with SyntaxError TRE0001 (DSL) or MalformedTree TRE0014 (JSON/YAML). The code has `Project(TruthValue unknownAs)` that throws ArgumentOutOfRangeException at runtime for Unknown (src/TruthWeaver.Abstractions/Decision.cs:82-88), and every surface rejects Project/Collapse with UnknownPredicate TRE0002 (src/TruthWeaver/Parsing/DslParser.cs:1083,1115; CollapseRejection.cs:20). CONTEXT.md, docs/strong-k3/result-transformations/project.md:11,48 and specification/diagnostics.md:16 agree with the code, so the ADR text is the stale side.

### Failure scenario

A reader who trusts the ADR (declared ground truth) writes `decision.Project(true)` and gets a compile error, or filters diagnostics on TRE0001/TRE0014 to catch a persisted `Collapse(...)` rule and never sees it because the engine emits TRE0002. Expected: the ADR and the code name the same signature and code.

### Expected behavior

ADR-0005 decisions 12 and 14 are amended in place to `Project(TruthValue unknownAs)` (Unknown rejected with ArgumentOutOfRangeException) and to UnknownPredicate (TRE0002) on every surface, matching the code and the reference pages.

### Verification

not verified (medium or low)

## Finding L1.3: ADR-0005 cites diagnostic codes for NXOR and for short PARITY/ANY/ALL/NONE nodes that the diagnostics page and the code do not use

Location: `docs/adr/0005-strong-k3-language-surface.md:122` (PLAUSIBLE, low)

ADR-0005 decision 4 (line 122-123) says rule text, JSON or YAML that writes `NXOR` is rejected with `SyntaxError` in the DSL and `MalformedTree` in JSON and YAML, and decision 13 (lines 403-404) says a `PARITY` with fewer than two operands is `MalformedTree`. `docs/strong-k3/specification/diagnostics.md` lists `NXOR` under `TRE0002` `UnknownPredicate` and short n-ary nodes under `TRE0006` `InfixArityViolation`, and the code does the same (`DslParser.RejectNxor` and `TreeFormatReader.ReadOperator` use `DiagnosticCodes.UnknownPredicate`; `RuleNodeCompiler.BuildVariadic` uses `InfixArityViolation`). The ADR text is the stale side; the reference page and the code agree.

### Failure scenario

A reader who checks the ADR writes a test or a tool filter that expects `TRE0001`/`TRE0014` for `NXOR(a, b)` or `PARITY(a)`; the compiler returns `TRE0002`/`TRE0006` and the check never matches.

### Expected behavior

ADR-0005 decisions 4 and 13 should carry an in-place amendment note that the codes are `TRE0002` for `NXOR`, `Project` and `Collapse` and `TRE0006` for an operand-count violation of any n-ary operator, as diagnostics.md and the code state.

### Verification

not verified (medium or low)

## Finding L2.4: ADR-0005 decisions 13/14 name MalformedTree for too-few ANY/ALL/NONE/BETWEEN operands and non-integer BETWEEN bounds; docs and code use InfixArityViolation and InvalidThresholdValue

Location: `docs/adr/0005-strong-k3-language-surface.md:419` (PLAUSIBLE, low)

ADR-0005 lines 418-420 and 439 say fewer than two operands for `ANY`/`ALL`/`NONE`/`BETWEEN` 'is `MalformedTree`', and lines 445-446 say non-integer `min`/`max` in JSON/YAML are 'else `MalformedTree`'. The compiler raises `InfixArityViolation` (RuleNodeCompiler.cs:458 via BuildVariadic, :659 for BETWEEN) and the tree reader raises `InvalidThresholdValue` for a missing or non-integer bound (TreeFormatReader.cs:561); docs/strong-k3/cardinality/any.md:491, between.md:1150-1153 and specification/diagnostics.md:22 agree with the code. The ADR is the stale side; the task frames an ADR as ground truth, so it should be amended in place (as ADR-0005 already is elsewhere) rather than left contradicting the reference.

### Failure scenario

A reader checking `{"op":"between","min":1.5,"max":2,"operands":[...]}` against the ADR expects `TRE0014` (MalformedTree) and writes tooling or tests around that code; the engine reports `TRE0008` (InvalidThresholdValue).

### Expected behavior

ADR-0005's implemented-in notes for k3-conformance 13 and 14 name `InfixArityViolation` (`TRE0006`) for the operand minimum and `InvalidThresholdValue` (`TRE0008`) for a missing or non-integer bound, matching diagnostics.md and the code.

### Verification

not verified (medium or low)

## Finding L3.6: ADR-0005 says a wrong operand count for If and the inspections is MalformedTree; the code and the K3 reference report InfixArityViolation

Location: `docs/adr/0005-strong-k3-language-surface.md:491` (PLAUSIBLE, low)

Decision 13 (k3-conformance 16, line 491; k3-conformance 17, line 510) and the 'Decided, If (log row 19)' entry (line 664) say the operand-count error is MalformedTree. RuleNodeCompiler.BuildIf (RuleNodeCompiler.cs:325) and BuildInspection (:351) report InfixArityViolation (TRE0006), and docs/strong-k3/functions/if.md:26, istrue.md:29 and diagnostics.md's TRE0006 row document that code. The ADR is stale relative to the shipped behavior and the reference pages.

### Failure scenario

A host that follows the ADR filters compile results for TRE0014 to detect `If(a, b)` written in JSON; the compiler emits TRE0006 instead and the mistake is not caught by that filter.

### Expected behavior

ADR-0005 carries an in-place amendment that the If/inspection arity error is InfixArityViolation (TRE0006), as the reference documents.

### Verification

not verified (medium or low)

## Finding F1.6: TruthValueText cites the wrong ADR-0005 decision

Location: `src/TruthWeaver/Ast/TruthValueText.cs:4` (PLAUSIBLE, low)

The summary says the spelling table implements 'ADR-0005 decision 11'. Decision 11 covers structured validation messages. The constant spellings (case-insensitive True/False/Unknown, JSON 'unknown', the T/F/U letters) are decision 16.

### Failure scenario

A maintainer follows the reference to check how the letters or the JSON 'unknown' constant should be read and lands on the diagnostics decision instead.

### Expected behavior

The comment cites ADR-0005 decision 16.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
