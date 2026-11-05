namespace TruthWeaver.Evaluation;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;

/// <summary>
/// Finds and compiles the rule that a <see cref="CallExpression"/> names, when the evaluator reaches the call (ADR-0019
/// decisions 7 and 9). One instance belongs to one <see cref="CompiledRule{TContext}"/>. It compiles a library rule with
/// the compiler options of that rule, against the registry of the evaluator that reached the call, so the library rule
/// resolves its predicate and data source names against the caller, and it keeps the result for each library. A host that
/// reloads its library hands the new library to the next evaluation in <see cref="EvaluationOptions.Library"/>; the new
/// library has its own cache, so the old compiled rules are freed with the old library and a call never uses a rule
/// that its library no longer holds.
/// </summary>
/// <param name="compilerOptions">The options the calling rule was compiled with, which a callee is compiled with.</param>
internal sealed class LibraryCalls(CompilerOptions compilerOptions)
{
    // The compile of each library rule for each registry, by library. A library is immutable, so an entry never goes stale;
    // the table drops the entries of a library that nothing references any more. The value is a CompiledRule<TContext> for
    // the context type of the registry (the registry key says which), or the text of why the rule cannot run.
    private readonly ConditionalWeakTable<
        RuleLibrary,
        ConcurrentDictionary<(object Registry, string Name), Lazy<Callee>>
    > compiled = [];

    /// <summary>Gets the library the calling rule was compiled with, which a call uses when the evaluation names no other.</summary>
    public RuleLibrary? CompiledWith => compilerOptions.Library;

    /// <summary>Gets how many calls may nest, which is <see cref="CompilerOptions.MaxDepth"/> of the calling rule.</summary>
    public int MaxCallDepth => compilerOptions.MaxDepth;

    /// <summary>Finds the rule that a call names in a library and compiles it, or says why the call cannot run.</summary>
    /// <typeparam name="TContext">The context type of the evaluator that reached the call.</typeparam>
    /// <param name="registry">The registry of that evaluator, which the callee compiles against.</param>
    /// <param name="library">The library to look the rule up in, or <see langword="null"/> when there is none.</param>
    /// <param name="call">The call.</param>
    /// <param name="failure">Why the call cannot run, or <see langword="null"/> when it can.</param>
    /// <returns>The compiled callee, or <see langword="null"/> when the call cannot run.</returns>
    public CompiledRule<TContext>? Resolve<TContext>(
        PredicateRegistry<TContext> registry,
        RuleLibrary? library,
        CallExpression call,
        out string? failure
    )
    {
        // The rule must still be in the library, and for a pinned call it must still have the pinned content (decision 9).
        if (library is null || !library.TryGet(call.Name, out LibraryRule? rule))
        {
            failure = $"Library rule '{call.Name}' is not in the rule library.";
            return null;
        }

        if (call.PinnedHash is { } pin && !string.Equals(pin, rule.Hash, StringComparison.OrdinalIgnoreCase))
        {
            failure = $"Library rule '{rule.Name}' has content hash {rule.Hash}, not the pinned hash {pin}.";
            return null;
        }

        ConcurrentDictionary<(object Registry, string Name), Lazy<Callee>> forLibrary = this.compiled.GetValue(
            library,
            _ => new ConcurrentDictionary<(object Registry, string Name), Lazy<Callee>>()
        );
        Lazy<Callee> entry = forLibrary.GetOrAdd(
            (registry, rule.Name),
            _ => new Lazy<Callee>(() => Compile(registry, library, rule, compilerOptions))
        );
        failure = entry.Value.Failure;
        return entry.Value.Rule as CompiledRule<TContext>;
    }

    private static Callee Compile<TContext>(
        PredicateRegistry<TContext> registry,
        RuleLibrary library,
        LibraryRule rule,
        CompilerOptions callerOptions
    )
    {
        // The callee compiles with the library it was found in, so its own rule and call references resolve there and
        // a cycle that a reload introduced is a compile error here, not a call that never returns.
        RuleCompiler<TContext> compiler = new(registry, callerOptions with { Library = library });
        if (
            compiler
                .Compile(rule.Text)
                .TryGetRule(out CompiledRule<TContext>? callee, out EquatableArray<Diagnostic> diagnostics)
        )
        {
            return new Callee(callee, null);
        }

        Diagnostic first = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
        return new Callee(null, $"Library rule '{rule.Name}' does not compile here: {first.Code} {first.Message}");
    }

    private sealed record Callee(object? Rule, string? Failure);
}
