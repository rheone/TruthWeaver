# 02: A date-time literal must carry Z or an offset

**What to build:** A rule author who writes a date-time literal with no offset, such as `"2026-01-01"` or `"2026-01-01T09:00"`, gets a compile error that says to add `Z` or an offset. A stored rule then means the same instant on every host. The same rule applies to a value that arrives from a data source.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Offset-less text is rejected in rule literals (DSL, JSON, YAML, builder) and in resolved variable values, with an error that names the fix
- [x] No parse depends on the host time zone: a test runs under two different `TimeZoneInfo.Local` values
- [x] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [x] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
