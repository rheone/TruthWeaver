# 10: `default(TruthValue)` is `Unknown`

**Parent:** [05](05-unknown-consistency-audit.md) D1 (owner decision 2026-10-17)

**Blocked by:** None

**Status:** ready-for-agent

**What to build:** Reorder `TruthValue` to `Unknown = 0, False = 1, True = 2` so an uninitialized field, a `default` array element or a failed-parse `out` value reads as `Unknown`. `NullBehavior` already did this (least-surprise ticket 08). Find every place that depends on the numeric value or on `default` being `False` (JSON/YAML/decision-record writers and readers, BDD code, arrays, `TruthValueText.TryParse` and `TryParseLetter` out values, sorting, `Enum` casts) and fix or test each. This breaks any persisted numeric value; state that in the ADR note and CHANGELOG.

- [ ] A test proves `default(TruthValue)` is `Unknown` and each place found above behaves as documented
- [ ] An ADR amendment (ADR-0002 or the ADR that defines `TruthValue`) records the reorder and the migration note
- [ ] Docs for the changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
