# 20: Move the examples with their doctests

**What to build:** A reader finds the seven worked examples and the bonus example on `docs/examples.md`. The `worked` chain (rule text, JSON, YAML, `RuleBuilder` tree and diagram) moves as one unit with examples 1 to 7 and keeps its marker order, so each `rule` block precedes the blocks that use its ID.

**Blocked by:** 19

**Status:** done

- [x] `docs/examples.md` follows the standard and is not on the baseline
- [x] The page is added to the doctest list and to `docs/doc-examples.md`, and every one of its markers passes
- [x] No `text`, `json`, `yaml`, `mermaid` or `ebnf` block in the README or the new page is untagged
- [x] The README section is replaced by a link, and its table of contents matches
- [x] No link is broken, and `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

- 2026-10-04: Moved the README Examples section (examples 1 to 7 and the bonus) to `docs/examples.md` with all nine rule markers, the worked chain and their order. Registered the page in `DocExampleTests` and `docs/doc-examples.md`. Redirected the links in `docs/predicates.md` and `docs/rulebuilder.md`. The README keeps a short Examples entry. The page is not on the lint baseline.
