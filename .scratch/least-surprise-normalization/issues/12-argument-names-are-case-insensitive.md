# 12: Argument names are case-insensitive

**What to build:** A rule author who writes `hasCrust(Crust: "thin")` matches the schema argument `crust`, as predicate and operator names already match ignoring case. Canonical text keeps the schema's spelling. Two arguments of one schema that differ only in case are a registration error. The predicate conventions page states that `lower`/`upper` bound a value, `min`/`max` bound an operand count, and `start`/`end` bound a time of day.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Failing tests first on every surface: DSL, JSON, YAML, builder
- [ ] A schema with two case-variant argument names fails at registration with a clear message
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
