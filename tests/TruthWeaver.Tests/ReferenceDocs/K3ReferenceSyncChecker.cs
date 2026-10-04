namespace TruthWeaver.Tests.ReferenceDocs;

using System.Reflection;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Predicates;

/// <summary>
/// Keeps the reference under docs/strong-k3 in step with the engine. Every operator in the engine's operator table has one
/// document in <c>gates/</c>, <c>derived/</c>, <c>cardinality/</c> or <c>functions/</c>, named by the lower-cased operator
/// name. Every <c>Decision</c> result transformation has one in <c>result-transformations/</c>. Every public predicate factory
/// of the predicates package has one in <c>predicates/</c>, or is listed in <see cref="K3PredicateDocumentationHold"/>. A
/// document that names nothing in the engine fails.
/// </summary>
/// <remarks>
/// A predicate document is named <c>&lt;kind&gt;-&lt;factory&gt;.md</c>: the factory class name without <c>Predicates</c>, a
/// hyphen and the factory method name, lower-case. Failures are returned, not thrown, so fixtures can prove the check fails.
/// </remarks>
internal static class K3ReferenceSyncChecker
{
    private const string ReferenceRoot = "docs/strong-k3";

    private static readonly string[] OperatorDirectories = ["gates", "derived", "cardinality", "functions"];

    private static readonly string HoldList = $"{nameof(K3PredicateDocumentationHold)} (tests/TruthWeaver.Tests/ReferenceDocs)";

    /// <summary>Compares the reference of the repository at <paramref name="repositoryRoot"/> with the real engine.</summary>
    /// <param name="repositoryRoot">The directory holding <c>TruthWeaver.slnx</c>.</param>
    /// <returns>One message per failure; empty when the reference and the engine agree.</returns>
    internal static IReadOnlyList<string> CheckTree(string repositoryRoot)
    {
        string referenceDirectory = Path.Combine(repositoryRoot, "docs", "strong-k3");
        if (!Directory.Exists(referenceDirectory))
        {
            return [$"{ReferenceRoot}: the reference directory does not exist"];
        }

        string[] documents =
        [
            .. Directory
                .EnumerateFiles(referenceDirectory, "*.md", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/')),
        ];

        return Check(
            [.. OperatorDefinitions.All.Select(definition => definition.OpName.ToLowerInvariant())],
            [
                .. typeof(Decision)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => method.Name is nameof(Decision.Project) or nameof(Decision.Collapse))
                    .Select(method => method.Name.ToLowerInvariant())
                    .Distinct(),
            ],
            PredicateStems(),
            K3PredicateDocumentationHold.Stems,
            documents
        );
    }

    /// <summary>Compares engine facts with the reference documents.</summary>
    /// <param name="engineOperators">The lower-case names of the engine's operators.</param>
    /// <param name="engineTransformations">The lower-case names of the <c>Decision</c> result transformations.</param>
    /// <param name="enginePredicates">The document stems of the predicates package's public factories.</param>
    /// <param name="predicatesOnHold">The document stems listed in the hold list.</param>
    /// <param name="documents">The repository-relative paths, with <c>/</c> separators, of every reference Markdown file.</param>
    /// <returns>One message per failure; empty when the reference and the engine agree.</returns>
    internal static IReadOnlyList<string> Check(
        IReadOnlyCollection<string> engineOperators,
        IReadOnlyCollection<string> engineTransformations,
        IReadOnlyCollection<string> enginePredicates,
        IReadOnlyCollection<string> predicatesOnHold,
        IReadOnlyCollection<string> documents
    )
    {
        List<string> failures = [];
        HashSet<string> operatorDocuments = [];
        HashSet<string> transformationDocuments = [];
        HashSet<string> predicateDocuments = [];

        foreach (string path in documents.Order(StringComparer.Ordinal))
        {
            string[] segments = path.Split('/');
            if (
                segments.Length != 4
                || !path.StartsWith(ReferenceRoot + "/", StringComparison.Ordinal)
                || segments[3].Equals("README.md", StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            string stem = Path.GetFileNameWithoutExtension(segments[3]);
            if (OperatorDirectories.Contains(segments[2], StringComparer.Ordinal))
            {
                operatorDocuments.Add(stem);
                if (!engineOperators.Contains(stem, StringComparer.Ordinal))
                {
                    failures.Add($"{path}: the engine has no operator named '{stem}'");
                }
            }
            else if (segments[2] == "result-transformations")
            {
                transformationDocuments.Add(stem);
                if (!engineTransformations.Contains(stem, StringComparer.Ordinal))
                {
                    failures.Add($"{path}: Decision has no result transformation named '{stem}'");
                }
            }
            else if (segments[2] == "predicates")
            {
                predicateDocuments.Add(stem);
                if (!enginePredicates.Contains(stem, StringComparer.Ordinal))
                {
                    failures.Add($"{path}: the predicates package has no predicate factory documented as '{stem}'");
                }
            }
        }

        failures.AddRange(
            engineOperators
                .Where(name => !operatorDocuments.Contains(name))
                .Order(StringComparer.Ordinal)
                .Select(name =>
                    $"{ReferenceRoot}: the engine operator '{name}' has no document in gates/, derived/, cardinality/ or functions/"
                )
        );
        failures.AddRange(
            engineTransformations
                .Where(name => !transformationDocuments.Contains(name))
                .Order(StringComparer.Ordinal)
                .Select(name =>
                    $"{ReferenceRoot}: the Decision result transformation '{name}' has no document in result-transformations/"
                )
        );

        foreach (string stem in enginePredicates.Order(StringComparer.Ordinal))
        {
            bool documented = predicateDocuments.Contains(stem);
            bool held = predicatesOnHold.Contains(stem, StringComparer.Ordinal);
            if (!documented && !held)
            {
                failures.Add(
                    $"{ReferenceRoot}/predicates: the predicate '{stem}' has no document {stem}.md. Add the document, or list it in {HoldList}."
                );
            }
            else if (documented && held)
            {
                failures.Add($"{ReferenceRoot}/predicates: the predicate '{stem}' has a document. Remove it from {HoldList}.");
            }
        }

        failures.AddRange(
            predicatesOnHold
                .Where(stem => !enginePredicates.Contains(stem, StringComparer.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(stem => $"{HoldList}: '{stem}' names no predicate factory of the predicates package")
        );
        return failures;
    }

    /// <summary>Lists the document stem of every public predicate factory of the predicates package.</summary>
    private static string[] PredicateStems()
    {
        return
        [
            .. typeof(StringPredicates)
                .Assembly.GetExportedTypes()
                .Where(type =>
                    type is { IsAbstract: true, IsSealed: true, Namespace: "TruthWeaver.Predicates" }
                    && type.Name.EndsWith("Predicates", StringComparison.Ordinal)
                )
                .SelectMany(type =>
                    type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                        .Where(method => method.IsGenericMethodDefinition && method.ReturnType.IsValueType)
                        .Select(method =>
                            $"{type.Name[..^"Predicates".Length].ToLowerInvariant()}-{method.Name.ToLowerInvariant()}"
                        )
                )
                .Distinct()
                .Order(StringComparer.Ordinal),
        ];
    }
}
