using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1401FunctionalAcceptanceAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static readonly IReadOnlySet<string> Cases =
        new HashSet<string>(StringComparer.Ordinal) { "AF01", "AF02" };
    private static readonly IReadOnlySet<string> Actions =
        new HashSet<string>(StringComparer.Ordinal) { "READ_OWN", "PREPARE_ONLY" };

    private static C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext Verified() =>
        new("functional_acceptance", Sha, 7, 11, true, true, true, true,
            false, Cases, Actions);

    private static V5FunctionalAcceptanceDomainRules.AcceptanceEvidence Good() =>
        new("AF01", "READ_OWN", 11, Sha, "ci:acceptance:123",
            true, true, false, false, true, true);

    private static C1401FunctionalAcceptanceAuthorizationGate.Request Intent(
        params V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?[] items) =>
        new("functional_acceptance", Sha, 11,
            items.Length == 0
                ? new V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?[] { Good() }
                : items);

    private sealed class Reader : IC1401FunctionalAcceptanceAuthorityReader
    {
        public C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext? Context = Verified();
        public Exception? Error;
        public int Reads;
        public ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadAsync(
            string key, CancellationToken cancellationToken)
        {
            Reads++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Context);
        }
    }

    private static C1401FunctionalAcceptanceAuthorizationGate Gate(Reader r) => new(r);

    [Fact]
    public async Task ValidPreflightStillNeverGrantsAccessOrCertifiesQa()
    {
        var r = new Reader();
        var decision = await Gate(r).InspectAsync(Intent(Good(), Good() with { CaseKey = "AF02" }));
        Assert.Equal("AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.Equal(2, decision.ItemsChecked);
        Assert.True(decision.RequiresFutureAuthenticatedAdapter);
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.AllowsInstitutionalWrite);
        Assert.False(decision.CertifiesFunctionalAcceptance);
        Assert.False(decision.AuthorizesQaOrProduction);
        Assert.Equal(1, r.Reads);
    }

    [Fact]
    public async Task InvalidResourceShaOrTenantDeniedBeforeReader()
    {
        var r = new Reader();
        Assert.Equal("VALID_RESOURCE_TENANT_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(null)).Code);
        Assert.Equal("VALID_RESOURCE_TENANT_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent() with { ResourceKey = "bad/path" })).Code);
        Assert.Equal("VALID_RESOURCE_TENANT_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent() with { ExpectedSourceSha = "xyz" })).Code);
        Assert.Equal("VALID_RESOURCE_TENANT_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent() with { ClientTenantId = 0 })).Code);
        Assert.Equal(0, r.Reads);
    }

    [Fact]
    public async Task ClientClaimedPermissionMembershipAndProductionBypassDenied()
    {
        var r = new Reader();
        foreach (var intent in new[] {
            Intent() with { ClientClaimsMembership = true },
            Intent() with { ClientClaimsPermission = true },
            Intent() with { RequestsProductionOrBypass = true }
        })
            Assert.Equal("CLIENT_PRIVILEGE_OR_PRODUCTION_OVERRIDE_DENIED",
                (await Gate(r).InspectAsync(intent)).Code);
        Assert.Equal(0, r.Reads);
    }

    [Fact]
    public async Task MissingOrMismatchedPersistedResourceAndSourceDenied()
    {
        var r = new Reader { Context = null };
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent())).Code);
        r.Context = Verified() with { PersistedResourceKey = "another" };
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent())).Code);
        r.Context = Verified() with { SourceCommitSha = new string('a', 40) };
        Assert.Equal("PERSISTED_RESOURCE_AND_SOURCE_REQUIRED",
            (await Gate(r).InspectAsync(Intent())).Code);
    }

    [Fact]
    public async Task RevocationOrNoCurrentSessionMembershipRbacOwnershipDenied()
    {
        var r = new Reader();
        foreach (var evidence in new[] {
            Verified() with { SessionOrRoleRevoked = true },
            Verified() with { CurrentBackendJwtVerified = false },
            Verified() with { CurrentMembershipVerified = false },
            Verified() with { CurrentRelationalRbacVerified = false },
            Verified() with { PersistedOwnershipVerified = false },
            Verified() with { ActorId = 0 },
            Verified() with { TenantId = 0 }
        })
        {
            r.Context = evidence;
            Assert.Equal("CURRENT_BACKEND_JWT_RBAC_MEMBERSHIP_OWNERSHIP_REQUIRED",
                (await Gate(r).InspectAsync(Intent())).Code);
        }
    }

    [Fact]
    public async Task ClientTenantBIsNotAuthorityForTrustedTenantA()
    {
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED",
            (await Gate(new Reader()).InspectAsync(
                Intent() with { ClientTenantId = 22 })).Code);
    }

    [Fact]
    public async Task ItemOwnedByOtherTenantDeniedEvenWithValidActor()
    {
        var result = await Gate(new Reader()).InspectAsync(
            Intent(Good(), Good() with { ResourceTenantId = 22 }));
        Assert.Equal("CROSS_TENANT_OWNERSHIP_DENIED", result.Code);
        Assert.Equal(2, result.ItemsChecked);
    }

    [Fact]
    public async Task ServerCatalogAbsentOrForgedActionCannotPass()
    {
        var r = new Reader { Context = Verified() with { ApprovedActionKeys = null } };
        Assert.Equal("TRUSTED_SERVER_CASE_ACTION_CATALOG_REQUIRED",
            (await Gate(r).InspectAsync(Intent())).Code);
        r.Context = Verified();
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION",
            (await Gate(r).InspectAsync(
                Intent(Good() with { ActionKey = "CHARGE_CARD" }))).Code);
    }

    [Fact]
    public async Task CaseInsensitiveCatalogComparerCannotAuthorizeNoncanonicalKeys()
    {
        // Regression for A5_FAIL P2: the catalog implementation controls Contains()
        // semantics; fail-closed membership must use ordinal comparison regardless
        // of the comparer chosen by the backend collection provider.
        var reader = new Reader
        {
            Context = Verified() with
            {
                ApprovedCaseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "AF01", "AF02" },
                ApprovedActionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "READ_OWN", "PREPARE_ONLY" }
            }
        };
        var gate = Gate(reader);
        var canonical = await gate.InspectAsync(Intent(Good()));
        Assert.Equal("AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED", canonical.Code);
        Assert.False(canonical.GrantsTenantOrResourceAccess);
        var wrongCase = await gate.InspectAsync(Intent(Good() with { CaseKey = "af01" }));
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION", wrongCase.Code);
        Assert.False(wrongCase.GrantsTenantOrResourceAccess);
        var wrongAction = await gate.InspectAsync(Intent(Good() with { ActionKey = "read_own" }));
        Assert.Equal("UNAPPROVED_CASE_OR_ACTION", wrongAction.Code);
        Assert.False(wrongAction.GrantsTenantOrResourceAccess);
        Assert.Equal(3, reader.Reads);
    }

    [Fact]
    public async Task OriginalCommitMismatchDeniedAtDomainBoundary()
    {
        Assert.Equal("EXACT_SOURCE_COMMIT_REQUIRED",
            (await Gate(new Reader()).InspectAsync(
                Intent(Good() with { SourceCommitSha = new string('e', 40) }))).Code);
    }

    [Fact]
    public async Task NullOrOversizedEvidenceBatchFailsClosed()
    {
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            (await Gate(new Reader()).InspectAsync(Intent() with { Items = null })).Code);
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            (await Gate(new Reader()).InspectAsync(
                Intent() with { Items = Array.Empty<V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?>() })).Code);
        var big = Enumerable.Range(0, 65)
            .Select(_ => (V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?)Good()).ToArray();
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID",
            (await Gate(new Reader()).InspectAsync(Intent() with { Items = big })).Code);
    }

    [Fact]
    public async Task PrivateMissingNegativeOrRollbackEvidenceDenied()
    {
        Assert.Equal("UNREDACTED_PRIVATE_EVIDENCE",
            (await Gate(new Reader()).InspectAsync(
                Intent(Good() with { ContainsPrivateData = true }))).Code);
        Assert.Equal("POSITIVE_AND_NEGATIVE_EVIDENCE_REQUIRED",
            (await Gate(new Reader()).InspectAsync(
                Intent(Good() with { NegativeScenarioPresent = false }))).Code);
        Assert.Equal("ROLLBACK_OR_IDEMPOTENCY_GATE_MISSING",
            (await Gate(new Reader()).InspectAsync(
                Intent(Good() with { RollbackDefined = false }))).Code);
    }

    [Fact]
    public async Task ReaderFailureDeniesWithoutGrant()
    {
        var r = new Reader { Error = new InvalidOperationException("transient") };
        var result = await Gate(r).InspectAsync(Intent());
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_SOURCE_UNAVAILABLE", result.Code);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutReadingEvidence()
    {
        var r = new Reader();
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Gate(r).InspectAsync(Intent(), new CancellationToken(true)));
        Assert.Equal(0, r.Reads);
    }
}
