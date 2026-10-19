namespace TruthWeaver.Tool.Tui.Predicates;

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TruthWeaver.Predicates;
using TruthWeaver.Tool.Core.Palette;

/// <summary>
/// The ready-made predicates of the <c>TruthWeaver.Predicates</c> package as palette templates. The catalog scans the public
/// static factories of the package by reflection, so a new factory appears in the palette with no change here. A factory is a
/// generic method that takes a <c>name</c> and a <c>selector</c>; the scan calls it with <see cref="object"/> as the context
/// and a selector that returns the default value, and takes the schema from the result. The method name is the predicate name.
/// </summary>
internal static class PredicateCatalog
{
    // The types that a factory with a value type parameter (the numeric factories) is tried with, in order. The first type
    // that satisfies the constraints of the method wins; the kind of its arguments follows.
    private static readonly Type[] ValueTypes =
    [
        typeof(decimal),
        typeof(long),
        typeof(double),
        typeof(bool),
        typeof(Guid),
        typeof(DateTimeOffset),
    ];

    // Words of a search that stand for a predicate without being in its name, label, family or description. Each phrase
    // names predicates by their method name; a test checks that every name exists in the catalog.
    private static readonly (string Phrase, string[] Names)[] AliasTable =
    [
        ("contains", ["Contains", "ContainsAny", "ContainsAll"]),
        ("includes", ["Contains", "ContainsAny", "ContainsAll"]),
        ("starts", ["StartsWith"]),
        ("prefix", ["StartsWith"]),
        ("begins with", ["StartsWith"]),
        ("suffix", ["EndsWith"]),
        ("ends", ["EndsWith"]),
        ("equals", ["IsEqualTo"]),
        ("same as", ["IsEqualTo"]),
        ("not equal", ["IsNotEqualTo"]),
        ("different from", ["IsNotEqualTo"]),
        ("older than", ["IsBefore", "IsBeforeNow"]),
        ("earlier than", ["IsBefore", "IsBeforeNow"]),
        ("newer than", ["IsAfter", "IsAfterNow"]),
        ("later than", ["IsAfter", "IsAfterNow"]),
        ("past", ["IsBeforeNow"]),
        ("future", ["IsAfterNow"]),
        ("bigger than", ["IsGreaterThan"]),
        ("larger than", ["IsGreaterThan"]),
        ("more than", ["IsGreaterThan", "HasCountGreaterThan"]),
        ("at least", ["IsGreaterThanOrEqualTo", "HasCountGreaterThanOrEqualTo"]),
        ("at most", ["IsLessThanOrEqualTo", "HasCountLessThanOrEqualTo"]),
        ("smaller than", ["IsLessThan"]),
        ("fewer than", ["IsLessThan", "HasCountLessThan"]),
        ("less than", ["IsLessThan", "HasCountLessThan"]),
        ("range", ["IsBetween", "IsNotBetween"]),
        ("within", ["IsBetween"]),
        ("empty", ["IsEmpty", "IsNotEmpty", "IsNullOrEmpty"]),
        ("blank", ["IsNullOrWhiteSpace", "IsEmpty"]),
        ("missing", ["IsNull", "IsNullOrEmpty"]),
        ("nothing", ["IsNull", "IsEmpty"]),
        ("one of", ["IsIn", "ContainsAny"]),
        ("in list", ["IsIn"]),
        ("any of", ["ContainsAny", "IsIn"]),
        ("all of", ["ContainsAll"]),
        ("subset", ["IsSubsetOf"]),
        ("size", ["HasCountEqualTo"]),
        ("length", ["HasCountEqualTo"]),
        ("how many", ["HasCountEqualTo"]),
        ("regex", ["IsMatch"]),
        ("regular expression", ["IsMatch"]),
        ("pattern", ["IsMatch"]),
        ("like", ["IsMatch"]),
        ("weekday", ["IsOnDayOfWeek"]),
        ("day of the week", ["IsOnDayOfWeek"]),
        ("hours", ["IsInTimeWindow"]),
        ("uuid", ["IsGuid"]),
        ("number", ["IsNumeric"]),
        ("link", ["IsUrl"]),
        ("uri", ["IsUrl"]),
        ("text", ["IsString"]),
    ];

    private static readonly Lazy<IReadOnlyList<CatalogPredicate>> Scanned = new(Scan);

    /// <summary>Gets every predicate of the catalog, grouped by family, in the order of the factories.</summary>
    public static IReadOnlyList<CatalogPredicate> All => Scanned.Value;

    /// <summary>Gets the aliases: a phrase and the method names of the predicates it stands for.</summary>
    public static IReadOnlyList<(string Phrase, IReadOnlyList<string> Names)> Aliases { get; } =
    [.. AliasTable.Select(a => (a.Phrase, (IReadOnlyList<string>)a.Names))];

    /// <summary>
    /// Finds the predicates that match a search. Case does not matter. Every word of the query must match the name, the
    /// label, the family or the start of a word of the description; or the query is part of an alias.
    /// </summary>
    /// <param name="query">The words to search for. An empty query matches every predicate.</param>
    /// <returns>The matching predicates, in the order of <see cref="All"/>.</returns>
    public static IReadOnlyList<CatalogPredicate> Search(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        string trimmed = query.Trim();
        string[] words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> aliased = [with(StringComparer.Ordinal)];
        if (trimmed.Length > 0)
        {
            foreach ((string phrase, string[] names) in AliasTable)
            {
                if (phrase.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    aliased.UnionWith(names);
                }
            }
        }

        return [.. All.Where(p => aliased.Contains(p.Entry.Name) || words.All(w => Matches(p, w)))];
    }

