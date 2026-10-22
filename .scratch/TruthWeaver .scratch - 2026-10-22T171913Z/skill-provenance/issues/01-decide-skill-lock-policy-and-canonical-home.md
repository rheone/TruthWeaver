# 01: Decide the skill lock policy and where each skill is canonical

**What to build:** A recorded owner decision on three questions, so the later tickets have one answer to implement. (1) Do the 19 skills authored by the owner live canonically in this repository, or in `rheone/miscellaneous-agentic-tooling` with this repository holding copies? (2) Does `skills-lock.json` cover only skills fetched from an outside source, or every skill? (3) Which tool, if any, refreshes the lock (`npx skills`)? Recommendation: this repository is canonical for the skills it uses; the lock covers only externally sourced skills; `npx skills` stays the lock tool if the owner uses it.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The three answers are written in this ticket under a Decision heading
- [ ] Each skill is classed as authored here, authored elsewhere with a copy here, or vendored from a third party
- [ ] The decision names which skills, if any, come from `miscellaneous-agentic-tooling`

Source: owner request, 2026-10-04 ("should there be a skills lock for all skills?").

## Findings

Checked 2026-10-04 against the owner's GitHub repositories by comparing each skill's `SKILL.md` blob hash (SKILL.md only; supporting files are not compared yet).

| Skill | Source repository | SKILL.md |
| --- | --- | --- |
| `github-markdown` | `rheone/miscellaneous-agentic-tooling` (also in the next repository) | identical in both |
| 20 authored skills (all `csharp-*`, `dotnet-*`, `dispatch-tasks`, `mermaid-diagram-generator` except as noted below) | `rheone/Booststraping-LLM-DEV-Container` (public, `skills/<name>`) | identical, except `csharp-builder-pattern` and `csharp-system-attributes`, which differ |
| `humanizer` | `blader/humanizer` (third party, MIT) | not in any owner repository |

- `miscellaneous-agentic-tooling` holds only six skills, and `github-markdown` is the only one this project uses. Most of this project's skills come from `Booststraping-LLM-DEV-Container`.
- The owner said `miscellaneous-agentic-tooling` is the source for many skills but not all; the table suggests the main source is `Booststraping-LLM-DEV-Container`. Confirm which repository the owner treats as canonical, and whether the two differing skills were edited here or upstream.

## Decision

Owner answers, 2026-10-04:

1. `rheone/Booststraping-LLM-DEV-Container` is the canonical home of the authored skills. This repository holds copies. `github-markdown` is also published in `miscellaneous-agentic-tooling`; the bootstrapping repository stays its source of record.
2. Where a copy here differs from the bootstrapping repository (`csharp-builder-pattern`, `csharp-system-attributes`), the bootstrapping repository is right. This repository's copy is brought back in line with it.
3. `npx skills` is used to update skills, so `skills-lock.json` stays and the tool refreshes it.

Consequences: every row in the provenance table (ticket 02) names `Booststraping-LLM-DEV-Container` as the source of an authored skill; ticket 02 also syncs the two differing skills; ticket 03 locks the externally sourced skills (`humanizer` for certain) and decides, from what the tool accepts, whether the authored skills (a repository the owner controls) are locked too.
