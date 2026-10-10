# 14: A YAML null argument is rejected like JSON

**What to build:** An unquoted `null` or `~` as a rule argument is rejected with the diagnostic JSON gives, on every surface. An author who wants the text "null" quotes it. A failing test confirms today's behavior (the string "null") before the fix.

**Blocked by:** 03 (both edit the YAML reader)

**Status:** ready-for-agent

- [ ] Failing test first showing `null` and `~` become the string "null"
- [ ] The rule reader and the diagnostic text agree with JSON
- [ ] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [ ] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
