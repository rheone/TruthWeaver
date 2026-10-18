# TruthWeaver.Tool

The `truthweaver` command line: validate, convert, import, format, print, export, report, compare, evaluate and test rules.

## Purpose

The tool works on a rule file and needs no host application. It compiles every rule permissively, and it evaluates a rule
with stub values in place of the predicates. The commands are a thin front end over
[TruthWeaver.Tool.Core](../TruthWeaver.Tool.Core/README.md). The full reference is in [Command line](../../docs/cli.md).

## Use

Install the tool from NuGet. The command is `truthweaver`.

<!-- doctest:skip shell command, not a rule -->
```shell
dotnet tool install --global TruthWeaver.Tool
```

Put a rule in `rule.txt`:

<!-- doctest:rule readme-first-rule -->
```text
isActive AND NOT isBanned
```

Convert it to JSON:

<!-- doctest:skip shell command, not a rule -->
```shell
truthweaver convert rule.txt --to json
```

<!-- doctest:skip output of the command, not a rule -->
```json
{"op":"and","operands":[{"predicate":"isActive"},{"op":"not","operands":[{"predicate":"isBanned"}]}]}
```

Evaluate it with a stub-value file `stubs.yaml`, which gives each term a truth value:

<!-- doctest:skip stub values, not a rule -->
```yaml
isActive: True
isBanned: False
```

<!-- doctest:skip shell command, not a rule -->
```shell
truthweaver evaluate rule.txt --stubs stubs.yaml
```

<!-- doctest:skip output of the command, not a rule -->
```text
Result: True
```

A term that has no stub value is `Unknown`. Without `--stubs`, the result of this rule is `Unknown`.

### Commands

`truthweaver --help` lists the commands, and `truthweaver <command> --help` lists the options of a command.

| Command | Does |
| --- | --- |
| `validate` | Validates a rule in every level, and reports the statistics. |
| `convert` | Converts a rule to rule text, JSON or YAML. |
| `import` | Imports a JsonLogic rule as rule text, JSON or YAML. |
| `format` | Prints a rule in its own format, canonical or pretty, or checks that it is. |
| `print` | Prints one artifact of a rule, such as an equation, a diagram or a truth table. |
| `export` | Writes one artifact to a file, or every artifact to a folder with a manifest. |
| `report` | Writes the Markdown report of a rule. |
| `diff` | Compares two rules. |
| `evaluate` | Evaluates a rule with stub values and data files. |
| `test` | Runs the cases of a rule test file and lists each case as passed or failed. |

### Input formats

A command reads a file, or standard input when the argument is `-` or missing. The format is rule text, JSON or YAML.
`--from text|json|yaml` sets it. Without `--from`, the tool uses the file extension, then the content.

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | Success. |
| 1 | The command failed: an error in the rule, a warning with `validate --strict`, a `--check` that found a difference, or rules that are not equivalent in `diff`. |
| 2 | The tool could not do its work: a usage error, or a `TWT` error about the input, the configuration or an output file. |

Code 2 wins over code 1.

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `Decision`.
- [TruthWeaver](../TruthWeaver/README.md) to compile and evaluate the rules.
- [TruthWeaver.Yaml](../TruthWeaver.Yaml/README.md) to read and write YAML rules.
- [TruthWeaver.Tool.Core](../TruthWeaver.Tool.Core/README.md) for the logic that every command shares.
- [TruthWeaver.Testing](../TruthWeaver.Testing/README.md) for the stand-in predicates that give the stub values.

## Key types

| Type | Role |
| --- | --- |
| `TruthWeaverCli` | Builds the command tree and runs it. |
| `ValidateCommand`, `ConvertCommand`, `ImportCommand`, `FormatCommand`, `PrintCommand`, `ExportCommand`, `ReportCommand`, `DiffCommand`, `EvaluateCommand`, `TestCommand` | One class for each command. |
| `RuleInputOptions` | The input options that every command shares. |
