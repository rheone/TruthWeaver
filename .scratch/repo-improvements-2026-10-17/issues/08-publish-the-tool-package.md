# 08: Publish TruthWeaver.Tool with the other packages

**What to build:** The owner decided that a release publishes all nine packages, including the `truthweaver` .NET tool. Remove the "built and checked but not published" rule from: `src/TruthWeaver.Tool/TruthWeaver.Tool.csproj`, the `scripts/check-packages.ps1` header, the CI `pack` job, `docs/releasing.md` (the step that deletes the Tool package before the push), `docs/cli.md` and `docs/packages.md`. Then verify the package is ready: `dotnet tool install --add-source` from the packed output works in a clean directory, `truthweaver --help` runs, the package README shows on NuGet (`PackageReadmeFile`), `PackageTags` include `dotnet-tool`, and the tool runs on `net11.0`. State in `docs/cli.md` that the CLI surface is under SemVer once published. Confirm the package validation setting is correct for a tool.

**Blocked by:** None

**Status:** ready-for-agent

- [ ] No page or comment says the Tool package is not published
- [ ] A clean-directory install test of the packed tool passes and is recorded
- [ ] `docs/cli.md`, `docs/packages.md` and `docs/releasing.md` describe the current behavior
- [ ] The full validation set from `CLAUDE.md` passes
