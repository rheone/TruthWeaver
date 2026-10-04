# 01: Line endings and file-type config

**What to build:** Repository text files use LF by default, with best-practice exceptions per file type, and the three config files agree. `.gitattributes` becomes `* text=auto eol=lf` with the existing binary rules kept (extended with other binary artifact types as appropriate) and CRLF only for `*.bat`, `*.cmd` and `*.sln`. `.editorconfig` gets matching per-glob `end_of_line`, charset, `insert_final_newline`, `trim_trailing_whitespace` (off for Markdown, where trailing spaces are a line break) and indentation (4 spaces for C# and PowerShell, 2 for JSON, YAML, XML, props, targets and slnx). Read the existing `.editorconfig` first and preserve its C# style rules and analyzer severities. `.gitignore` gains the .NET and tool outputs that are missing (benchmark artifacts, test results, coverage output, IDE folders, user files, mutation-testing output). Renormalise the working tree so the files that sit as LF today are correct and a fresh checkout shows no pending changes.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `.gitattributes`, `.editorconfig` and `.gitignore` follow the file-type rules above, and each choice that is not obvious has a one-line comment saying why
- [ ] After renormalising, `git status` is clean and a fresh clone shows no line-ending diffs
- [ ] CSharpier and dotnet format agree with the new `end_of_line` (no churn on a format run)
- [ ] Generated and tool output directories are ignored and none is tracked
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
