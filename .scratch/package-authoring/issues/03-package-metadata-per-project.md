# 03: Complete package metadata for each packable project

**What to build:** For each packable project under `src/` set a `PackageId`, a one-sentence `Description` and `PackageTags` that fit that package, on top of the shared metadata in the root props. Check `Authors`, `Copyright`, license, repository, project URL and `PackageReleaseNotes` (point at `CHANGELOG.md`). No `PackageIcon` (no asset exists).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `dotnet pack -c Release` into a scratch directory produces every package with the metadata in its nuspec
- [x] The README, license expression and repository commit are in each package
- [x] Nothing is pushed
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
