# 16b: Load a host registry from an assembly

**Group:** D tooling and integration

**What to build:** `--registry <assembly>` for the `truthweaver` tool: load a host assembly that exposes a predicate registry (through a documented contract, for example an attribute or a static factory method), so `validate`, `evaluate` and `test` run with the host's real predicates and schemas. Needs a plugin contract, an assembly load context, a trust warning (loading code runs it), and documentation that this mode is not AOT. Grill the owner on the contract and the security stance first.

**Blocked by:** 16

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work
- [ ] A host registry loads and its schemas are used for strict validation
- [ ] The trust warning and non-AOT note are documented in `docs/cli.md`
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
