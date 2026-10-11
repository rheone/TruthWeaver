# 16: Validate and render every Mermaid diagram once

**What to build:** A one-off pass validates the syntax of and renders every Mermaid diagram in the reference, fixes any that fail, and records the result in the validation report. There is no permanent dependency. It needs the owner to authorize the Mermaid Chart connector first, which an agent cannot do.

**Blocked by:** The owner authorizing the Mermaid Chart connector in their claude.ai connector settings

**Status:** done

- [ ] The owner has authorized the connector (waived: the local validator was used instead)
- [x] Every Mermaid diagram in the reference renders, and any that did not are fixed and match their formulas
- [ ] The validation report records the result and marks its Mermaid limitation resolved (waived: the report no longer exists; the result is in the comment below)
- [x] The reference verification harness still passes (K3Reference tests, 33 of 33)

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).

## Comments

- 2026-10-09: Ran the local validator instead of the connector, at the owner's direction (the connector's tools were not available in the session). `validate-mermaid.mjs --mode parse` and `--mode render` (mermaid 12.0.0, headless Edge through `PUPPETEER_EXECUTABLE_PATH`) both passed all 10 Mermaid blocks in the 9 files of `docs/strong-k3/` that hold one: cardinality/README, derived/equivalent, derived/xor, functions/if, result-transformations/collapse, result-transformations/project, specification/evaluation, specification/operations, specification/values (2 blocks). No diagram failed, so none was changed. The diagram-versus-formula comparison was not repeated. The owner accepted the local run in place of the connector run, and closed the ticket. `docs/strong-k3/VALIDATION.md` no longer exists (removed by docs-reference-cleanup 02), so there is no report to update.
