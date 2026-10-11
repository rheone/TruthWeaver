# 02: Serializable Decision record

**Group:** A understand a decision

**What to build:** A `Decision` (value, faults, trace, evaluation options summary) can be written as JSON for audit logs and read back for replay. Include a rule identity (a hash of the canonical rule text, plus a caller-supplied name and version string) so a stored decision says which rule produced it. Redaction: argument values and data-source values are omitted unless the caller opts in (the trace already redacts variable values; keep that behavior). AOT-clean (no reflection serialization). Add a `TextWriter` overload per the printer convention and a `Try…` reader.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Round trip: write then read gives an equal `Decision` (using `EquatableArray` equality)
- [ ] Redaction defaults are tested; an opt-in includes values
- [ ] The JSON shape is documented and has a schema under `src/TruthWeaver/Json`
- [ ] Trim and AOT analyzers stay clean
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
