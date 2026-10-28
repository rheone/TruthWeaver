# VS Code rule-text extension: QOL and generator

**Status:** ready-for-agent (grilled 2026-10-24; ticket 02 needs-triage, ticket 03 wontfix)

Source: a `/grill-with-docs` session scoping follow-up quality-of-life and value-add work for `editors/vscode/`, the unpublished, code-free TextMate grammar extension for rule text. Four candidates were raised (grammar/manifest QOL, a generator script, devcontainer auto-register, a rule-text language server); this spec records what ships now versus what's deferred.

## Problem

`editors/vscode/` colors rule text but has no generator keeping its operator lists in sync with the language, supports only one file extension, is missing a small editing convenience (comment continuation), and has no path yet to a richer in-editor experience (live diagnostics, hover docs). This spec scopes the first slice and parks the rest with their research intact.

## Decisions

| # | Question | Decision | Why |
| --- | --- | --- | --- |
| 1 | How to organize the four streams | Bundle grammar QOL + generator as one change; split out devcontainer auto-register and the language server | The language server is a real scope/architecture change (code, build, maintenance); the other three stay within the extension's current "no code, no dependency" design |
| 2 | `.tw` file extension | Add it now, alongside `.truthweaver` | Free to add alongside the other manifest edits in this pass |
| 3 | Deprecated-spelling styling | Dropped — no target exists | Per ADR-0005, none of the grammar's current words (`XOR`, `EQUIVALENT`, `IFF`, `XNOR`, `IMPLIES`, `NAND`, `NOR`) are deprecated; `IFF`/`XNOR` are still-valid aliases of `EQUIVALENT`. The only dead spelling, `NXOR`, isn't in the grammar's word list today |
| 4 | File icon | Skip | Cosmetic, needs an art asset that doesn't exist, nothing else depends on it |
| 5 | Folding/indentation rules | `onEnterRules` for `//` comment continuation only | Bracket folding already works by default; full indentation/folding rules are more than this pass needs |
| 6 | Generator source and enforcement | Parse `docs/strong-k3/specification/syntax.md`'s Spellings table; enforce as a `dotnet test` drift check, not a manual script | `OperatorDefinitions` is `internal` and unreachable from a standalone script; the Spellings table is already the grammar's documented source of truth (`editors/vscode/README.md`). CI enforcement matches the project's existing no-manual-sync philosophy |
| 7 | Snippets | Dropped from this bundle | Needs hand-written placeholder bodies per operator, a distinct effort from the generator; revisit separately if wanted |
| 8 | Devcontainer auto-register | Deferred (ticket 02, on hold) with findings recorded | A race condition between `postCreateCommand`/`postStartCommand` and `extensions.json` population was found during research; needs a deliberate mechanism choice, not a quick add |
| — | Language server / live diagnostics | Deferred (ticket 03, on hold), needs its own design pass | Crosses from "no dependency" into a real extension with a build and ongoing maintenance; too large to settle in this session |

## Tickets

| Ticket | Change | Status | Blocked by |
| --- | --- | --- | --- |
| [01](issues/01-grammar-manifest-qol-and-generator.md) | `.tw` extension, comment-continuation rule, syntax.md-driven generator with CI drift check | ready-for-agent | None |
| [02](issues/02-devcontainer-auto-register.md) | Auto-install the extension into the devcontainer's VS Code Server | on-hold | None |
| [03](issues/03-rule-text-language-server.md) | Rule-text language server / live diagnostics | superseded by [`rule-text-language-server`](../rule-text-language-server/spec.md) | None |

## Out of scope

- Icon contribution.
- Deprecated-spelling styling (nothing currently deprecated).
- Snippets.
- Reading `OperatorDefinitions` directly from a build script (it is `internal`; use the public Spellings table instead).
