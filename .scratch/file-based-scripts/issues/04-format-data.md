# 04: format-data as a file-based app

**What to build:** `scripts/format-data.cs` runs the pinned Prettier over JSON, YAML and YML, and only parse-checks the fixture folders, as the `.ps1` does. `-CheckOnly` becomes `--check-only`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Same files formatted and same files parse-checked as the `.ps1`
- [ ] `--check-only` exits 1 when a file needs formatting
- [ ] A missing Node produces a clear message and a non-zero exit
- [ ] All build and format gates pass for the new file
