# 05: format-staged as a file-based app

**What to build:** `scripts/format-staged.cs` formats fully staged C# files (CSharpier, dotnet format, CSharpier again), re-stages them, and only checks partly staged files, failing the commit with the same message as the `.ps1`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] A fully staged file is formatted and re-staged without staging unstaged edits in other files
- [ ] A partly staged file is checked, not rewritten, and a formatting problem fails the run
- [ ] Non-ASCII paths work (`core.quotepath=off`)
- [ ] All build and format gates pass for the new file
