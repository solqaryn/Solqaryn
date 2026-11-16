using Solqaryn.Application.AutomationV5;
using Xunit;
using Guard = Solqaryn.Domain.AutomationV5.V5PrivateMySqlCutoverDomainRules;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C12-04.M03.03: isolated xUnit proofs, not real JWT/HTTP/MySQL evidence.
/// </summary>
public sealed class C1204PrivateMySqlTenantAuthorizationPolicyTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static C1204VerifiedActor Actor() =>
        new("actor-A", "tenant-A", true, true, true, true);

    private static Guard.Candidate Request() => new()
    {
        Intent = Guard.Intent.InspectReadiness,
        Environment = "dev",
        RequestedTenantId = "tenant-A",
        ExpectedCommitSha = Sha,
        ManifestCommitSha = Sha,
        SourceAndDestinationOwnedByTenant = true
    };

    private sealed class SpyReader : IC1204CutoverReadinessEvidenceReader
    {
        public int Calls { get; private set; }

        public Task<C1204VerifiedEvidence?> ReadVerifiedAsync(
            string actorId, string tenantId, string expectedCommitSha,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<C1204VerifiedEvidence?>(null);
        }
    }

    private static async Task DeniedAsync(
        C1204VerifiedActor? actor, Guard.Candidate? request, string expected)
    {
        var reader = new SpyReader();
        var result = await new C1204PrivateMySqlReadinessApplicationService(reader)
            .ReviewAsync(actor, request);
        Assert.Equal(expected, result.Code);
        Assert.False(result.RequiresIndependentServerEnforcement);
        Assert.False(result.AuthorizesSqlOrMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesDeployment);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ValidReadOnlyPreflightOnlyAllowsEvidenceLookupNeverSql()
    {
        Assert.Null(C1204PrivateMySqlTenantAuthorizationPolicy
            .DenyBeforeEvidenceRead(Actor(), Request()));
        var reader = new SpyReader();
        var result = await new C1204PrivateMySqlReadinessApplicationService(reader)
            .ReviewAsync(Actor(), Request());
        Assert.Equal("EVIDENCE_NOT_VERIFIED", result.Code);
        Assert.False(result.AuthorizesSqlOrMigration);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task NullActorOrRequestFailsBeforeReader()
    {
        await DeniedAsync(null, Request(), "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor(), null, "VERIFIED_SERVER_ACTOR_REQUIRED");
    }

    [Fact]
    public async Task TenantIsolationRejectsAtoBandUnverifiedOwnership()
    {
        await DeniedAsync(Actor(), Request() with { RequestedTenantId = "tenant-B" },
            "TENANT_OWNERSHIP_UNVERIFIED");
        await DeniedAsync(Actor() with { TenantId = "tenant-B" }, Request(),
            "TENANT_OWNERSHIP_UNVERIFIED");
        await DeniedAsync(Actor(), Request() with { SourceAndDestinationOwnedByTenant = false },
            "TENANT_OWNERSHIP_UNVERIFIED");
        await DeniedAsync(Actor(), Request() with { RequestedTenantId = "tenant-A " },
            "TENANT_OWNERSHIP_UNVERIFIED");
    }

    [Fact]
    public async Task RevokedRoleMissingJwtOrMfaIsDeniedBeforeExternalRead()
    {
        await DeniedAsync(Actor() with { MembershipRevoked = true }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { MembershipCurrent = false }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { CanReviewCutover = false }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { RecentMfaVerified = false }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { JwtAuthenticatedByServer = false }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
    }

    [Fact]
    public async Task UnsafeActorAndTenantIdsCannotReachReader()
    {
        await DeniedAsync(Actor() with { ActorId = "  " }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { ActorId = "actor\nCRLF" }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
        await DeniedAsync(Actor() with { TenantId = new string('t', 129) }, Request(),
            "VERIFIED_SERVER_ACTOR_REQUIRED");
    }

    [Fact]
    public async Task NonDevEnvironmentsAndUnknownIntentDenyByDefault()
    {
        await DeniedAsync(Actor(), Request() with { Environment = "prod" },
            "NON_DEV_ENVIRONMENT_DENIED");
        await DeniedAsync(Actor(), Request() with { Environment = "qa" },
            "NON_DEV_ENVIRONMENT_DENIED");
        await DeniedAsync(Actor(), Request() with { Environment = "DEV" },
            "NON_DEV_ENVIRONMENT_DENIED");
        await DeniedAsync(Actor(), Request() with { Intent = (Guard.Intent)999 },
            "UNKNOWN_CUTOVER_INTENT");
    }

    [Fact]
    public async Task MutationSecretsCrossTenantAndP0P1AreDeniedBeforeReader()
    {
        await DeniedAsync(Actor(), Request() with { RequestsRealCutoverOrDualWrite = true },
            "UNAUTHORIZED_RUNTIME_MUTATION_DENIED");
        await DeniedAsync(Actor(), Request() with { RequestsGrantElevation = true },
            "UNAUTHORIZED_RUNTIME_MUTATION_DENIED");
        await DeniedAsync(Actor(), Request() with { RequestsDestructiveSchemaMutation = true },
            "UNAUTHORIZED_RUNTIME_MUTATION_DENIED");
        await DeniedAsync(Actor(), Request() with { AllowsCrossTenantFallback = true },
            "UNAUTHORIZED_RUNTIME_MUTATION_DENIED");
        await DeniedAsync(Actor(), Request() with { RequestsSecretOrPersonalDataEmission = true },
            "UNAUTHORIZED_RUNTIME_MUTATION_DENIED");
        await DeniedAsync(Actor(), Request() with { HasUnmitigatedP0OrP1 = true },
            "CRITICAL_FINDING_NO_GO");
    }

    [Fact]
    public async Task ExactShaAndDryRunIdempotencyMustBeValidBeforeReader()
    {
        await DeniedAsync(Actor(), Request() with { ExpectedCommitSha = new string('x', 40) },
            "EXACT_MANIFEST_SHA_REQUIRED");
        await DeniedAsync(Actor(), Request() with { ManifestCommitSha = new string('f', 40) },
            "EXACT_MANIFEST_SHA_REQUIRED");
        await DeniedAsync(Actor(), Request() with {
            Intent = Guard.Intent.ProposeIsolatedDryRun, IdempotencyKey = "  "
        }, "DRY_RUN_IDEMPOTENCY_REQUIRED");
        await DeniedAsync(Actor(), Request() with {
            Intent = Guard.Intent.ProposeIsolatedDryRun, IdempotencyKey = "x\nunsafe"
        }, "DRY_RUN_IDEMPOTENCY_REQUIRED");
    }
}
