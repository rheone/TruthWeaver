# 02: K3 foundation pages

**What to build:** A reader opens `docs/strong-k3/README.md` and finds a reference that explains values, semantics, the full list of Operations and the K3 vocabulary without any project history. `specification/operations.md` is the inventory of 27 Operations (category, kind, arity, canonical form, connective or external) with the operation-map Mermaid diagram. `values.md`, `semantics.md`, `notation.md` and `terminology.md` are rewritten in present tense, with no ADR, ticket or owner references. `terminology.md` becomes the K3 glossary and holds K3-specific concepts only, including the classification terms and the Gate versus Operator definition. The K3 root README lists a reading order and explains how an operation page is laid out. `PROPOSAL.md` is deleted. The checker's inventory messages and `K3Operation` remarks cite `operations.md` instead of `PROPOSAL.md`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `operations.md` holds all 27 Operations, and the checker still verifies that every document matches the inventory
- [ ] The operation map diagram shows the 7 primitives and what derives from each, and its syntax is valid
- [ ] The five foundation pages and the K3 README contain no history, ticket, ADR or owner references, and link only inside `docs/strong-k3/`
- [ ] `PROPOSAL.md` is removed and no file links to it
- [ ] The baseline list in the lint test loses the pages this ticket cleans
- [ ] The reference harness (`K3ReferenceTests`) and the lint test pass
