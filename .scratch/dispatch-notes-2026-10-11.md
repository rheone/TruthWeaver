# Dispatch notes

- Ticket B: the editor LSP locks TruthWeaver.Generators.dll in Debug builds; use --artifacts-path for a CI build while the editor is open (the pre-commit hook Release run passed all tests). Doc-example checker got a `words` marker word.
- Ticket C: T, F, U are now reserved names (predicates, terms); K3Rule test helper skips f/t/u; reserved-word compile message reuses TRE0002. Review the wording in CHANGELOG and ADR-0005 decision 16.
