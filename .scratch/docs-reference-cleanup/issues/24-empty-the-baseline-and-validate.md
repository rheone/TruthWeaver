# 24: Empty the lint baseline and run the full validation list

**What to build:** Every in-scope file meets the standard and `DocumentationLintBaseline` is empty. The full validation list from `CLAUDE.md` passes.

**Blocked by:** 14, 15, 22, 23

**Status:** done

- [x] `DocumentationLintBaseline.Files` is empty, and a comment states that the list may only shrink
- [x] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and `dotnet roslynator analyze` pass with `CI=true`
- [x] Line endings in every touched file are CRLF

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: The baseline is empty and its comment now says the list may only shrink. The full validation list passes with CI=true (2545 tests), the touched files are CRLF, and the final sweep found no link leaving docs/strong-k3 and no history words outside code.
