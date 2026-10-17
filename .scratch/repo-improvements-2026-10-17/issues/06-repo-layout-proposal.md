# 06: Repository layout proposal (no moves)

**What to build:** A proposal document, not a change. Record the current layout and a recommended one. Facts from the audit: tool configs at the root (`stylecop.json`, `stryker-config.json`, `skills-lock.json`, `opencode.jsonc`, `.csharpierrc.json`, `.prettierrc.json`, `.prettierignore`), `scripts/` beside `samples/` and `benchmarks/`, 560 tracked `.scratch` files, 333 tracked `.claude` files, and tool folders `.codex` and `.cursor` that are git-ignored. For every candidate move state: the target path, the files and settings that break (CI paths, Husky tasks, the docs-lint scope, doc links, the `DocumentationLint` baseline, `AdditionalFiles` paths, tool discovery of root configs) and the risk. Decide whether `.scratch` history should be archived or pruned, and whether `.claude` content belongs in a separate repo. Mark which configs must stay at the root because tools discover them there.

**Blocked by:** None

**Status:** ready-for-agent

- [ ] The proposal has a before/after tree and a cost table per move
- [ ] The owner approves or declines each move; each approved move becomes its own ticket
- [ ] No file is moved in this ticket
