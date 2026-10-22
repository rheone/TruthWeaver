# 05: MinVer version scheme

**What to build:** Replace the hand-edited `Version` (`1.0.0-dev`) with MinVer: tag prefix `v`, a default pre-release phase, configured once in the shared props, with the package version added to `Directory.Packages.props` and referenced as a global build-only package. Untagged builds get a pre-release version. Do not create a tag.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `dotnet pack` shows the MinVer-derived version and `InformationalVersion`
- [x] CI checks out full history (or tags) so the version is correct
- [x] `CHANGELOG.md` and `docs/contributing.md` describe how a version is made
- [x] `dotnet restore --locked-mode` is satisfied and nothing is pushed or released
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
