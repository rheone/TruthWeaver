# 16: Validate and render every Mermaid diagram once

**What to build:** A one-off pass validates the syntax of and renders every Mermaid diagram in the reference, fixes any that fail, and records the result in the validation report. There is no permanent dependency. It needs the owner to authorize the Mermaid Chart connector first, which an agent cannot do.

**Blocked by:** The owner authorizing the Mermaid Chart connector in their claude.ai connector settings

**Status:** needs-owner-action

- [ ] The owner has authorized the connector
- [ ] Every Mermaid diagram in the reference renders, and any that did not are fixed and match their formulas
- [ ] The validation report records the result and marks its Mermaid limitation resolved
- [ ] The reference verification harness still passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
