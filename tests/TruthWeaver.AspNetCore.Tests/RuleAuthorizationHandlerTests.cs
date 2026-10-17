namespace TruthWeaver.AspNetCore.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Rule policies through <see cref="IAuthorizationService"/>: a satisfied rule succeeds, every other result fails closed, and
/// the failure keeps the decision.
/// </summary>
public sealed class RuleAuthorizationHandlerTests
{
    private const string Policy = "rule-policy";

    /// <summary>A rule that is <c>True</c> meets the policy.</summary>
    [Fact]
    public async Task AuthorizeAsync_RuleTrue_Succeeds_Test()
    {
        AuthorizationResult result = await AuthorizeAsync("isInRole(role: \"editor\")", TestSupport.User("alice", ["editor"]));

        Assert.True(result.Succeeded);
    }

    /// <summary>A rule that is <c>False</c> fails the policy, and the failure holds the decision with the <c>False</c> result.</summary>
    [Fact]
    public async Task AuthorizeAsync_RuleFalse_FailsWithDecision_Test()
    {
        AuthorizationResult result = await AuthorizeAsync("isInRole(role: \"editor\")", TestSupport.User("alice"));

        Assert.False(result.Succeeded);
        RuleAuthorizationFailureReason failure = Assert.Single(result.GetRuleFailures());
        Assert.Equal(TruthValue.False, failure.Decision.Result);
        Assert.Equal(Policy, failure.Requirement.Name);
    }

    /// <summary>A rule that is <c>Unknown</c> fails the policy by default.</summary>
    [Fact]
    public async Task AuthorizeAsync_RuleUnknownByDefault_Fails_Test()
    {
        AuthorizationResult result = await AuthorizeAsync("isOwner", TestSupport.User("alice"));

        Assert.False(result.Succeeded);
        Assert.Equal(TruthValue.Unknown, Assert.Single(result.GetRuleFailures()).Decision.Result);
    }

    /// <summary>With <see cref="RuleRequirementOptions.AllowUnknown"/> set, a rule that is <c>Unknown</c> meets the policy.</summary>
    [Fact]
    public async Task AuthorizeAsync_RuleUnknownWithAllowUnknown_Succeeds_Test()
    {
        AuthorizationResult result = await AuthorizeAsync(
            "isOwner",
            TestSupport.User("alice"),
            options: new RuleRequirementOptions { AllowUnknown = true }
        );

        Assert.True(result.Succeeded);
    }

    /// <summary>With <see cref="RuleRequirementOptions.AllowUnknown"/> set, a rule that is <c>False</c> still fails the policy.</summary>
    [Fact]
    public async Task AuthorizeAsync_RuleFalseWithAllowUnknown_Fails_Test()
    {
        AuthorizationResult result = await AuthorizeAsync(
            "isInRole(role: \"editor\")",
            TestSupport.User("alice"),
            options: new RuleRequirementOptions { AllowUnknown = true }
        );

        Assert.False(result.Succeeded);
    }

    /// <summary>A predicate that throws makes the rule <c>Unknown</c> with a fault, and the policy fails with the fault in the decision.</summary>
    [Fact]
    public async Task AuthorizeAsync_FaultingPredicate_FailsWithFault_Test()
    {
        AuthorizationResult result = await AuthorizeAsync(
            "isAuthenticated AND isOnCall",
            TestSupport.User("alice"),
            configure: builder => TestSupport.AddFaulting(builder, "isOnCall")
        );

        Assert.False(result.Succeeded);
        Decision decision = Assert.Single(result.GetRuleFailures()).Decision;
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.IsType<InvalidOperationException>(fault.Exception);
    }

    /// <summary>With <see cref="RuleRequirementOptions.AllowUnknown"/> set, an <c>Unknown</c> result that a fault caused also meets the policy.</summary>
    [Fact]
    public async Task AuthorizeAsync_FaultWithAllowUnknown_Succeeds_Test()
    {
        AuthorizationResult result = await AuthorizeAsync(
            "isOnCall",
            TestSupport.User("alice"),
            configure: builder => TestSupport.AddFaulting(builder, "isOnCall"),
            options: new RuleRequirementOptions { AllowUnknown = true }
        );

        Assert.True(result.Succeeded);
    }

    /// <summary>A resource-based check passes its resource to the rule, so <c>isOwner</c> decides it.</summary>
    [Theory]
    [InlineData("alice", true)]
    [InlineData("bob", false)]
    public async Task AuthorizeAsync_ResourceBasedCheck_RuleReadsResource_Test(string userId, bool expected)
    {
        AuthorizationResult result = await AuthorizeAsync("isOwner", TestSupport.User(userId), new Document("doc-1", "alice"));

        Assert.Equal(expected, result.Succeeded);
    }

    /// <summary>A resource of another type gives the rule no resource, so <c>isOwner</c> is <c>Unknown</c> and the policy fails.</summary>
    [Fact]
    public async Task AuthorizeAsync_ResourceOfOtherType_RuleHasNoResource_Test()
    {
        AuthorizationResult result = await AuthorizeAsync("isOwner", TestSupport.User("alice"), resource: "not a document");

        Assert.Equal(TruthValue.Unknown, Assert.Single(result.GetRuleFailures()).Decision.Result);
    }

