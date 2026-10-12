# Command line

`truthweaver` is the command line of TruthWeaver. It validates, converts, formats, prints, exports, reports, compares and evaluates rules in rule text, JSON or YAML. Back to the [README](../README.md).

The commands are a thin front end over the [tool core](tool-core.md), so the command line and the terminal UI behave the same way. The tool has no host registry: it compiles every rule permissively, and it evaluates a rule with stub values in place of the predicates.

## Install

The package `TruthWeaver.Tool` is a .NET tool. It is not on NuGet. Pack it from the repository and install it from the folder:

```shell
dotnet pack src/TruthWeaver.Tool --configuration Release --output ./packages
dotnet tool install --global TruthWeaver.Tool --add-source ./packages --prerelease
```

The command is `truthweaver`. `truthweaver --help` lists the commands, and `truthweaver <command> --help` lists the options of a command.

## Commands

| Command | Does |
| --- | --- |
| [`validate`](#validate) | Validates a rule in every level, and reports the statistics. |
| [`convert`](#convert) | Converts a rule to rule text, JSON or YAML. |
| [`format`](#format) | Prints a rule in its own format, canonical or pretty, or checks that it is. |
| [`print`](#print) | Prints one artifact of a rule on standard output. |
| [`export`](#export) | Writes one artifact to a file, or every artifact to a folder with a manifest. |
| [`report`](#report) | Writes the Markdown report of a rule. |
| [`diff`](#diff) | Compares two rules. |
| [`evaluate`](#evaluate) | Evaluates a rule with stub values and data files. |

## Input

Each command reads one rule. The argument is a file path, or `-` for standard input. When you leave out the argument, the command reads standard input. `diff` reads two rules, and only one of them can be `-`.

These options apply to every command:

| Option | Meaning |
| --- | --- |
| `--from <format>` | The format of the input: `text`, `json` or `yaml`. The default finds the format from the file extension, then from the content. See [Format detection](tool-core.md#format-detection). |
| `--path <path>` | Selects one rule inside a JSON or YAML document, such as `rules.publish` or `$['rules'][0]`. See [Embedded rules](tool-core.md#embedded-rules). |
| `--config <file>` | The configuration file. See [Configuration](#configuration). |
| `--operator-style <style>` | The operator style of rule text: `word`, `symbolic` or `cstyle`. |
| `--constant-style <style>` | The style of the constants in equations: `word`, `letters` or `logicsymbols`. |
| `--dialect <dialect>` | The equation dialect: `unicode`, `latex`, `asciimath`, `cstyle`, `mathml`, `typst` or `ascii`. |
| `--max-depth <n>`, `--max-node-count <n>`, `--max-analysis-terms <n>` | The limits of the compiler and the analysis, each a whole number from 1. |
| `--schema-file <file>` | The [predicate schema file](tool-core.md#predicate-schema-file). |
| `--json` | Writes one JSON object. See [Output](#output). |

Option values ignore case. A path is relative to the current directory.

## Configuration

`truthweaver.json` holds the defaults. Without `--config`, the command uses the nearest `truthweaver.json` in the directory of the input or in a parent directory. For standard input, the search starts in the current directory. [Configuration file](tool-core.md#configuration-file) describes the members.

A value on the command line wins over the configuration file, and the configuration file wins over the default. A configuration error is a `TWT0007` error with exit code 2. An unknown property is a `TWT0008` warning.

## Output

The output is UTF-8 with no byte order mark and LF line endings, and it ends with one line feed. The same input gives the same bytes on every platform.

| Mode | Standard output | Standard error |
| --- | --- | --- |
| Text (the default) | The result of the command. | The diagnostics, as [Reading diagnostics](diagnostics.md) shows them. |
| `--json` | One JSON object, the envelope. | Nothing. |
| `validate --sarif` | A SARIF 2.1.0 log. | Nothing. |

The envelope has these members:

| Member | Content |
| --- | --- |
| `command` | The command name, such as `convert`. |
| `exitCode` | The exit code of the run. |
| `diagnostics` | The diagnostics of the run, each one as [Diagnostics output](tool-core.md#diagnostics-output) describes the JSON format. For `validate`, the diagnostics of the rule are in the report. |
| `result` | The result of the command. Each command section names its members. |

<!-- doctest:skip an envelope, not a rule -->
```json
{
  "command": "convert",
  "exitCode": 0,
  "diagnostics": [],
  "result": {
    "format": "yaml",
    "text": "predicate: isActive\n...\n"
  }
}
```

### Output files and checks

`convert`, `format`, `report` and `export` take these options:

| Option | Meaning |
| --- | --- |
| `--output <file>`, `-o <file>` | Writes the output to the file, and creates its directory. Nothing goes to standard output. |
| `--check` | Writes nothing. Compares the output with the `--output` file byte for byte. A file that differs or is missing fails the command with exit code 1. |

The text output of a check is one line: the file and `identical`, `missing` or `differs at line N`. The JSON result has `check` with `path`, `status` and `firstDifferentLine`. `--check` needs `--output`, except for `format` and for `export --all`.

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Success. |
| 1 | The command failed: an error in the rule, a warning with `validate --strict`, a `--check` that found a difference, or `diff` found that the rules are not equivalent. |
| 2 | The tool could not do its work: a usage error (an unknown option, a missing value, options that cannot go together), or a `TWT` error about the input, the configuration, the stub values, the data files, the schema file, an output file, an artifact id or an image renderer. |

Code 2 wins over code 1. [Exit codes](tool-core.md#exit-codes) lists the `TWT` codes that give code 2.

## validate

`truthweaver validate [input]` runs every [validation level](tool-core.md#levels) and reports the status of each level, the statistics and the diagnostics.

| Option | Meaning |
| --- | --- |
| `--strict` | A warning fails the command too. |
| `--skip <level>` | Switches a level off: `compile`, `tree-schema`, `limits`, `predicate-schema`, `analysis` or `conversion-check`. Repeat it for more levels. |
| `--style-lint` | Adds the style lint to the analysis level. |
| `--sarif` | Writes the diagnostics as a SARIF 2.1.0 log. It cannot go with `--json`. |

The JSON result has `report`: the JSON validation report of [Diagnostics output](tool-core.md#diagnostics-output).

## convert

`truthweaver convert [input] --to <format>` converts a rule to `text`, `json` or `yaml`. The result compiles to the same tree with the same notes. Rule text uses the operator style of the settings.

| Option | Meaning |
| --- | --- |
| `--to <format>` | The format to convert to. It is required. |
| `--no-notes` | Leaves out the notes. |

The JSON result has `format`, and `text` or `output`.

## format

`truthweaver format [input]` prints a rule in its own format. The canonical form of rule text and JSON is one line. YAML is one node per line.

| Option | Meaning |
| --- | --- |
| `--pretty` | Breaks rule text at 80 characters and indents JSON by two spaces. |

`format --check` without `--output` compares the output with the input. A whole file is compared byte for byte. Standard input and an embedded rule are compared as text. The command never changes the input file.

The JSON result has `format`, and `text`, `output` or `check`.

## print

`truthweaver print [input]` writes one artifact of the rule to standard output.

| Option | Meaning |
| --- | --- |
| `--artifact <id>`, `-a <id>` | The artifact. The default is `rule-text`. `equation` is the equation in the `--dialect` and `--constant-style` of the settings (`letters` by default). [Artifacts and export](tool-core.md#artifacts-and-export) lists every id. |
| `--allow-large-truth-table` | Prints a truth table with more than 8 terms, up to 12, with a `TRE0034` warning. |

An id that is not in the catalog is a `TWT0018` error. The JSON result has `artifact` (the catalog id) and `text`.

## export

`truthweaver export [input]` writes artifacts to files. It takes `--artifact` and `--allow-large-truth-table` as `print` does.

| Form | Writes |
| --- | --- |
| `export [input] --artifact <id> --output <file>` | One artifact. |
| `export [input] --all <folder>` | Every artifact, one file each, and `manifest.json`. [Export of every artifact](tool-core.md#export-of-every-artifact) describes the folder. |

| Option | Meaning |
| --- | --- |
| `--render svg\|png` | Renders the diagram as an image with Graphviz `dot` or the Mermaid CLI `mmdc` from the `PATH`. With `--artifact`, the artifact must be `mermaid` or `dot`, and `--output` is the image file. With `--all`, the folder also gets `mermaid.svg` and `dot.svg` (or `.png`). A missing program is a `TWT0016` error. See [Images](tool-core.md#images). |
| `--check` | Compares the files with the export and writes nothing. With `--all`, it checks every file and the manifest. It cannot go with `--render`. |

The JSON result has `artifact` and `output` for one artifact, or `folder` and `manifest` for `--all`. A check has `check`, or `checks` (one item for each file) for `--all`.

## report

`truthweaver report [input]` writes the [Markdown report](tool-core.md#report): the rule, the equation, a diagram, the truth table, the statistics and the analysis.

| Option | Meaning |
| --- | --- |
| `--allow-large-truth-table` | Puts a truth table with more than 8 terms, up to 12, in the report, with a `TRE0034` warning. |

The JSON result has `text` or `output`, and `check` for `--check`.

## diff

`truthweaver diff <before> <after>` compares two rules. The rules can be in different formats.

The text output has three parts:

1. The structural changes, one line each, or `No changes.`.
2. `Strong K3 equivalence`: `equivalent`, `not equivalent` or `undecided`. Two rules are equivalent when they give the same value for every assignment of `True`, `False` and `Unknown` to the terms.
3. `Relation`: `equivalent`, `looser`, `tighter`, `overlapping`, `disjoint` or `undecided`. It compares the assignments that make each rule `True`. An example assignment follows for each side that only one rule accepts.

The exit code is 0 only when the rules are equivalent. When the rules are not equivalent, or the check is undecided because the rules have more distinct terms than `--max-analysis-terms`, the exit code is 1.

The JSON result has `equivalent` (`true`, `false` or `null`), `relation`, `changes` (each with `kind`, `path`, `before` and `after`), `satisfiesOnlyBefore`, `satisfiesOnlyAfter` and `reason`.

## evaluate

`truthweaver evaluate [input]` evaluates a rule and prints the result and the faults.

| Option | Meaning |
| --- | --- |
| `--stubs <file>` | The [stub-value file](tool-core.md#stub-values): a truth value for each term. A term with no value is `Unknown`. |
| `--data <name>=<file>` | A JSON or YAML data file for the `from("name", "query")` references. Repeat it for more sources. |
| `--exhaustive` | Evaluates every term, with no short-circuiting. |
| `--fault-budget <n>` | Stops the evaluation when `n` faults are recorded. |
| `--show-values` | Shows the value of each `from(...)` reference in the trace. |
| `--trace` | Prints the trace tree after the result. |

Each predicate has a stand-in that gives the stub value of its term. A term with a `from(...)` argument takes the stub value of the term with the value that the data file gives. For example, with `{"region": "EU"}` as `user`, the term `inRegion(region: from("user", "$.region"))` takes the stub value of `inRegion(region: "EU")`. A reference that does not resolve is a fault, and its term is `Unknown`. [Stub evaluation](tool-core.md#stub-evaluation) describes the rules.

The exit code is 0 when the rule was evaluated, whatever the result is. The JSON result has `result` and `decision`: the [decision record](decision-records.md) with the literal argument values.

## Shell completion

The command line answers the `[suggest]` directive of `System.CommandLine`, which completes the command names, the options, the enumerated values and the artifact ids. To use it in a shell, install the `dotnet-suggest` global tool and add its shell script to your shell profile, as the [System.CommandLine](https://github.com/dotnet/command-line-api) repository describes. Then register the command:

```shell
dotnet tool install --global dotnet-suggest
dotnet-suggest register --command-path "$(which truthweaver)"
```
