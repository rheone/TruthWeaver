# 08: Migrate baselined test names

**What to build:** Rename the test methods listed in `TestNamingBaseline` to `{MemberUnderTest}_{Scenario}_{Expectation}_Test` until the baseline is empty.

**Blocked by:** 03 (done)

**Status:** done

- [x] 450 methods are listed at the start (292 in `TruthWeaver.Tests`, 77 in `TruthWeaver.Abstractions.Tests`, 31 in `TruthWeaver.Predicates.Tests`, 28 in `TruthWeaver.Testing.Tests`, 12 in `TruthWeaver.Yaml.Tests`, 9 in `TruthWeaver.Architecture.Tests`, 1 in `TruthWeaver.DataSources.Json.Tests`)
- [x] Work in batches of about one test class group per context window; remove each renamed method from `TestNamingBaseline` in the same batch
- [x] A rename keeps the test's behavior unchanged; a touched test gets an XML `<summary>`
- [x] `TestNamingBaseline.Methods` ends empty (replace the collection with an empty initializer per `DocumentationLintBaseline` if Roslynator raises RCS1259)
- [x] The full validation set from CLAUDE.md passes

See also [spec](../spec.md) and ticket 03.