    /// <summary>A host context type works through a mapper: the rule reads what the mapper built from the check.</summary>
    [Fact]
    public async Task AddTruthWeaverPolicy_CustomMapper_EvaluatesHostContext_Test()
    {
        PredicateRegistryBuilder<string> builder = PredicateRegistry<string>.CreateBuilder();
        builder.Add(
            PredicateSchema.NoArguments("isAlice", "Is Alice", "Is the mapped user name alice?"),
            (name, _, _) => ValueTask.FromResult(name == "alice" ? TruthValue.True : TruthValue.False)
        );
        CompiledRule<string> rule = new RuleCompiler<string>(builder.Build()).Compile("isAlice").GetRuleOrThrow();
        await using ServiceProvider provider = CreateServices(options =>
            options.AddTruthWeaverPolicy(
                Policy,
                rule,
                (context, _) => context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty
            )
        );
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        AuthorizationResult alice = await authorization.AuthorizeAsync(TestSupport.User("alice"), null, Policy);
        AuthorizationResult bob = await authorization.AuthorizeAsync(TestSupport.User("bob"), null, Policy);

        Assert.True(alice.Succeeded);
        Assert.False(bob.Succeeded);
    }

    /// <summary><c>RequireRule</c> without a name gives the requirement <see cref="RuleRequirement.DefaultName"/>, never the rule text.</summary>
    [Fact]
    public async Task RequireRule_NoName_UsesDefaultName_Test()
    {
        CompiledRule<AuthorizationContext<Document>> rule = TestSupport.Compile("isInRole(role: \"editor\")");
        await using ServiceProvider provider = CreateServices(options =>
            options.AddPolicy(Policy, policy => policy.RequireRule(rule))
        );
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        AuthorizationResult result = await authorization.AuthorizeAsync(TestSupport.User("alice"), null, Policy);

        Assert.Equal(RuleRequirement.DefaultName, Assert.Single(result.GetRuleFailures()).Requirement.Name);
    }

    /// <summary>A requirement name in the options wins over the policy name.</summary>
    [Fact]
    public async Task AddTruthWeaverPolicy_NameInOptions_UsesOptionName_Test()
    {
        AuthorizationResult result = await AuthorizeAsync(
            "isInRole(role: \"editor\")",
            TestSupport.User("alice"),
            options: new RuleRequirementOptions { Name = "editors-only" }
        );

        Assert.Equal("editors-only", Assert.Single(result.GetRuleFailures()).Requirement.Name);
    }

    /// <summary>A denied requirement writes one log entry with the requirement name, the result and the fault count.</summary>
    [Fact]
    public async Task HandleAsync_RequirementDenied_LogsDecision_Test()
    {
        using ListLoggerProvider logs = new();
        CompiledRule<AuthorizationContext<Document>> rule = TestSupport.Compile("isOwner");
        await using ServiceProvider provider = CreateServices(options => options.AddTruthWeaverPolicy(Policy, rule), logs);
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        await authorization.AuthorizeAsync(TestSupport.User("alice"), null, Policy);

        (string _, LogLevel level, string? eventName, string message) = Assert.Single(
            logs.Entries,
            entry => entry.Category.StartsWith("TruthWeaver.AspNetCore", StringComparison.Ordinal)
        );
        Assert.Equal(LogLevel.Information, level);
        Assert.Equal("RuleRequirementDenied", eventName);
        Assert.Contains(Policy, message, StringComparison.Ordinal);
        Assert.Contains("Unknown", message, StringComparison.Ordinal);
    }

    /// <summary>Calling the registration twice registers the handler once, so a rule is evaluated once per check.</summary>
    [Fact]
    public void AddTruthWeaverAuthorization_CalledTwice_RegistersOneHandler_Test()
    {
        ServiceCollection services = new();

        services.AddTruthWeaverAuthorization().AddTruthWeaverAuthorization();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAuthorizationHandler));
    }

    /// <summary>An absent failure has no rule failures.</summary>
    [Fact]
    public void GetRuleFailures_NullFailure_ReturnsEmpty_Test()
    {
        AuthorizationFailure? failure = null;

        Assert.Empty(failure.GetRuleFailures());
    }

    /// <summary><see cref="RuleRequirement.IsMetBy"/> accepts only <c>True</c> by default.</summary>
    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    [InlineData(TruthValue.Unknown, false)]
    public void IsMetBy_DefaultOptions_AcceptsOnlyTrue_Test(TruthValue value, bool expected)
    {
        RuleRequirement requirement = new RuleRequirement<AuthorizationContext<Document>>(
            TestSupport.Compile("isAuthenticated"),
            (context, http) => new AuthorizationContext<Document>(context.User, null, http)
        );

        Assert.Equal(expected, requirement.IsMetBy(new Decision(value, [])));
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        string text,
        ClaimsPrincipal user,
        object? resource = null,
        Action<PredicateRegistryBuilder<AuthorizationContext<Document>>>? configure = null,
        RuleRequirementOptions? options = null
    )
    {
        CompiledRule<AuthorizationContext<Document>> rule = TestSupport.Compile(text, configure);
        await using ServiceProvider provider = CreateServices(authorizationOptions =>
            authorizationOptions.AddTruthWeaverPolicy(Policy, rule, options)
        );
        return await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, resource, Policy);
    }

    private static ServiceProvider CreateServices(Action<AuthorizationOptions> configure, ILoggerProvider? logs = null)
    {
        ServiceCollection services = new();
        services.AddLogging(logging =>
        {
            if (logs is not null)
            {
                logging.AddProvider(logs);
            }
        });
        services.AddAuthorization(configure);
        services.AddTruthWeaverAuthorization();
        return services.BuildServiceProvider();
    }
}
