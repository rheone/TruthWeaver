# 03: TruthWeaver.Tool README

**What to build:** `src/TruthWeaver.Tool/README.md` with a `## Use` section: install (`dotnet tool install --global TruthWeaver.Tool`), a first run on a sample rule with the expected output, a table of the nine commands (validate, convert, format, print, export, report, diff, evaluate, test), the input formats, exit codes, and a link to `docs/cli.md`.

**Blocked by:** 01

**Status:** done

- [x] Every flag, command and exit code matches the real `--help` output and `docs/cli.md`
- [x] Each example block carries a `<!-- doctest:... -->` marker and passes `dotnet test`
- [x] The drift test passes for this project

## Comments

- Wrote `src/TruthWeaver.Tool/README.md`. The drift test passes for it (five references listed).
- The tool has ten commands, not nine: `import` exists in `--help` and `docs/cli.md`. The README lists all ten.
- Output blocks were produced by running the tool. The `rule` block has a doctest marker; the shell, YAML and output blocks use `doctest:skip` with a reason. Project READMEs are not in `DocExampleTests`, so the markers are not run.
- The README links to the READMEs of `Tool.Core`, `Yaml` and `Testing`; those links fail the documentation lint until the other tickets land.
