# 01: Settable truth-table term limit (core)

**What to build:** A caller of `PrintTruthTable` and `TryPrintTruthTable` sets the largest number of distinct terms a truth table may have. There is no ceiling: the caller owns the time and memory cost (a table of n terms has 3^n rows in K3). Writing to a `TextWriter` keeps memory flat. The default refusal limit stays 12 and the default warning threshold stays 8. Origin: Follow-up F, ticket L2, grilling 2026-10-18.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `TruthTableOptions` has a settable term limit. The warning threshold (`DefaultTermCap`, 8) and the refusal limit (default 12) behave as before when the caller sets nothing.
- [ ] `TRE0034` (cap raised, warning) and `TRE0035` (refused, error) report the configured values.
- [ ] A limit below 1 is rejected.
- [ ] The page that describes truth tables is updated to the current behavior, and `CHANGELOG.md` has an entry.
- [ ] Tests cover a raised limit, a lowered limit, the default and both diagnostics.
