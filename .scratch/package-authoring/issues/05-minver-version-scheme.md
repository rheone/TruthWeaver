# 05: MinVer version scheme

**What to build:** Replace the hand-edited `Version` (`1.0.0-dev`) with MinVer: tag prefix `v`, a default pre-release phase, configured once in the shared props, with the package version added to `Directory.Packages.props` and referenced as a global build-only package. Untagged builds get a pre-release version. Do not create a tag.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `dotnet pack` shows the MinVer-derived version and `InformationalVersion`
- [ ] CI checks out full history (or tags) so the version is correct
- [ ] `CHANGELOG.md` and `docs/contributing.md` describe how a version is made
- [ ] `dotnet restore --locked-mode` is satisfied and nothing is pushed or released
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
