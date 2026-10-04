# Project skills

This table lists the source of every skill in this folder. The skill metadata (`SKILL.md` front matter) is the source of the license and version columns.

Classes:

- **Authored elsewhere, copy here**: the author is the repository owner. The canonical home is [`rheone/Booststraping-LLM-DEV-Container`](https://github.com/rheone/Booststraping-LLM-DEV-Container) (path `skills/<name>`). A change goes upstream first. `npx skills` updates the copy.
- **Vendored**: a third-party skill. `skills-lock.json` records it.

The Booststraping-LLM-DEV-Container repository is Apache-2.0. A copy here matches upstream except for line endings (LF here). `github-markdown` is also published in [`rheone/miscellaneous-agentic-tooling`](https://github.com/rheone/miscellaneous-agentic-tooling) (identical); Booststraping-LLM-DEV-Container is its source of record.

| Skill | Source | Class | License | Version |
| --- | --- | --- | --- | --- |
| `csharp-async` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-async` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-builder-pattern` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-builder-pattern` | Authored elsewhere, copy here | Apache-2.0 | 1.1.0 |
| `csharp-code-organization` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-code-organization` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-docs-and-comments` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-docs-and-comments` | Authored elsewhere, copy here | Apache-2.0 | 1.0.1 |
| `csharp-exception-handling` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-exception-handling` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-expression-trees` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-expression-trees` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-extension-members` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-extension-members` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-factory-pattern` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-factory-pattern` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-generics` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-generics` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-library-repo-structure` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-library-repo-structure` | Authored elsewhere, copy here | Apache-2.0 (not in the skill metadata) | 1.0.0 |
| `csharp-source-generators` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-source-generators` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-system-attributes` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-system-attributes` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `csharp-visitor-pattern` | `rheone/Booststraping-LLM-DEV-Container` `skills/csharp-visitor-pattern` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dispatch-tasks` | `rheone/Booststraping-LLM-DEV-Container` `skills/dispatch-tasks` | Authored elsewhere, copy here | Apache-2.0 | 2.0.0 |
| `dotnet-dependency-injection` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-dependency-injection` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dotnet-linq` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-linq` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dotnet-markdig` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-markdig` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dotnet-nsubstitute` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-nsubstitute` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dotnet-xunit` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-xunit` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `dotnet-yamldotnet` | `rheone/Booststraping-LLM-DEV-Container` `skills/dotnet-yamldotnet` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `github-markdown` | `rheone/Booststraping-LLM-DEV-Container` `skills/github-markdown` | Authored elsewhere, copy here | Apache-2.0 | 1.0.0 |
| `mermaid-diagram-generator` | `rheone/Booststraping-LLM-DEV-Container` `skills/mermaid-diagram-generator` | Authored elsewhere, copy here | Apache-2.0 | 2.0.0 |
| `humanizer` | [`blader/humanizer`](https://github.com/blader/humanizer) (repository root; plugin and CI files are not copied) | Vendored | MIT | 3.1.0 |

Notes:

- `csharp-library-repo-structure` has no `license` field upstream. The license column shows the license of its source repository. Add the field upstream, then sync the copy.
- Checked 2026-10-04: every skill directory was compared whole (all files, ignoring line endings) with its source. `csharp-builder-pattern` and `csharp-system-attributes` differed and were replaced with the upstream directories. All other skills are identical.
