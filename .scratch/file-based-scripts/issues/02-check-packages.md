# 02: check-packages as a file-based app

**What to build:** `scripts/check-packages.cs` checks the `.nupkg` and `.snupkg` files that `dotnet pack` wrote, with the same expected package set, required entries and failure messages as the `.ps1`. The package directory is a required argument.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Passes on real `dotnet pack` output and exits 1 with the same messages on a package set with a missing README, a missing symbol package and an unexpected file
- [ ] Uses `System.IO.Compression` only (no new dependency)
- [ ] All build and format gates pass for the new file
