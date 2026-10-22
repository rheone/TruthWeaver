# 03: Schema test for option defaults

**What to build:** A contributor who adds a predicate with an optional `ignoreCase` or `trim` argument that defaults to `true`, or with an `include*` flag that has no default, sees a failing test that names the predicate and the rule from the conventions page.

**Blocked by:** 01

**Status:** resolved

- [ ] A test walks every predicate schema in the built-in catalog and fails on an optional `ignoreCase` or `trim` argument whose default is not `false`
- [ ] The test fails on an optional Boolean `include*` argument that has no default. The `nullBehavior` default is a factory parameter, not a schema field, so a reflection check of the factory methods is added only if it stays small
- [ ] The failure message names the predicate, the argument and the convention, and links the conventions page
- [ ] The test passes on the current catalog after ticket 01
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
