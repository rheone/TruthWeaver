# TruthWeaver.Tool.Core.Tests

Tests for [TruthWeaver.Tool.Core](../../src/TruthWeaver.Tool.Core/README.md).

## Purpose

This project checks the shared code of the command-line tools: format detection, output writers, diagnostics, exporters and importers.

## Folders

The test files follow the folders of [TruthWeaver.Tool.Core](../../src/TruthWeaver.Tool.Core/README.md). Each test file is
in the folder of the type it tests, with the namespace `TruthWeaver.Tool.Core.Tests.<Folder>`. A test that covers several
folders stays at the root.

## Depends on

- [TruthWeaver.Abstractions](../../src/TruthWeaver.Abstractions/README.md) for the shared value types.
- [TruthWeaver](../../src/TruthWeaver/README.md) for the compiler and the evaluator.
- [TruthWeaver.Yaml](../../src/TruthWeaver.Yaml/README.md) for the YAML format.
- [TruthWeaver.Tool.Core](../../src/TruthWeaver.Tool.Core/README.md) for the code under test.
