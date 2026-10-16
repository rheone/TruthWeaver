namespace TruthWeaver.AspNetCore.Tests;

using System.Security.Claims;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>The ready-made authorization predicates answer from the user's claims and the resource's owner.</summary>
public sealed class AuthorizationPredicatesTests
{
    private static readonly Document OwnedByAlice = new("doc-1", "alice");

    /// <summary><c>hasClaim</c> with a type only is <c>True</c> when the user has any claim of that type, ignoring the type's case.</summary>
    [Fact]
    public async Task HasClaim_TypeOnlyAndClaimPresent_IsTrue_Test()
    {
        ClaimsPrincipal user = TestSupport.User("alice", claims: new Claim("department", "audit"));

        TruthValue result = await EvaluateAsync("hasClaim(type: \"DEPARTMENT\")", user);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary><c>hasClaim</c> with a value is <c>False</c> when the user's claim of that type has another value.</summary>
    [Fact]
    public async Task HasClaim_ValueDiffers_IsFalse_Test()
    {
        ClaimsPrincipal user = TestSupport.User("alice", claims: new Claim("department", "audit"));

        TruthValue result = await EvaluateAsync("hasClaim(type: \"department\", value: \"sales\")", user);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><c>hasClaim</c> with a matching type and value is <c>True</c>.</summary>
    [Fact]
    public async Task HasClaim_TypeAndValueMatch_IsTrue_Test()
    {
        ClaimsPrincipal user = TestSupport.User("alice", claims: new Claim("department", "audit"));

        TruthValue result = await EvaluateAsync("hasClaim(type: \"department\", value: \"audit\")", user);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary><c>hasClaim</c> is <c>False</c>, not <c>Unknown</c>, when the user has no claim of the type.</summary>
    [Fact]
    public async Task HasClaim_ClaimMissing_IsFalse_Test()
    {
        TruthValue result = await EvaluateAsync("hasClaim(type: \"department\")", TestSupport.User("alice"));

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><c>isInRole</c> is <c>True</c> for a role of the user and <c>False</c> for another role.</summary>
    [Theory]
    [InlineData("editor", true)]
    [InlineData("admin", false)]
    public async Task IsInRole_RoleOfUser_MatchesClaimsPrincipal_Test(string role, bool expected)
    {
        ClaimsPrincipal user = TestSupport.User("alice", ["editor"]);

        TruthValue result = await EvaluateAsync($"isInRole(role: \"{role}\")", user);

        Assert.Equal(expected ? TruthValue.True : TruthValue.False, result);
    }

    /// <summary><c>isAuthenticated</c> is <c>True</c> for an authenticated user.</summary>
    [Fact]
    public async Task IsAuthenticated_AuthenticatedUser_IsTrue_Test()
    {
        TruthValue result = await EvaluateAsync("isAuthenticated", TestSupport.User("alice"));

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary><c>isAuthenticated</c> is <c>False</c> for an anonymous user.</summary>
    [Fact]
    public async Task IsAuthenticated_AnonymousUser_IsFalse_Test()
    {
        TruthValue result = await EvaluateAsync("isAuthenticated", TestSupport.Anonymous());

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><c>isOwner</c> is <c>True</c> when the user identifier equals the owner identifier of the resource.</summary>
    [Fact]
    public async Task IsOwner_UserOwnsResource_IsTrue_Test()
    {
        TruthValue result = await EvaluateAsync("isOwner", TestSupport.User("alice"), OwnedByAlice);

        Assert.Equal(TruthValue.True, result);
    }

    /// <summary><c>isOwner</c> is <c>False</c> for another user, and the comparison is ordinal.</summary>
    [Theory]
    [InlineData("bob")]
    [InlineData("ALICE")]
    public async Task IsOwner_OtherUser_IsFalse_Test(string userId)
    {
        TruthValue result = await EvaluateAsync("isOwner", TestSupport.User(userId), OwnedByAlice);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary><c>isOwner</c> is <c>Unknown</c> when the check has no resource, because ownership cannot be decided.</summary>
    [Fact]
    public async Task IsOwner_NoResource_IsUnknown_Test()
    {
        TruthValue result = await EvaluateAsync("isOwner", TestSupport.User("alice"), resource: null);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary><c>isOwner</c> is <c>Unknown</c> when the resource has no owner.</summary>
    [Fact]
    public async Task IsOwner_ResourceWithoutOwner_IsUnknown_Test()
    {
        TruthValue result = await EvaluateAsync("isOwner", TestSupport.User("alice"), new Document("doc-2", null));

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary><c>isOwner</c> is <c>Unknown</c> when the user has no identifier claim.</summary>
    [Fact]
    public async Task IsOwner_UserWithoutIdentifier_IsUnknown_Test()
    {
        TruthValue result = await EvaluateAsync("isOwner", TestSupport.Anonymous(), OwnedByAlice);

        Assert.Equal(TruthValue.Unknown, result);
    }

    /// <summary><c>isOwner</c> reads the claim type that the registration names.</summary>
    [Fact]
    public async Task AddIsOwner_CustomClaimType_ComparesThatClaim_Test()
    {
        CompiledRule<AuthorizationContext<Document>> rule = new TruthWeaver.Compilation.RuleCompiler<
            AuthorizationContext<Document>
        >(PredicateRegistry<AuthorizationContext<Document>>.CreateBuilder().AddIsOwner(d => d.OwnerId, "sub").Build())
            .Compile("isOwner")
            .GetRuleOrThrow();
        ClaimsPrincipal user = TestSupport.User("someone-else", claims: new Claim("sub", "alice"));

        Decision decision = await rule.EvaluateAsync(
            new AuthorizationContext<Document>(user, OwnedByAlice, null),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The registration rejects a missing owner selector.</summary>
    [Fact]
    public void AddIsOwner_NullSelector_Throws_Test()
    {
        PredicateRegistryBuilder<AuthorizationContext<Document>> builder = PredicateRegistry<
            AuthorizationContext<Document>
        >.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddIsOwner(null!));
    }

    private static async Task<TruthValue> EvaluateAsync(string text, ClaimsPrincipal user, Document? resource = null)
    {
        CompiledRule<AuthorizationContext<Document>> rule = TestSupport.Compile(text);
        Decision decision = await rule.EvaluateAsync(
            new AuthorizationContext<Document>(user, resource, null),
            cancellationToken: TestContext.Current.CancellationToken
        );
        return decision.Result;
    }
}
