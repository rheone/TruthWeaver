# 01: Bump the two behind packages; resolve the missing dictionary.dic

**What to build:** Move `Microsoft.NET.Test.Sdk` to 18.11.0 and `Microsoft.VisualStudio.Threading.Analyzers` to 18.12.1 in `Directory.Packages.props`. Re-run `dotnet list TruthWeaver.slnx package --outdated` and confirm nothing else is behind (stable only; the documented RC and beta pins stay). Fix any warning the update causes at its cause. `Directory.Build.props` includes `dictionary.dic` as an `AdditionalFiles` item, but no such file exists in the repo. Find out why the build passes, then either add the file with a stated purpose or remove the item.

**Blocked by:** None

**Status:** done

- [ ] Both packages are updated; the outdated report shows no stable update left
- [ ] `dictionary.dic` is added with a purpose or its `AdditionalFiles` item is removed, with the reason stated in the commit
- [ ] Lock files are regenerated if they exist
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
