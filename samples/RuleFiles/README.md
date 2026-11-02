# RuleFiles sample

A rule kept as a file, with the commands that check it, test it and show it. This sample shows `TruthWeaver.Tool` (the
`truthweaver` command) and `TruthWeaver.Tool.Tui` (the `truthweaver-tui` workbench). Neither tool needs a host
project. They compile a rule permissively and evaluate it with stub values.

## Files

| File | Holds |
| --- | --- |
| [publish.rule](publish.rule) | The rule, in rule text. A user can publish when the user is active and has the role `editor` or `admin`. |
| [publish.test.yaml](publish.test.yaml) | The rule test file. Each case gives a stub value to the terms and states the result that you expect. |

## Check the rule

Run the commands from this folder. `validate` checks the rule on every level. `format --check` fails when the file is
not in canonical form.

```shell
truthweaver validate publish.rule
truthweaver format publish.rule --check
```

## Test the rule

`test` runs each case of the test file and lists it as passed or failed. The exit code is 1 when a case fails.

```shell
truthweaver test publish.test.yaml
```

## View the rule

`print` writes one view of the rule to standard output. `convert` writes the same rule as JSON or YAML. The workbench
shows the rule next to its tree, its diagnostics and its decision, and lets you give each term a value.

```shell
truthweaver print publish.rule
truthweaver convert publish.rule --to yaml
truthweaver-tui publish.rule
```

## The test project

The [RuleFiles test project](../RuleFiles.Tests/) needs no terminal. It calls the same code that the commands call.
`RuleLoader` and `RuleValidator` from `TruthWeaver.Tool.Core` load and validate the rule file. `RuleTestFile` from
`TruthWeaver.Testing` runs the cases of the test file.

For the full list of commands, see [Command line](../../docs/cli.md) and [Terminal UI](../../docs/tui.md).