    private static bool Matches(CatalogPredicate predicate, string word)
    {
        if (
            predicate.Entry.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || predicate.Label.Contains(word, StringComparison.OrdinalIgnoreCase)
            || predicate.Family.Contains(word, StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        // A description word, not any part of the text: "is" does not match every word that contains it.
        return predicate
            .Entry.Description.Split([' ', ',', '.', '(', ')', ';', ':', '-', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Any(d => d.StartsWith(word, StringComparison.OrdinalIgnoreCase));
    }

    // The scan reads the factories by reflection and builds generic methods. The tool is a framework-dependent .NET tool; it
    // is not trimmed and not compiled ahead of time (docs/tui.md), so the members it reads are always there.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The tool is framework-dependent and never trimmed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The tool is framework-dependent and never trimmed.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The tool is never compiled ahead of time.")]
    private static List<CatalogPredicate> Scan()
    {
        List<CatalogPredicate> result = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        IEnumerable<Type> families = typeof(NullBehavior)
            .Assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: true, IsSealed: true })
            .OrderBy(t => t.Name, StringComparer.Ordinal);
        foreach (Type type in families)
        {
            string family = FamilyOf(type);
            IEnumerable<MethodInfo> methods = type.GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly
                )
                .OrderBy(m => m.MetadataToken);
            foreach (MethodInfo method in methods)
            {
                if (TryCall(method) is not { } schema)
                {
                    continue;
                }

                CatalogPredicate predicate = new(
                    family,
                    schema.Label,
                    new PaletteEntry(
                        PaletteSource.Catalog,
                        schema.Name,
                        schema.Description,
                        [.. schema.Arguments.Select(a => new PaletteArgument(a.Name, a.Type, a.Required, null, a.Description))]
                    )
                )
                {
                    TakesNullBehavior = method.GetParameters().Any(p => p.ParameterType == typeof(NullBehavior)),
                };

                // Overloads for several value types give the same template; keep one of each.
                string key =
                    $"{family}|{predicate.Entry.Name}|{string.Join(",", predicate.Entry.Arguments.Select(a => a.Kind))}";
                if (seen.Add(key))
                {
                    result.Add(predicate);
                }
            }
        }

        return result;
    }

    // A factory returns the schema and the delegate as a tuple, takes the name as its first parameter, and takes a selector.
    // Anything else (SelectedValuePredicates.Create, a helper) is not a template.
    [RequiresUnreferencedCode("Builds a generic method by reflection.")]
    [RequiresDynamicCode("Builds a generic method by reflection.")]
    private static PredicateSchema? TryCall(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (
            !method.IsGenericMethodDefinition
            || method.ReturnType is not { IsGenericType: true } returnType
            || returnType.GetGenericTypeDefinition() != typeof(ValueTuple<,>)
            || returnType.GetGenericArguments()[0] != typeof(PredicateSchema)
            || parameters.Length < 2
            || parameters[0].Name != "name"
            || parameters[0].ParameterType != typeof(string)
            || parameters[1].Name != "selector"
            || !parameters[1].ParameterType.IsGenericType
        )
        {
            return null;
        }

        Type context = parameters[1].ParameterType.GetGenericArguments()[0];
        Type[] parameterTypes = method.GetGenericArguments();
        foreach (Type valueType in ValueTypes)
        {
            Type[] arguments = [.. parameterTypes.Select(p => p == context ? typeof(object) : valueType)];
            MethodInfo closed;
            try
            {
                closed = method.MakeGenericMethod(arguments);
            }
            catch (ArgumentException)
            {
                // The type does not satisfy the constraints of this factory; try the next.
                continue;
            }

            if (Arguments(closed) is not { } values)
            {
                return null;
            }

            return ((ITuple)closed.Invoke(null, values)!)[0] as PredicateSchema;
        }

        return null;
    }

    // The arguments of a call: the method name, a selector that returns the default value, and the defaults of the rest. A
    // required parameter of another kind has no sensible stub, so the factory is not offered.
    [RequiresDynamicCode("Compiles a selector.")]
    private static object?[]? Arguments(MethodInfo closed)
    {
        ParameterInfo[] parameters = closed.GetParameters();
        object?[] values = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            if (i == 0)
            {
                values[i] = closed.Name;
            }
            else if (parameter.Name == "selector")
            {
                Type[] funcArguments = parameter.ParameterType.GetGenericArguments();
                values[i] = Expression
                    .Lambda(
                        parameter.ParameterType,
                        Expression.Default(funcArguments[1]),
                        Expression.Parameter(funcArguments[0])
                    )
                    .Compile();
            }
            else if (parameter.ParameterType == typeof(TimeProvider))
            {
                values[i] = TimeProvider.System;
            }
            else if (parameter.HasDefaultValue)
            {
                values[i] = parameter.DefaultValue;
            }
            else
            {
                return null;
            }
        }

        return values;
    }

    // The family title from the name of the factory class: StringPredicates is String, DateTimePredicates is Date and time.
    private static string FamilyOf(Type type)
    {
        string name = type.Name.EndsWith("Predicates", StringComparison.Ordinal)
            ? type.Name[..^"Predicates".Length]
            : type.Name;
        if (name == "DateTime")
        {
            return "Date and time";
        }

        StringBuilder title = new();
        foreach (char c in name)
        {
            if (title.Length > 0 && char.IsUpper(c))
            {
                title.Append(' ').Append(char.ToLowerInvariant(c));
            }
            else
            {
                title.Append(c);
            }
        }

        return title.ToString();
    }
}
