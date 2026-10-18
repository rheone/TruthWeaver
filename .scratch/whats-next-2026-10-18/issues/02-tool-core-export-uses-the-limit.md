# 02: Tool.Core export uses the configured limit

**What to build:** Export-all in Tool.Core honors the settable term limit from ticket 01. `ExportOptions.AllowLargerTruthTable` carries the number, or maps to the option, and the skip warning names the configured limit.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `ExportOptions` lets a front end set the truth-table term limit; the old Boolean behavior has a documented migration or is replaced.
- [ ] `TWT0015` (truth table skipped, warning) names the configured limit, and the manifest `skipped` entry stays.
- [ ] `docs/tool-core.md` is updated to the current behavior.
- [ ] Tests cover an export with a raised and a default limit.
