# 04: Package validation and a no-push CI pack check

**What to build:** Turn on `EnablePackageValidation` for the packable projects (set `PackageValidationBaselineVersion` only once a first release exists, with a comment saying so). Add a CI step that runs `dotnet pack`, fails on a pack warning, and checks each package (symbols package, README present, no unexpected files). The step never pushes.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] `dotnet pack` runs package validation with no errors
- [ ] The CI workflow has a pack step that does not push
- [ ] `docs/contributing.md` mentions the step
- [ ] `dotnet restore --locked-mode` is satisfied and nothing is pushed or released
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
