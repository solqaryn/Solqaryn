using Solqaryn.Application.AutomationV5;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C02-02.M05.05: adversarial isolation and provider-fault acceptance.
/// All test evidence is synthetic; no database connections are granted.
/// </summary>
public sealed class C0202M0505PlacementNegativeSecurityTests
{
    private const string Tenant = "tenant-A";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static DomainRules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = Tenant,
        AuthenticatedActorId = "actor-A",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = Tenant,
        CurrentAssignmentRevision = 17,
        CurrentBindingAlias = "shared-A",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static C0202PlacementApplicationGate.Evidence Evidence() => new(
        Sha, Tenant, Tenant, "dedicated-A",
        true, true, true, true, true, true, true, true, true, true, Snapshot());

    private static C0202PlacementApplicationGate.Request Request() => new(
        Tenant, Sha, DomainRules.Intent.ProposeMigration,
        DomainRules.PlacementMode.Dedicated, "dedicated-A", 17, "intent-A-17");

    private sealed class Reader(C0202PlacementApplicationGate.Evidence? evidence,
        Exception? failure = null) : IC0202PlacementEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            Calls++;
            if (failure is not null)
                return ValueTask.FromException<C0202PlacementApplicationGate.Evidence?>(failure);
            return ValueTask.FromResult(evidence);
        }
    }

    private sealed class CancelDuringRead(CancellationTokenSource source)
        : IC0202PlacementEvidenceReader
    {
        public ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            source.Cancel();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static void NoStorageAuthority(C0202PlacementApplicationGate.Result result)
    {
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesProduction);
    }

    [Fact]
    public async Task ProviderFailureIsSanitizedWithoutLeakingDiagnosticOrAuthority()
    {
        foreach (Exception failure in new Exception[]
        {
            new InvalidOperationException("synthetic DSN and internal credentials"),
            new IOException("synthetic tenant-B provider failure")
        })
        {
            var reader = new Reader(Evidence(), failure);
            var result = await new C0202PlacementApplicationGate(reader)
                .EvaluateAsync(Request());
            Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", result.Code);
            Assert.DoesNotContain("DSN", result.Code, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("tenant-B", result.Code, StringComparison.Ordinal);
            Assert.Equal(1, reader.Calls);
            NoStorageAuthority(result);
        }
    }

    [Fact]
    public async Task UnrequestedProviderCancellationIsFailClosed()
    {
        var reader = new Reader(Evidence(), new OperationCanceledException("provider canceled itself"));
        var result = await new C0202PlacementApplicationGate(reader).EvaluateAsync(Request());
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(1, reader.Calls);
        NoStorageAuthority(result);
    }

    [Fact]
    public async Task CallerCancellationDuringReadIsNotSwallowed()
    {
        using var source = new CancellationTokenSource();
        var gate = new C0202PlacementApplicationGate(new CancelDuringRead(source));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await gate.EvaluateAsync(Request(), source.Token));
        Assert.True(source.IsCancellationRequested);
    }

    [Fact]
    public async Task TenantAndSnapshotInconsistencyAlwaysDeny()
    {
        var variants = new[]
        {
            Evidence() with { VerifiedTenantId = "tenant-B" },
            Evidence() with { PersistedOwnerTenantId = "tenant-B" },
            Evidence() with { Snapshot = Snapshot() with { VerifiedTenantId = "tenant-B" } },
            Evidence() with { Snapshot = Snapshot() with { AssignmentOwnerTenantId = "tenant-B" } },
            Evidence() with { SourceSha = new string('b', 40) }
        };
        foreach (var evidence in variants)
        {
            var result = await new C0202PlacementApplicationGate(new Reader(evidence))
                .EvaluateAsync(Request());
            Assert.NotEqual("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
            NoStorageAuthority(result);
        }
    }

    [Fact]
    public async Task RevocationAndLostRoleDenyEvenWhenProviderClaimsRestoreEvidence()
    {
        var variants = new[]
        {
            Evidence() with { CurrentJwtAndMembershipVerified = false },
            Evidence() with { FreshRelationalRbacVerified = false },
            Evidence() with { Snapshot = Snapshot() with { TenantSuspendedOrRevoked = true } },
            Evidence() with { Snapshot = Snapshot() with { CurrentRoleCanReviewPlacement = false } },
            Evidence() with { CurrentRevisionFenceVerified = false }
        };
        foreach (var evidence in variants)
        {
            var result = await new C0202PlacementApplicationGate(new Reader(evidence))
                .EvaluateAsync(Request());
            Assert.NotEqual("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
            NoStorageAuthority(result);
        }
    }

    [Fact]
    public async Task MaliciousDsnFallbackAndBindingAliasNeverSelectInfrastructure()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        foreach (var attack in new[]
        {
            Request() with { ClientSuppliesDsn = true },
            Request() with { CrossTenantFallback = true },
            Request() with { RequestsLiveCutoverOrDualWrite = true },
            Request() with { RequestsDestructiveSchema = true }
        })
        {
            var result = await gate.EvaluateAsync(attack);
            Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION", result.Code);
            NoStorageAuthority(result);
        }
        Assert.Equal(0, reader.Calls);
        var forgedAlias = await gate.EvaluateAsync(
            Request() with { TargetBindingAlias = "other-tenant-dedicated" });
        Assert.Equal("TARGET_BINDING_NOT_SERVER_APPROVED", forgedAlias.Code);
        Assert.Equal(1, reader.Calls);
        NoStorageAuthority(forgedAlias);
    }

    [Fact]
    public async Task ValidPreflightIsStillNonAuthorizing()
    {
        var result = await new C0202PlacementApplicationGate(new Reader(Evidence()))
            .EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedStorageAdapter);
        NoStorageAuthority(result);
    }
}
