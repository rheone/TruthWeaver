# 82 - Warnings and information from a library rule's text never reach the caller

Status: ready-for-agent
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
- [ ] A test fails before and passes after: a library rule that uses a deprecated predicate produces the deprecation warning when referenced, with the library rule named.
- [ ] A rule referenced from many sites reports a given warning once per site, not more (see ticket 80).
- [ ] Errors still give `TRE0050`.
- [ ] Docs state that library rule warnings surface; CHANGELOG entry; the validation list in CLAUDE.md passes.

**Blocked by:** 83 (the locator in the message).
