# 07: format-all as a file-based app

**What to build:** `scripts/format-all.cs` formats the repo and runs the CI gates in the same order as the `.ps1`, stopping at the first failure. `-CheckOnly` becomes `--check-only`. It calls the file-based apps from tickets 03 and 04, not `pwsh`.

**Blocked by:** 03, 04

**Status:** ready-for-agent

- [ ] Order and commands match the `.ps1` and `.github/workflows/ci.yml`
- [ ] `--check-only` skips the apply step
- [ ] A failing step exits 1 with the step name in the message
- [ ] All build and format gates pass for the new file
