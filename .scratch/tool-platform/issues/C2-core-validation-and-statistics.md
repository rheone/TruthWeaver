# C2: Core: validation levels, statistics, diagnostics writers

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** A validation engine with these levels, each switchable and reported separately: syntax and compile diagnostics; JSON schema for the tree (JSON and YAML); limits (depth, size, definitions); strict schema file (predicate names, argument counts and kinds from the optional YAML schema file); analysis (tautology, contradiction, redundancy and the opt-in lints); conversion self-check (text, JSON and YAML give the same tree). Statistics: node count, depth, term count, distinct predicates, operator histogram, variables and data sources used, BDD size, tautology or contradiction status, truth-table size and whether it is within the cap. Diagnostics writers: text with spans, JSON and SARIF 2.1.0 (rule ids from the diagnostic code table, locations from spans). Severity model with exit-code mapping and a strict mode that fails on warnings.

**Blocked by:** C1

**Status:** ready-for-agent

- [ ] Each level has pass and fail tests; a level can be switched off
- [ ] Statistics are tested against hand-counted rules
- [ ] The SARIF output validates against the SARIF 2.1.0 schema in a test (schema file checked in under test support, no network)
- [ ] The diagnostics code table is the source of rule ids
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
