# 08: MathML, Typst and plain-ASCII equation dialects

**What to build:** Add three `EquationDialect` values: `MathML` (presentation MathML in a `<math>` element, escaped), `Typst` (Typst math markup, term text quoted and escaped), and `Ascii` (7-bit ASCII only: for example `/\`, `\/`, `~`, `(+)`, written as a documented table; every non-ASCII character in a term is replaced or escaped by a stated rule). All use the constant style from ticket 07. Document each dialect's escaping and its limits on `docs/equations.md`.

**Blocked by:** 06, 07

**Status:** done

- [x] Each dialect prints every operator, term, constant and the `SimpleVariables` legend, with tests
- [x] MathML output is well-formed XML (parse it in a test)
- [x] `Ascii` output contains only characters 0x20 to 0x7E and newline, for any rule, proven by a property test
- [x] `docs/equations.md` and `docs/examples.md` show a rule in each new dialect
- [x] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
