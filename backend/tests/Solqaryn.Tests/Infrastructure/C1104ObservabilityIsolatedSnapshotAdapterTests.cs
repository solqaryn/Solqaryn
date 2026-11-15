using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Adapter = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityIsolatedSnapshotAdapter;
using Gate = Solqaryn.Application.AutomationV5.C1104ObservabilityAuthorizationGate;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1104ObservabilityIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Time =
        new(2026, 11, 15, 18, 0, 0, TimeSpan.Zero);
    private static C1104VerifiedPrincipal Principal() =>
        new(7, "tenant-A", true, true, true, true, false, true);
    private static C1104ObservabilitySignalDomainRules.PrivateAggregate Aggregate() =>
        new("http_requests", "corr-01", "tenant-A", "tenant-A", true, true,
            Time, Time.AddHours(1), 4, 5,
            new Dictionary<string, string> { ["method_class"] = "GET" });
    private static C1104ObservabilityApplicationService.VerifiedEvidence Evidence() =>
        new(Sha, "tenant-A", "tenant-A", true, true, true, true, true,
            Aggregate());
    private static Gate.Request Request() => new("http_requests", "corr-01", Sha);
    private static Adapter Create(C1104VerifiedPrincipal? principal = null,
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence = null) =>
        new("tenant-A", Sha, principal ?? Principal(), evidence ?? Evidence());
    private static Task<Adapter.Outcome> Review(Adapter adapter, Gate.Request? request = null,
        string tenant = "tenant-A", string sha = Sha,
        CancellationToken ct = default) =>
        adapter.InspectAsync(request ?? Request(), tenant, sha, ct);

    [Fact] public async Task NominalSnapshotStillGrantsNoTelemetryOrSlo()
    {
        var result = await Review(Create());
        Assert.Equal("AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedApi);
        Assert.False(result.GrantsTenantRead);
        Assert.False(result.ExposesTelemetry);
        Assert.False(result.WritesDatabase);
        Assert.False(result.ExportsMetrics);
        Assert.False(result.CertifiesSloOrCost);
        Assert.False(result.HasRealJwtAuthentication);
    }

    [Fact] public async Task TenantBAndStaleSourceShaDenyBeforeReader()
    {
        Assert.Equal("ISOLATED_OBSERVABILITY_TENANT_OR_SHA_MISMATCH",
            (await Review(Create(), tenant: "tenant-B")).Code);
        Assert.Equal("ISOLATED_OBSERVABILITY_TENANT_OR_SHA_MISMATCH",
            (await Review(Create(), sha: new string('f', 40))).Code);
        Assert.Equal("ISOLATED_OBSERVABILITY_TENANT_OR_SHA_MISMATCH",
            (await Review(Create(), Request() with { ExpectedSourceSha = new string('f', 40) })).Code);
    }

    [Fact] public async Task ForeignPrincipalTenantCannotLaunderEvidence()
    {
        var adapter = Create(principal: Principal() with { TenantScope = "tenant-B" });
        Assert.Equal("ISOLATED_OBSERVABILITY_TENANT_OR_SHA_MISMATCH",
            (await Review(adapter)).Code);
    }

    [Fact] public async Task RevokedMembershipAndMissingRbacStayDenied()
    {
        foreach (var p in new[]
        {
            Principal() with { JwtVerified = false },
            Principal() with { MembershipActive = false },
            Principal() with { RoleActive = false },
            Principal() with { ExplicitReadPermissionCurrent = false },
            Principal() with { Revoked = true }
        })
            Assert.Equal("FRESH_JWT_MEMBERSHIP_RBAC_AUDIT_REQUIRED",
                (await Review(Create(principal: p))).Code);
    }

    [Fact] public async Task StaleJwtAndForeignResourceInSnapshotDenied()
    {
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Review(Create(evidence: Evidence() with { CurrentJwtVerified = false }))).Code);
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Review(Create(evidence: Evidence() with
            { PersistedResourceTenantId = "tenant-B" }))).Code);
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Review(Create(evidence: Evidence() with
            { Aggregate = Aggregate() with { ResourceTenant = "tenant-B" } }))).Code);
    }

    [Fact] public async Task NoMeasuredDenominatorIsNotSloPass()
    {
        var adapter = Create(evidence: Evidence() with
        { Aggregate = Aggregate() with { Numerator = 0, Denominator = 0 } });
        Assert.Equal("NO_MEDIDO", (await Review(adapter)).Code);
    }

    [Fact] public async Task PiiLabelAndUnsafeRawSignalCannotBeExported()
    {
        var adapter = Create(evidence: Evidence() with
        { Aggregate = Aggregate() with
            { Labels = new Dictionary<string,string> { ["email"] = "private@example.com" } } });
        Assert.Equal("LABEL_NOT_ALLOWLISTED", (await Review(adapter)).Code);
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Review(Create(), Request() with { SignalName = "raw_credentials" })).Code);
    }

    [Fact] public async Task SnapshotDeepCopyCannotBeMutatedAfterConstruction()
    {
        var original = new Dictionary<string,string> { ["method_class"] = "GET" };
        var adapter = Create(evidence: Evidence() with
        { Aggregate = Aggregate() with { Labels = original } });
        original["method_class"] = "private@example.com";
        original["email"] = "exfiltrate";
        Assert.Equal("AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED",
            (await Review(adapter)).Code);
    }

    [Fact] public async Task WrongSignalCorrelationAndMissingAggregateDenied()
    {
        Assert.Equal("INVALID_SIGNAL_CORRELATION_OR_SOURCE",
            (await Review(Create(), Request() with { CorrelationId = "\n" })).Code);
        Assert.Equal("TENANT_EVIDENCE_OR_REVOCATION_MISMATCH",
            (await Review(Create(evidence: Evidence() with { Aggregate = null }))).Code);
    }

    [Fact] public async Task CancellationFailsBeforeAnySnapshotResult()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Review(Create(), ct: cts.Token));
    }

    [Fact] public void UnsafeConstructorTenantOrShaIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new Adapter("../tenant", Sha, Principal(), Evidence()));
        Assert.Throws<ArgumentException>(() =>
            new Adapter("tenant-A", "BAD", Principal(), Evidence()));
    }
}