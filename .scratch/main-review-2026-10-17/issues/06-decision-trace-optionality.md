# 06: Decision.Trace is documented as optional but always set

**Parent:** [main review](../spec.md)

**Status:** needs-owner-decision

**What to build:** Decide: add the EvaluationOptions flag the ADR and docs promise, or change the docs and the nullable shape to match the always-on behavior.

**Evidence:** ADR-0001 and Decision.cs:15-17 say Trace is present only when requested; EvaluationOptions has no such flag and Evaluator.cs:56-61 always populates it. Removing the nullability is a breaking API change.

- [ ] Docs, type shape and behavior agree
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
