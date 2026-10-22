# 11: One diagnostic code per mistake, and one code table

**What to build:** The same authoring mistake gets the same code on every surface: wrong operand count, `Collapse` and `Project` used as operators, a bad threshold bound, an unknown operator. `TRE0014` is kept for structural tree shape only. Codes are not renumbered. One table lists every code with its severity, phase and whether it is on by default or opt-in, linked from `docs/diagnostics.md`.

**Blocked by:** 01, 03 (all touch operand-count and tree diagnostics)

**Status:** resolved

- [x] Tests assert the same code for each mistake in the DSL, JSON, YAML and builder
- [x] The code table is complete: a test fails when a code in `DiagnosticCodes` is missing from it
- [x] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [x] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
