# Rule tests

A rule test file holds one rule and named cases. Each case gives a value to the terms of the rule, and states the result that you expect. You write the file in YAML, with no C#. The `truthweaver test` command runs it, and a C# test can run the same file with `RuleTestFile` in the `TruthWeaver.Testing` package.

The rule runs over stand-in predicates. A stand-in gives the stub value of its term from the case, so no host and no predicate code is necessary.

## A test file

<!-- doctest:skip a rule test file, not a rule; RuleTestDocumentationTests runs it -->
```yaml
name: access
rule: |
  isActive AND hasRole(role: "editor")
cases:
  - name: an active editor is let in
    given:
      isActive: True
      'hasRole(role: "editor")': True
    expect:
      result: True
  - name: an inactive user is kept out
    given:
      isActive: False
    expect:
      result: False
      faults: 0
  - name: an unknown role is undecided
    given:
      isActive: True
    expect:
      result: Unknown
```

The third case gives no value to `hasRole(role: "editor")`, so that term is `Unknown`, and `True AND Unknown` is `Unknown`.

## Keys of the file

| Key | Required | Value |
| --- | --- | --- |
| `name` | No | The name of the file. A JUnit report uses it as the suite name. The default is the file name without its extension. |
| `rule` | One of `rule` and `ruleFile` | The rule inline: rule text, or a YAML rule tree as [Rule formats](rule-formats.md) describes it. |
| `ruleFile` | One of `rule` and `ruleFile` | The path of the rule file, relative to the test file. A `.json` file is JSON, a `.yaml` or `.yml` file is YAML, and a file with any other extension is rule text. |
| `cases` | Yes | A list of one or more cases. The cases run in the order of the file. |

Rule text with a `name: value` argument is not a plain YAML value, because `: ` starts a mapping. Write it as a block (`rule: |` and the rule text on the next line), or in quotes.

## Keys of a case

| Key | Required | Value |
| --- | --- | --- |
| `name` | Yes | The name of the case. Each name is used once in a file. |
| `given` | No | The stub value of each term: a term in rule text, such as `hasRole(role: "editor")`, and `True`, `False` or `Unknown`. A term with no value is `Unknown`. |
| `variables` | No | The data for each `from("source", "query")` source name, inline in the file. |
| `variableFiles` | No | A JSON or YAML data file for each source name, relative to the test file. A `.json` file is JSON, and a file with any other extension is YAML. |
| `expect` | Yes | What the case expects. See [Expectations](#expectations). |

A truth value is `True`, `False` or `Unknown`, or `T`, `F` or `U`, in any case. The YAML Booleans `true` and `false` are `True` and `False`.

A term key is the term as rule text writes it. Spaces do not change the key, so `hasRole( role:"editor" )` is the same term as `hasRole(role: "editor")`. The names of predicates and arguments must have the same spelling as in the rule. A given term that the rule does not use fails the case: a predicate that the rule does not use, or a predicate with arguments that no term of the rule has. The message names the spelling of the rule when only the letter case is different. A term of a predicate that the rule uses with a `from(...)` argument is not checked, because the data gives the arguments only when the case runs.

## Expectations

| Key | Required | Value |
| --- | --- | --- |
| `result` | Yes | The expected result: `True`, `False` or `Unknown`. |
| `faults` | No | The expected number of faults, a whole number from 0. |
| `trace` | No | The expected trace tree, as `truthweaver evaluate --trace` prints it. The check ignores the style of the line endings, spaces at the end of a line and empty lines at the end. |

A case passes when each expectation holds. The rule evaluates with short-circuiting, so the trace shows the terms that the evaluation did not reach.

## Data for from(...) references

A term with a `from(...)` argument takes the value that the data gives, and then the stub value of the term with that value. With `{ region: EU }` as `user`, the term `inRegion(region: from("user", "$.region"))` takes the stub value of `inRegion(region: "EU")`. Give the stub value for that resolved term in `given`.

<!-- doctest:skip a rule test file, not a rule; RuleTestDocumentationTests runs it -->
```yaml
rule: |
  isActive AND inRegion(region: from("user", "$.region"))
cases:
  - name: a user in the EU
    given:
      isActive: True
      'inRegion(region: "EU")': True
    variables:
      user: { region: EU }
    expect:
      result: True
  - name: no data is a fault
    given:
      isActive: True
    expect:
      result: Unknown
      faults: 1
```

A reference to a source that has no data, or a query that finds nothing, is a fault, and the term is `Unknown`. An argument that only `from(...)` references give is a string, so data of another kind, such as a number, is a fault.

## Problems in a test file

A file that is not usable fails as a whole, before any case runs. The reader reports each problem with its path and its line and column: YAML that is not well formed, a missing or unknown key, empty rule text, a value that is not a truth value, a term key that is not exactly one term or that has a `from(...)` reference, the same term or case name two times, and a rule file or data file that cannot be read.

A rule that does not compile fails each case, and the report holds the compile diagnostics. For an inline rule, the line and column of a compile diagnostic count in the rule text, not in the test file.

## Run the tests from C#

`RuleTestFile.RunAsync(path)` loads the file and runs each case. `ShouldPass()` on the report throws a `RuleTestException` that lists each failed case, so the test fails in any test framework. A file that is not usable throws a `RuleTestException` that lists the problems.

```csharp
RuleTestReport report = await RuleTestFile.RunAsync("rules/access.test.yaml");

report.ShouldPass();
```

`RuleTestFile.TryLoad` and `TryParse` read a file without an exception and return each problem as a `RuleTestFileError`. The instance method `RunAsync` runs the cases of a file that is already read. A report has one `RuleTestResult` for each case, with the decision, the trace and the reasons for a failure. `PrintJUnit()` writes the report as JUnit XML.

`StubRuleCompiler.Compile` compiles any rule over the stand-in predicates. The compiled rule takes a `StubValues` as its context, so you can evaluate it with values that you set in code:

```csharp
CompiledRule<StubValues> rule = StubRuleCompiler.Compile("isActive AND isAdmin", RuleFormat.Text).GetRuleOrThrow();
StubValues stubs = StubValues.Create(new Dictionary<string, TruthValue> { ["isActive"] = TruthValue.True });

Decision decision = await rule.EvaluateAsync(stubs);
```

## Run the tests from the command line

`truthweaver test <file>` runs a test file and lists each case as `PASS` or `FAIL`, with the reasons. The exit code is 0 when each case passes, 1 when a case fails or the rule does not compile, and 2 when the file is not usable. `--junit <file>` also writes the JUnit XML report. When the file is not usable, no case runs and the command writes no JUnit report. See [Command line](cli.md#test).

## JUnit report

The JUnit XML has one `testsuite`, named after the file, with one `testcase` for each case. A failed expectation is a `failure` element with the reasons. A rule that does not compile is an `error` element in each case, with the compile errors. The report has no time attributes, so the same run writes the same bytes.

## JSON schema

The schema of a rule test file is `rule-test.schema.json`, in the `content` folder of the `TruthWeaver.Testing` package. Point a YAML editor at it to get completion and checks while you write a file.
