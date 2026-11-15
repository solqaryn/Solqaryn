using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1104ObservabilityAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Start =
        new(2026, 11, 15, 16, 0, 0, TimeSpan.Zero);

    private static C1104VerifiedPrincipal Principal() =>
        new(7, "tenant-A", true, true, true, true, false, true);

    private static C1104ObservabilitySignalDomainRules.PrivateAggregate Aggregate() =>
        new("http_requests", "corr-01", "tenant-A", "tenant-A", true, true,
            Start, Start.AddHours(1), 4, 5,
            new Dictionary<string, string> { ["method_class"] = "GET" });

    private static C1104ObservabilityApplicationService.VerifiedEvidence Evidence() =>
        new(Sha, "tenant-A", "tenant-A", true, true, true, true, true,
            Aggregate());

    private static C1104ObservabilityAuthorizationGate.Request Request() =>
        new("http_requests", "corr-01", Sha);

    private sealed class Sources : IC1104VerifiedPrincipalSource,
        IC1104TenantPartitionedEvidenceSource
    {
        public C1104VerifiedPrincipal? PrincipalValue { get; set; } = Principal();
        public C1104ObservabilityApplicationService.VerifiedEvidence? EvidenceValue { get; set; } =
            Evidence();
        public int Reads { get; private set; }
        public string? RequestedTenant { get; private set; }

        public ValueTask<C1104VerifiedPrincipal?> CurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(PrincipalValue);
        }

        public ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
            string verifiedTenantScope, string signalName, string correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedTenant = verifiedTenantScope;
            Reads++;
            return ValueTask.FromResult(EvidenceValue);
        }
    }

    private static C1104ObservabilityAuthorizationGate Gate(Sources sources) =>
        new(sources, sources);

    [Fact]
    public async Task GoodEvidenceRemainsNonAuthorizingAndRequiresAuthenticatedAdapter()
    {
        var sources = new Sources();
        var result = await Gate(sources).EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresAuthenticatedApiAdapter);
        Assert.False(result.GrantsAccess);
        Assert.False(result.ExposesTelemetry);
        Assert.False(result.WritesData);
        Assert.False(result.CertifiesSlo);
        Assert.False(result.MayOverrideTenant);
        Assert.Equal("tenant-A", sources.RequestedTenant);
        Assert.Equal(1, sources.Reads);
    }

    [Fact]
    public async Task InvalidInputNeverReachesTrustedReader()
    {
        var sources = new Sources();
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Gate(sources).EvaluateAsync(null)).Code);
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Gate(sources).EvaluateAsync(Request() with { SignalName = "raw_tokens" })).Code);
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Gate(sources).EvaluateAsync(Request() with { CorrelationId = "..\r\n" })).Code);
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Gate(sources).EvaluateAsync(Request() with
                { ExpectedSourceSha = Sha.ToUpperInvariant() })).Code);
        Assert.Equal(0, sources.Reads);
    }

    [Fact]
    public async Task MissingOrRevokedJwtMembershipRoleAndRbacDenyBeforeRead()
    {
        var sources = new Sources { PrincipalValue = null };
        Assert.Equal("FRESH_JWT_MEMBERSHIP_RBAC_AUDIT_REQUIRED",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        foreach (var p in new[] {
            Principal() with { JwtVerified = false },
            Principal() with { MembershipActive = false },
            Principal() with { RoleActive = false },
            Principal() with { ExplicitReadPermissionCurrent = false },
            Principal() with { Revoked = true },
            Principal() with { AuditFenceAvailable = false },
            Principal() with { ActorId = 0 }
        })
        {
            sources.PrincipalValue = p;
            Assert.Equal("FRESH_JWT_MEMBERSHIP_RBAC_AUDIT_REQUIRED",
                (await Gate(sources).EvaluateAsync(Request())).Code);
        }
        Assert.Equal(0, sources.Reads);
    }

    [Fact]
    public async Task TenantIdentityMustBeFromTrustedPrincipal()
    {
        var sources = new Sources { PrincipalValue = Principal() with { TenantScope = "tenant-B" } };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        Assert.Equal("tenant-B", sources.RequestedTenant);
        sources.PrincipalValue = Principal() with { TenantScope = null };
        Assert.Equal("FRESH_JWT_MEMBERSHIP_RBAC_AUDIT_REQUIRED",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        Assert.Equal(1, sources.Reads);
    }

    [Fact]
    public async Task ReaderCannotSwitchTenantOrReturnForeignResource()
    {
        var sources = new Sources { EvidenceValue = Evidence() with
            { PersistedResourceTenantId = "tenant-B" } };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with
            { Aggregate = Aggregate() with { ResourceTenant = "tenant-B" } };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with { TrustedActorTenantId = "tenant-B" };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task StaleJwtOrMembershipEvidenceIsDenied()
    {
        var sources = new Sources { EvidenceValue = Evidence() with { CurrentJwtVerified = false } };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with { CurrentMembershipVerified = false };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with { CurrentExplicitReadRbacVerified = false };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with { AuditFenceAvailable = false };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task MissingEvidenceNeverAuthorizes()
    {
        var sources = new Sources { EvidenceValue = null };
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Gate(sources).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task StaleSourceShaAndSignalAreDeniedByComposedPreflight()
    {
        var sources = new Sources { EvidenceValue = Evidence() with
            { ObservedSourceSha = new string('b', 40) } };
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with
            { Aggregate = Aggregate() with { SignalName = "db_duration" } };
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await Gate(sources).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task SensitiveLabelsAndNoMeasuredDenominatorFailClosed()
    {
        var sources = new Sources { EvidenceValue = Evidence() with
        {
            Aggregate = Aggregate() with { Labels = new Dictionary<string, string>
                { ["email"] = "somebody@example.com" } }
        } };
        Assert.Equal("LABEL_NOT_ALLOWLISTED",
            (await Gate(sources).EvaluateAsync(Request())).Code);
        sources.EvidenceValue = Evidence() with
        {
            Aggregate = Aggregate() with { Numerator = 0, Denominator = 0 }
        };
        Assert.Equal("NO_MEDIDO", (await Gate(sources).EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeReader()
    {
        var sources = new Sources();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Gate(sources).EvaluateAsync(Request(), cancellation.Token));
        Assert.Equal(0, sources.Reads);
    }

    [Fact]
    public void RequiredTrustedServicesCannotBeNull()
    {
        var sources = new Sources();
        Assert.Throws<ArgumentNullException>(() =>
            new C1104ObservabilityAuthorizationGate(null!, sources));
        Assert.Throws<ArgumentNullException>(() =>
            new C1104ObservabilityAuthorizationGate(sources, null!));
    }
}
