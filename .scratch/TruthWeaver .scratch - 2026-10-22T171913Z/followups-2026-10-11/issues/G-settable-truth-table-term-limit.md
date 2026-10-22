# G: Settable truth-table term limit

**What to build:** `TruthTableOptions.HardTermLimit` is a constant 12 today, with `DefaultTermCap` 8. The owner decided (grilling 2026-10-18, Follow-up F, ticket L2) that the hard limit is settable by the caller with no ceiling. A table of n terms has 3^n rows in K3, so the caller owns the time and memory cost. Streaming to a `TextWriter` keeps memory flat.

**Blocked by:** None

**Status:** superseded by [01](../../whats-next-2026-10-18/issues/01-settable-truth-table-term-limit.md) and [02](../../whats-next-2026-10-18/issues/02-tool-core-export-uses-the-limit.md)

- [ ] The term limit is an option on `TruthTableOptions` with a default (open question for the implementer: keep `DefaultTermCap` 8 as the warning threshold and make the refusal limit settable, default 12). `TRE0034` and `TRE0035` keep their meaning against the configured values.
- [ ] A value below 1 is rejected.
- [ ] `ExportOptions.AllowLargerTruthTable` (Tool.Core, ticket C3) carries the number or maps to the option, and `TWT0015` reports the configured limit.
- [ ] The page that describes truth tables and `docs/tool-core.md` are updated to the current behavior, and `CHANGELOG.md` has an entry.
- [ ] Tests cover a raised limit, a lowered limit and the diagnostics.
