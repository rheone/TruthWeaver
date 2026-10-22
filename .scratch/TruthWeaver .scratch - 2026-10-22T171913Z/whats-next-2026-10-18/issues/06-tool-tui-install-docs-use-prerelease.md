# 06: Switch the Tool.Tui install docs to --prerelease

**What to build:** After the first Tool.Tui alpha is on NuGet, the install step in `docs/tui.md` and the Tool.Tui README is `dotnet tool install --global TruthWeaver.Tool.Tui --prerelease`. The pack-from-repo steps stay as the manual route.

**Blocked by:** Owner release steps: create the `nuget` environment and the `NUGET_USER` variable, add the NuGet.org trusted publishing policy, merge to main, and tag `v1.0.0-alpha.<UTC basic ISO 8601>`.

**Status:** needs-owner-action

- [ ] The alpha package appears on NuGet.org.
- [ ] `docs/tui.md` and `src/TruthWeaver.Tool.Tui/README.md` show the `--prerelease` install and keep the manual route.
- [ ] `ProjectReadmeTests` and the documentation tests pass.
