using Solqaryn.Application.AutomationV5;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0804SectorModuleApplicationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Tenant = "tenant-A";
    private const string Module = "sector.retail";
    private static DomainRules.VerifiedActorAndPolicy Policy() => new()
    {
        ActorId = "actor-7",
        VerifiedTenantId = Tenant,
        EffectiveRoleId = "role-admin",
        AuthenticatedJwtVerifiedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentExplicitRelationalRbacGrant = true,
        TrustedCapabilityCatalogLoaded = true,
        CurrentPlanAllowsCapability = true,
        CurrentRubroAllowsCapability = true,
        CurrentTenantAllowsCapability = true,
        TrustedPolicyRevisionVerified = true,
        TrustedResourceOwnershipVerified = true,
        ResourceOwnerTenantId = Tenant,
        CurrentCapabilityKey = Module,
        CurrentModuleRevision = 6
    };
    private static C0804SectorModuleApplicationGate.VerifiedEvidence Evidence() =>
        new(Sha, Module, Tenant, Tenant, true, true, true, true,
            DomainRules.State.Disabled, Policy());
    private static C0804SectorModuleApplicationGate.Request Request(
        DomainRules.Operation op = DomainRules.Operation.Activate) =>
        new(Module, Tenant, Sha, op, 6, "task-key-9");
    private sealed class Reader(C0804SectorModuleApplicationGate.VerifiedEvidence? snapshot)
        : IC0804SectorModuleEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C0804SectorModuleApplicationGate.VerifiedEvidence?> ReadAsync(
            string moduleKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(snapshot);
        }
    }
    private static async Task<C0804SectorModuleApplicationGate.Result> Check(
        C0804SectorModuleApplicationGate.Request? request = null,
        C0804SectorModuleApplicationGate.VerifiedEvidence? evidence = null)
    {
        var gate = new C0804SectorModuleApplicationGate(new Reader(evidence ?? Evidence()));
        return await gate.EvaluateAsync(request ?? Request());
    }

    [Fact]
    public async Task ValidPreflightNeverGrantsOrMutatesResources()
    {
        var v = await Check();
        Assert.True(v.RequiresAuthenticatedMutationAdapter);
        Assert.False(v.AuthorizesModuleActivation);
        Assert.False(v.AuthorizesTenantAccess);
        Assert.False(v.WritesToDatabase);
        Assert.False(v.AuthorizesRelease);
    }
    [Fact]
    public async Task MissingRequestOrSourceShaFailsBeforeIo()
    {
        var reader = new Reader(Evidence());
        var gate = new C0804SectorModuleApplicationGate(reader);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA", (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await gate.EvaluateAsync(Request() with { ExpectedSourceSha = "short" })).Code);
        Assert.Equal(0, reader.Calls);
    }
    [Fact]
    public async Task ClientOverridesNeverReachReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C0804SectorModuleApplicationGate(reader);
        Assert.Equal("UNTRUSTED_PRIVILEGE_OR_SCHEMA_OVERRIDE",
            (await gate.EvaluateAsync(Request() with { RequestsGlobalGrantOrSchemaMutation = true })).Code);
        Assert.Equal("UNTRUSTED_PRIVILEGE_OR_SCHEMA_OVERRIDE",
            (await gate.EvaluateAsync(Request() with { ClientControlsTenantOrResource = true })).Code);
        Assert.Equal("UNTRUSTED_PRIVILEGE_OR_SCHEMA_OVERRIDE",
            (await gate.EvaluateAsync(Request() with { ClientControlsCatalogOrPermission = true })).Code);
        Assert.Equal(0, reader.Calls);
    }
    [Fact]
    public async Task TenantBAndForeignOwnerAreRejected()
    {
        Assert.Equal("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED",
            (await Check(request: Request() with { RequestedTenantId = "tenant-B" })).Code);
        Assert.Equal("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED",
            (await Check(evidence: Evidence() with { PersistedOwnerTenantId = "tenant-B" })).Code);
    }
    [Fact]
    public async Task StaleSourceOrModuleIdentityIsRejected()
    {
        Assert.Equal("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED",
            (await Check(evidence: Evidence() with { ObservedSourceSha = new string('a', 40) })).Code);
        Assert.Equal("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED",
            (await Check(evidence: Evidence() with { PersistedModuleKey = "sector.other" })).Code);
    }
    [Fact]
    public async Task CurrentJwtMembershipAndRbacAreNecessary()
    {
        Assert.Equal("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentBackendJwtVerified = false })).Code);
        Assert.Equal("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentMembershipAndRbacVerified = false })).Code);
        Assert.Equal("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { VerifiedFromTrustedBackend = false })).Code);
    }
    [Fact]
    public async Task AuditAndConcurrencyFenceRequired() =>
        Assert.Equal("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { AuditAndConcurrencyFenceAvailable = false })).Code);
    [Fact]
    public async Task MismatchedPolicyTenantRejected() =>
        Assert.Equal("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { VerifiedTenantId = "tenant-B" } })).Code);
    [Fact]
    public async Task RevokedActorAndMissingRelationalGrantDenied()
    {
        Assert.Equal("TRUSTED_ACTIVE_ACTOR_AND_MEMBERSHIP_REQUIRED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { UserOrTenantOrRoleRevoked = true } })).Code);
        Assert.Equal("TRUSTED_POLICY_AND_EXPLICIT_RBAC_REQUIRED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { CurrentExplicitRelationalRbacGrant = false } })).Code);
    }
    [Fact]
    public async Task DisallowedPlanRubroOrTenantDenied()
    {
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { CurrentPlanAllowsCapability = false } })).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { CurrentRubroAllowsCapability = false } })).Code);
        Assert.Equal("CAPABILITY_NOT_ENTITLED_OR_DISABLED",
            (await Check(evidence: Evidence() with { Policy = Policy() with { DenyListBlocksCapability = true } })).Code);
    }
    [Fact]
    public async Task RevisionAndIdempotencyMustBeCurrent()
    {
        Assert.Equal("STALE_MODULE_VERSION",
            (await Check(request: Request() with { ExpectedRevision = 5 })).Code);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED",
            (await Check(request: Request() with { IdempotencyKey = "" })).Code);
    }
    [Fact]
    public async Task IllegalLifecycleAndOperationDenied()
    {
        Assert.Equal("ILLEGAL_MODULE_LIFECYCLE_TRANSITION",
            (await Check(request: Request(DomainRules.Operation.Resume))).Code);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await Check(request: Request((DomainRules.Operation)999))).Code);
    }
    [Fact]
    public async Task ReaderNullSnapshotFailsClosed()
    {
        var gate = new C0804SectorModuleApplicationGate(new Reader(null));
        Assert.Equal("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }
    [Fact]
    public async Task CancellationPropagatesBeforeAnyRead()
    {
        var reader = new Reader(Evidence());
        var gate = new C0804SectorModuleApplicationGate(reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await gate.EvaluateAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }

    // C08-04.M05.02: real Application DTO/error-contract regressions.
    // These in-process assertions do not claim an authenticated HTTP endpoint.
    private sealed class FaultingEvidenceReader(Exception error) : IC0804SectorModuleEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C0804SectorModuleApplicationGate.VerifiedEvidence?> ReadAsync(
            string moduleKey, CancellationToken cancellationToken)
        {
            Calls++;
            throw error;
        }
    }

    [Fact]
    public async Task C0804M0502_EvidenceReadFailureIsOpaqueAndCannotAuthorize()
    {
        var reader = new FaultingEvidenceReader(
            new InvalidOperationException("SECRET_JWT_AND_TENANT_B"));
        var gate = new C0804SectorModuleApplicationGate(reader);
        var result = await gate.EvaluateAsync(Request());
        Assert.Equal("TRUSTED_MODULE_EVIDENCE_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("SECRET", result.Code);
        Assert.False(result.RequiresAuthenticatedMutationAdapter);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.AuthorizesModuleActivation);
        Assert.False(result.WritesToDatabase);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task C0804M0502_TimeoutAlsoReturnsSafeErrorWithoutGrant()
    {
        var reader = new FaultingEvidenceReader(
            new TimeoutException("PRIVATE_PROVIDER_HOST_AND_PASSWORD"));
        var result = await new C0804SectorModuleApplicationGate(reader).EvaluateAsync(Request());
        Assert.Equal("TRUSTED_MODULE_EVIDENCE_UNAVAILABLE", result.Code);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.RequiresAuthenticatedMutationAdapter);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task C0804M0502_CancellationFromEvidenceReaderIsNotConvertedToSuccess()
    {
        var reader = new FaultingEvidenceReader(new OperationCanceledException("cancelled"));
        var gate = new C0804SectorModuleApplicationGate(reader);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await gate.EvaluateAsync(Request()));
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task C0804M0502_InvalidDtoFailsBeforeFaultingBackendEvidence()
    {
        var reader = new FaultingEvidenceReader(new InvalidOperationException("SECRET"));
        var gate = new C0804SectorModuleApplicationGate(reader);
        var invalid = Request() with { ExpectedSourceSha = "bad", RequestedTenantId = "tenant-B" };
        var result = await gate.EvaluateAsync(invalid);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA", result.Code);
        Assert.Equal(0, reader.Calls);
        Assert.False(result.AuthorizesTenantAccess);
    }

}
