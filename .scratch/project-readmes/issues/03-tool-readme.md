# 03: TruthWeaver.Tool README

**What to build:** `src/TruthWeaver.Tool/README.md` with a `## Use` section: install (`dotnet tool install --global TruthWeaver.Tool`), a first run on a sample rule with the expected output, a table of the nine commands (validate, convert, format, print, export, report, diff, evaluate, test), the input formats, exit codes, and a link to `docs/cli.md`.

**Blocked by:** 01

**Status:** ready

- [ ] Every flag, command and exit code matches the real `--help` output and `docs/cli.md`
- [ ] Each example block carries a `<!-- doctest:... -->` marker and passes `dotnet test`
- [ ] The drift test passes for this project
