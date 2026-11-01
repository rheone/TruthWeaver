namespace TruthWeaver.Predicates.Tests;

using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Pins registration-time argument checks on every public predicate factory of this package: a null selector and an
/// undefined <see cref="NullBehavior"/> value throw when the factory is called, so a host wiring error surfaces at startup
/// and not as a fault on every evaluation. Each factory is found by reflection, so a new factory is checked too.
/// </summary>
public class FactoryArgumentValidationTests
{
    private static readonly Type[] FactoryTypes =
    [
        typeof(CollectionPredicates),
        typeof(DateTimePredicates),
        typeof(NumericPredicates),
        typeof(RegexPredicates),
        typeof(ScalarPredicates),
        typeof(StringPredicates),
        typeof(TypePredicates),
    ];

    /// <summary>Gets every public factory that takes a selector, as "Type.Method(parameter types)".</summary>
    public static TheoryData<string> SelectorFactories => [.. Factories(_ => true).Keys];

    /// <summary>Gets every public factory that takes a <see cref="NullBehavior"/>.</summary>
    public static TheoryData<string> NullBehaviorFactories =>
        [.. Factories(m => m.GetParameters().Any(p => p.Name == "nullBehavior")).Keys];

    /// <summary>A factory called with a null selector throws <see cref="ArgumentNullException"/> naming the selector.</summary>
    /// <param name="factory">The factory, as "Type.Method(parameter types)".</param>
    [Theory]
    [MemberData(nameof(SelectorFactories))]
    public void Factory_NullSelector_ThrowsArgumentNullException_Test(string factory)
    {
        MethodInfo method = Factories(_ => true)[factory];

        TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, Arguments(method, selectorIsNull: true, NullBehavior.Default))
        );

        ArgumentNullException exception = Assert.IsType<ArgumentNullException>(thrown.InnerException);
        Assert.Equal("selector", exception.ParamName);
    }

    /// <summary>
    /// A factory called with a <see cref="NullBehavior"/> value that is not defined throws
    /// <see cref="ArgumentOutOfRangeException"/>, and does not answer Unknown silently.
    /// </summary>
    /// <param name="factory">The factory, as "Type.Method(parameter types)".</param>
    [Theory]
    [MemberData(nameof(NullBehaviorFactories))]
    public void Factory_UndefinedNullBehavior_ThrowsArgumentOutOfRangeException_Test(string factory)
    {
        MethodInfo method = Factories(_ => true)[factory];

        TargetInvocationException thrown = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, Arguments(method, selectorIsNull: false, (NullBehavior)3))
        );

        ArgumentOutOfRangeException exception = Assert.IsType<ArgumentOutOfRangeException>(thrown.InnerException);
        Assert.Equal("nullBehavior", exception.ParamName);
    }

    /// <summary>A factory called with valid arguments and each defined <see cref="NullBehavior"/> does not throw.</summary>
    /// <param name="factory">The factory, as "Type.Method(parameter types)".</param>
    [Theory]
    [MemberData(nameof(NullBehaviorFactories))]
    public void Factory_ValidArguments_DoesNotThrow_Test(string factory)
    {
        MethodInfo method = Factories(_ => true)[factory];

        foreach (NullBehavior behavior in Enum.GetValues<NullBehavior>())
        {
            Assert.NotNull(method.Invoke(null, Arguments(method, selectorIsNull: false, behavior)));
        }
    }

    // The public generic factories with a selector parameter, closed over TestContext, keyed by a readable signature.
    private static Dictionary<string, MethodInfo> Factories(Func<MethodInfo, bool> filter)
    {
        return FactoryTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.IsGenericMethodDefinition && m.GetParameters().Any(p => p.Name == "selector"))
            .Select(m => m.MakeGenericMethod(typeof(TestContext)))
            .Where(filter)
            .ToDictionary(
                m =>
                    $"{m.DeclaringType!.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => TypeName(p.ParameterType)))})",
                m => m,
                StringComparer.Ordinal
            );
    }

    // Builds an argument list: a name, a selector (null, or one that returns default), TimeProvider.System, the given
    // NullBehavior, and the declared default for every other optional parameter.
    private static object?[] Arguments(MethodInfo method, bool selectorIsNull, NullBehavior nullBehavior)
    {
        return
        [
            .. method
                .GetParameters()
                .Select(p =>
                    p.Name switch
                    {
                        "name" => "factoryUnderTest",
                        "selector" => selectorIsNull ? null : DefaultSelector(p.ParameterType),
                        "nullBehavior" => nullBehavior,
                        _ when p.ParameterType == typeof(TimeProvider) => TimeProvider.System,
                        _ => p.DefaultValue,
                    }
                ),
        ];
    }

    // A readable type name with generic arguments, so overloads that differ only in a selector's result type get distinct keys.
    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    private static Delegate DefaultSelector(Type delegateType)
    {
        Type[] types = delegateType.GetGenericArguments();
        ParameterExpression context = Expression.Parameter(types[0], "context");
        return Expression.Lambda(delegateType, Expression.Default(types[^1]), context).Compile();
    }
}
