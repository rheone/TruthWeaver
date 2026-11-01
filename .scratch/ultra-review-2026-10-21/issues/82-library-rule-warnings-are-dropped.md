# 82 - Warnings and information from a library rule's text never reach the caller

Status: needs-triage
Category: bug
Severity: low
Lens: Code review of ticket 11b-2
Blocked by: none

## Finding

When a library rule is referenced, the expander keeps only the errors the rule raises in the caller's compile and drops warnings and information (for example a deprecated predicate or a lint). A rule that would warn on its own compiles clean when referenced, in any mode, and there is no other way to see the warning. A comment in the expander says this is intended.

## Open question for the owner

Should a warning or information diagnostic from a library rule reach the caller's result (at the reference's span, naming the library rule), or are library rules expected to be checked on their own when they are added to the library?

## Next step

Decide; if surfaced, pick the code or reuse the original code, and decide the span (see ticket 83).
