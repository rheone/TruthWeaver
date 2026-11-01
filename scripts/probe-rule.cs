#!/usr/bin/env dotnet
#:project ../src/TruthWeaver.Yaml/TruthWeaver.Yaml.csproj

// Compiles one or more rule files and prints each file's diagnostics and canonical rule text.
//
// The format comes from the extension: .json is JSON, .yaml and .yml are YAML, anything else is rule text. The
// compiler runs in Lenient mode, so a predicate name needs no registration: an unknown name compiles to a term that
// is always Unknown. Pass the same rule in several formats to see whether the formats agree, for example to check
// that a name is rejected in rule text, JSON and YAML alike. The exit code is 1 when any file fails to compile.
//
// Usage: dotnet run scripts/probe-rule.cs -- <file> [<file> ...]
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

if (args.Length == 0)
{
    await Console.Error.WriteLineAsync("Usage: dotnet run scripts/probe-rule.cs -- <file> [<file> ...]");
    return 2;
}

RuleCompiler<object> compiler = new(
    PredicateRegistry<object>.CreateBuilder().Build(),
    new CompilerOptions(Mode: CompilationMode.Lenient)
);

int failed = 0;
foreach (string path in args)
{
    RuleFormat format = Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".json" => RuleFormat.Json,
        ".yaml" or ".yml" => RuleFormat.Yaml,
        _ => RuleFormat.Text,
    };

    CompilationResult<object> result = compiler.CompileAs(await File.ReadAllTextAsync(path), format);
    await Console.Out.WriteLineAsync($"{path} ({format})");
    foreach (Diagnostic diagnostic in result.Diagnostics)
    {
        await Console.Out.WriteLineAsync($"  {diagnostic.Code} {diagnostic.Severity}: {diagnostic.Message}");
    }

    if (result.CompiledRule is null)
    {
        failed++;
        await Console.Out.WriteLineAsync("  (not compiled)");
    }
    else
    {
        await Console.Out.WriteLineAsync($"  {result.CompiledRule.PrintRuleText()}");
    }
}

return failed > 0 ? 1 : 0;
