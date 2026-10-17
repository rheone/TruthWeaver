# Documentation modernization

## Goal

Every user-facing surface and every code comment meets the repository documentation standard (`docs/agents/documentation-standard.md`). The result is complete public API documentation, current guides and reference pages, clear runtime text, and code comments that carry value. None of it cites behind-the-scenes records (ADRs, tickets, PRDs, `.scratch` paths) that may not outlive the repository. Where a reference pointed to a reason or behavior, the text states that reason or behavior.

## Decisions

Recorded in the planning session of 2026-10-17. The owner approved each one.

1. **Umbrella.** This folder sequences the effort in phases. `public-api-documentation` stays its own folder and is phase 2. `docs-reference-cleanup` is finished and is the model for the lint and baseline pattern.
2. **Start gate.** All phase tickets are `on-hold`. The owner explicitly triggers the effort and acknowledges that the major implementation is done. Phase 0 (guards) may land earlier only if the owner approves it, because it only blocks new violations.
3. **Surfaces.** Beyond the in-scope Markdown set: public API XML docs, internal code comments and test summaries, package metadata and package READMEs, and runtime text (CLI help, `TRE` diagnostic messages, exception messages, JSON schema descriptions). `CHANGELOG.md` is included and describes each change itself.
4. **Phases.** 0 Guards, 1 Inventory, 2 Public API docs and package metadata, 3 Reference and guide pages, 4 Runtime text, 5 Internal comments and test summaries, 6 Final.
5. **Skills by surface.** `humanizer` and `voice-of-robert` apply to every surface, including code comments. Pages also use `github-markdown`, and `mermaid-diagram-generator` where a diagram adds something. C# comments also use `gen-dotnet-docs-comments` and `csharp-docs-and-comments`.
6. **No renames.** No existing member is renamed. Follow best practices for code and naming in new text. A poorly named member or other non-documentation defect goes to `findings.md` for owner triage. Docs describe the member as it is.
7. **API doc gate.** At the end, a missing XML doc on a public member fails the build. No suppressions.
8. **Reference rule.** No ADR, ticket, PRD or `.scratch` reference in `src`, `tests` or in-scope documents. Exempt: `docs/adr/`, `.scratch/`, `docs/agents/`, `.agents/`, `CLAUDE.md`. A guard test enforces it, with a shrink-only baseline. `CLAUDE.md` states the rule.
9. **Verification.** Each phase ends with a fresh-reader review by a sub-agent that has not seen the work. The owner approves the phase before the next starts.
10. **Execution.** Run groups of tickets with `/dispatch-tasks`. Open questions are settled with grilling first.

## Phases and tickets

| Phase | Tickets |
| --- | --- |
| 0 Guards | 00 |
| 1 Inventory | 01 |
| 2 Public API docs and package metadata | 02 (links `../public-api-documentation/`) |
| 3 Reference and guide pages | 03 |
| 4 Runtime text | 04 |
| 5 Internal comments and test summaries | 05 to 09 |
| 6 Final | 10 |

Tickets 03 to 08 of `public-api-documentation` also carry the criterion that the package's XML docs and comments contain no behind-the-scenes reference.

## Out of scope

- Renaming or changing any member.
- Changing behavior, operators or the engine.
- Editing `docs/adr/` or `.scratch/` history.
