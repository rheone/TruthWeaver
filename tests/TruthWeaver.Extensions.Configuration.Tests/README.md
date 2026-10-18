# TruthWeaver.Extensions.Configuration.Tests

Tests for [TruthWeaver.Extensions.Configuration](../../src/TruthWeaver.Extensions.Configuration/README.md).

## Purpose

This project checks the configuration rule store: loading, atomic swap on reload, and a bad reload that keeps the last good rules.

## Depends on

- [TruthWeaver.Abstractions](../../src/TruthWeaver.Abstractions/README.md) for the shared value types.
- [TruthWeaver](../../src/TruthWeaver/README.md) for the compiler and the evaluator.
- [TruthWeaver.Extensions.Configuration](../../src/TruthWeaver.Extensions.Configuration/README.md) for the code under test.
- [TruthWeaver.Testing](../../src/TruthWeaver.Testing/README.md) for fake predicates.
