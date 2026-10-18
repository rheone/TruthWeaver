# TruthWeaver.Tests

Tests for [TruthWeaver](../../src/TruthWeaver/README.md).

## Purpose

This project holds the engine tests: the parser, the compiler, the evaluator, the analyzer and the printers.

It also holds the property tests, which use CsCheck, and the documentation tests that check the Markdown files of the repository.

## Depends on

- [TruthWeaver.Abstractions](../../src/TruthWeaver.Abstractions/README.md) for the shared value types.
- [TruthWeaver](../../src/TruthWeaver/README.md) for the compiler and the evaluator.
- [TruthWeaver.Yaml](../../src/TruthWeaver.Yaml/README.md) for the YAML format.
- [TruthWeaver.Testing](../../src/TruthWeaver.Testing/README.md) for fake predicates and decision assertions.
- [TruthWeaver.Predicates](../../src/TruthWeaver.Predicates/README.md) for the predicate library.
