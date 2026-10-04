# Repo hygiene: line endings, file-type config and commit hook

Decided in the 2026-10-03 owner grilling session (Q17, Q13-14).

- Line endings: LF by default (`* text=auto eol=lf`), with best-practice exceptions per file type (CRLF only for `*.bat`, `*.cmd` and `*.sln`). `.editorconfig` mirrors this per glob and also sets charset, final newline, trailing whitespace and indentation. `.gitignore` gains the missing .NET and tool outputs.
- The commit hook fixes formatting (CSharpier and dotnet format) and re-stages instead of only checking. Build, test and Roslynator stay as checks because they cannot auto-fix.
- With `eol=lf` the line-endings guard (script, test, hook task and the CLAUDE.md note) is redundant and is retired.
- The vendored humanizer skill leaves the repo and lives at user level.

Tickets are in `issues/`.
