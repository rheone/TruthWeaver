# 68 - EvaluationLog.PredicateFaulted's message text says "Predicate fault" for a collection-level fault

Status: ready-for-agent
Category: bug
Severity: low
Lens: F4 Evaluation
Verdict: CONFIRMED
Blocked by: none

## Finding: The shared fault-recording path's log message is worded for a predicate, but ticket 51 now also routes collection-level faults through it

Location: `src/TruthWeaver/Logging/EvaluationLog.cs:13-19` (`PredicateFaulted`), called from `src/TruthWeaver/Evaluation/FaultRecorder.cs`

`EvaluationLog.PredicateFaulted`'s `LoggerMessage` text is `"Predicate fault for term {Term}: {Message}"`. Ticket 51 (`51-collection-fault-not-recorded.md`, done) moved collection-level faults - a throwing collection selector, or `MaxElementEvaluations` exceeded - onto this same shared path (`FaultRecorder.Record`, `src/TruthWeaver/Evaluation/FaultRecorder.cs`) so they get a log entry and a metric count like a predicate fault does. Neither of those two cases is a predicate: the rendered log line now reads "Predicate fault for term 'lines': ..." for a collection, which is misleading even though the structured fields (`EventId`, `Term`, `Exception`) are correct and unaffected.

Surfaced by the code-review skill's Standards pass during ticket 51's implementation and deliberately deferred as out of that ticket's scope (it didn't introduce the wording; it just started reaching it from two new call sites).

### Failure scenario

A collection selector throws, or a quantifier exceeds `MaxElementEvaluations`, in production. The structured log fields are fine for querying, but anyone reading the rendered message text ("Predicate fault for term 'lines': ...") is told the wrong thing: `lines` is a collection, not a predicate.

### Expected behavior

The log message text (and, if it reads the same way, any rendered text elsewhere that assumes "fault" always means "predicate fault") is accurate for all three fault origins `FaultRecorder.Record` now serves: a predicate, a collection selector, and an element-evaluation limit.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after (e.g. asserting the rendered or logged text for a collection fault does not say "Predicate").
- `EventId`, `Term` and `Exception` stay exactly as they are; this ticket is wording-only, not a structured-field or `Fault`/`Decision` shape change.
- The validation list in CLAUDE.md passes.

## Comments

> *Filed from the 2026-10-26 retro on the Bundle D session (tickets 09, 12, 51): flagged by ticket 51's own code-review pass, deliberately left out of that ticket's scope.*

## Agent Brief

**Category:** bug
**Summary:** The shared fault-recording log message is hardcoded to say "Predicate fault," but it now also fires for a collection-selector fault and an element-evaluation-limit fault, neither of which is a predicate.

**Current behavior:** `EvaluationLog.PredicateFaulted` has one `LoggerMessage` template, `"Predicate fault for term {Term}: {Message}"`, used by every call into `FaultRecorder.Record` regardless of what faulted - a predicate (`Evaluator<TContext>`'s own call sites), a collection selector, or an element-evaluation-limit breach (both in `CollectionBinding<TContext, TItem>.EvaluateQuantifierAsync`). The rendered text says "Predicate" even for the latter two.

**Desired behavior:** The rendered message names the right kind of fault for each of the three origins, without changing `EventId`, the `Term` or `Exception` fields, or any other behavior ticket 51 already fixed (the fault still lands in `Decision.Faults` and the `truthweaver.faults` metric exactly as it does today).

**Key interfaces:**
- `FaultRecorder.Record` (or its caller) is the natural place to know which kind of fault it is, since `CollectionBinding`'s two call sites and `Evaluator<TContext>`'s predicate call sites are already distinct callers.
- Consider whether `EvaluationLog` needs one log method with a `kind`/origin parameter, or one method per origin (matching the existing per-event-id style of this file) - either is acceptable; pick whichever keeps `EventId` stable per origin and avoids a string-typed "kind" field in the structured log.

**Acceptance criteria:**
- [ ] A throwing collection selector's log line does not say "Predicate".
- [ ] An element-evaluation-limit-exceeded fault's log line does not say "Predicate".
- [ ] A predicate fault's log line is unchanged.
- [ ] `EventId`, `Term` and `Exception` are unaffected; `Decision.Faults` and the `truthweaver.faults` metric are unaffected (ticket 51's behavior is not touched).
- [ ] The validation list in `CLAUDE.md` passes.

**Out of scope:**
- Any change to which faults go through `FaultRecorder.Record`, or to `Decision.Faults`/metric behavior - ticket 51 already settled that.
