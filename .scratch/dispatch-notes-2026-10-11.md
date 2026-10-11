# Dispatch notes

- Ticket B: the editor LSP locks TruthWeaver.Generators.dll in Debug builds; use --artifacts-path for a CI build while the editor is open (the pre-commit hook Release run passed all tests). Doc-example checker got a `words` marker word.
- Ticket C: T, F, U are now reserved names (predicates, terms); K3Rule test helper skips f/t/u; reserved-word compile message reuses TRE0002. Review the wording in CHANGELOG and ADR-0005 decision 16.
- Ticket A: the registry-wide default is a `NullBehaviorScope` (AsyncLocal) with `PredicateFamily`, not a `PredicateRegistryBuilder` property, because Core cannot reference Predicates and factories close over the behavior. New enum member `NullBehavior.Default = 2` is the parameter default. Review the API shape and name. Tests that read the repo (doc examples) fail under `--artifacts-path` outside the repo; use `--artifacts-path .tmp/art` while the editor holds the Generators dll.
- Found while doing A: `docs/predicate-conventions.md` also has the invalid 0x85 byte (as CHANGELOG.md); ticket E covers both.
- Ticket D: all Typst names confirmed, no behavior change. Verification used a page-summarizing fetch tool; the quoted-string-in-math-mode behavior was not shown on a fetched page. Worth one manual look at the Typst docs.
- Ticket E: also fixed `docs/predicate-conventions.md` (same byte), which the ticket did not name. The guard test scans the working tree, not `git ls-files`.
