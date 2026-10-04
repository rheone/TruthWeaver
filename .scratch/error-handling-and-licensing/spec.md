# Error handling, JSON Path licensing and API follow-ups

Status: ready-for-agent

Source: owner review of the data-sources and naming-cleanup chain, 2026-10-04.

## Problem Statement

1. `JsonPath.Net` 3.x ships under the Open Source Maintenance Fee EULA; 2.2.0 (MIT) is pinned but gets no updates.
2. Several public methods throw for failures that depend on external input (malformed JSON/YAML, a query that matches nothing, a validator rejection), where callers would prefer a `Try` or result form.
3. A few small decisions from the review are unrecorded: the `RuleOutline` name, the `EvaluateAsync` signature, the cancellation rule for data sources, and the vocabulary guard wording.

## Decisions

- The JSON Path dependency must be free to use at every tier, with no license, fee or EULA requirement. Research comes before any swap (ticket 01).
- `OutlineNode` is the whole outline; there is no `RuleOutline` type. Docs say "outline" for the concept.
- `EvaluateAsync` has one overload with trailing nullable/defaulted parameters.
- Cancellation is decided by the evaluation's own token: caller or timeout cancellation propagates; a source's own cancellation or timeout with a live token is a source-error fault.
- Rule for `Try` versus throw: failures driven by external input get a `Try` or result form; programmer errors (null arguments, duplicate registration, unsupported argument type, invalid enum values, internal "Unhandled X" defensive throws) keep throwing.
- Async members cannot use `out`, so they return a result type instead of a `Try` form.

## Tickets

See `issues/`. 01 is research; 05 and 06 wait on it. 07 waits on 06. 02, 03, 04, 08 and 09 are independent.
