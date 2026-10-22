# 06: normalize-text-staged and format-data-staged as file-based apps

**What to build:** `scripts/normalize-text-staged.cs` and `scripts/format-data-staged.cs` pass the staged files to the apps from tickets 03 and 04, with the same whole-file and partial-file rule as the `.ps1` wrappers.

**Blocked by:** 03, 04

**Status:** done

- [x] Both apps handle fully staged, partly staged and no staged files as the `.ps1` wrappers do
- [x] They reuse the staged-file helper from ticket 01
- [x] All build and format gates pass for the new files
