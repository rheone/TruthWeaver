# 10: Empty the baselines and verify (phase 6)

**What to build:** The guard test and the documentation lint have empty baselines. A last fresh-reader pass crosses all surfaces (follow Getting started, find a diagnostic fix from the docs alone, find an API from the package README).

**Blocked by:** 05, 06, 07, 08, 09

**Status:** on-hold (do not start or dispatch until the owner triggers the effort)

- [ ] Both baselines are empty
- [ ] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info` and Roslynator pass with `CI=true`
- [ ] `findings.md` is handed to the owner for triage
- [ ] A fresh-reader sub-agent reviewed the phase output cold and its gaps are fixed or logged in findings.md
- [ ] The owner approved the phase
