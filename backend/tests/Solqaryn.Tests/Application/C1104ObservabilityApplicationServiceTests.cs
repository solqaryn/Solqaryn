using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1104ObservabilityApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Start =
        new(2026, 11, 15, 16, 0, 0, TimeSpan.Zero);

    private static C1104ObservabilitySignalDomainRules.PrivateAggregate Aggregate() =>
        new("http_requests", "corr-01", "tenant-A", "tenant-A", true, true,
            Start, Start.AddHours(1), 4, 5,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["method_class"] = "GET", ["status_class"] = "2xx"
            });

    private static C1104ObservabilityApplicationService.VerifiedEvidence Evidence() =>
        new(Sha, "tenant-A", "tenant-A", true, true, true, true, true,
            Aggregate());

    private static C1104ObservabilityApplicationService.Request Request() =>
        new("http_requests", "corr-01", Sha);

    private sealed class Reader(
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence)
        : IC1104ObservabilityEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
            string signal, string correlation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(evidence);
        }
    }

    private static async Task<C1104ObservabilityApplicationService.Result> Evaluate(
        C1104ObservabilityApplicationService.Request? request = null,
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence = null)
    {
        var service = new C1104ObservabilityApplicationService(
            new Reader(evidence ?? Evidence()));
        return await service.PrepareAsync(request ?? Request());
    }

    [Fact]
    public async Task ValidPreflightStillRequiresAuthenticatedAdapter()
    {
        var result = await Evaluate();
        Assert.Equal("TRUSTED_PRIVATE_OBSERVABILITY_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresAuthenticatedReadAdapter);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.ExportsTelemetry);
        Assert.False(result.CertifiesSloOrCost);
        Assert.False(result.WritesMetricsOrDatabase);
    }

    [Fact]
    public async Task MissingRequestOrInvalidShaFailsBeforeReader()
    {
        var reader = new Reader(Evidence());
        var service = new C1104ObservabilityApplicationService(reader);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await service.PrepareAsync(null)).Code);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await service.PrepareAsync(Request() with { ExpectedSourceSha = "short" })).Code);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await service.PrepareAsync(Request() with { SignalName = "" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientOverridesDoNotReachEvidenceReader()
    {
        var reader = new Reader(Evidence());
        var service = new C1104ObservabilityApplicationService(reader);
        Assert.Equal("UNTRUSTED_TENANT_RAW_OR_EXPORT_OVERRIDE",
            (await service.PrepareAsync(Request() with { ClientTenantOverride = true })).Code);
        Assert.Equal("UNTRUSTED_TENANT_RAW_OR_EXPORT_OVERRIDE",
            (await service.PrepareAsync(Request() with { RequestsRawPayload = true })).Code);
        Assert.Equal("UNTRUSTED_TENANT_RAW_OR_EXPORT_OVERRIDE",
            (await service.PrepareAsync(Request() with
                { RequestsExporterOrSloCertification = true })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task UnknownSignalAndUnsafeCorrelationFailBeforeReader()
    {
        var reader = new Reader(Evidence());
        var service = new C1104ObservabilityApplicationService(reader);
        Assert.Equal("SIGNAL_NOT_ALLOWLISTED",
            (await service.PrepareAsync(Request() with { SignalName = "unknown" })).Code);
        Assert.Equal("UNSAFE_CORRELATION",
            (await service.PrepareAsync(Request() with { CorrelationId = "ok\\r\\nsecret" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ForeignOwnerOrUntrustedActorTenantIsDenied()
    {
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { PersistedResourceTenantId = "tenant-B" })).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { TrustedActorTenantId = "tenant-B" })).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { ResourceTenant = "tenant-B" } })).Code);
    }

    [Fact]
    public async Task RevokedMembershipOrMissingRbacIsDenied()
    {
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { CurrentMembershipVerified = false })).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { CurrentExplicitReadRbacVerified = false })).Code);
        Assert.Equal("MEMBERSHIP_REVOKED",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { MembershipActive = false } })).Code);
    }

    [Fact]
    public async Task JwtAndAuditFenceAreMandatory()
    {
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { CurrentJwtVerified = false })).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { FromTrustedBackend = false })).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { AuditFenceAvailable = false })).Code);
    }

    [Fact]
    public async Task WrongShaSignalOrCorrelationIsRejected()
    {
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { ObservedSourceSha = new string('a', 40) })).Code);
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { SignalName = "request_latency" } })).Code);
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { CorrelationId = "other" } })).Code);
    }

    [Fact]
    public async Task UnsafeSignalAndSensitiveLabelAreDeniedByDomain()
    {
        Assert.Equal("SIGNAL_NOT_ALLOWLISTED",
            (await Evaluate(request: Request() with { SignalName = "secret_signal" },
                evidence: Evidence() with
                { Aggregate = Aggregate() with { SignalName = "secret_signal" } })).Code);
        Assert.Equal("LABEL_NOT_ALLOWLISTED",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with
                    { Labels = new Dictionary<string, string> { ["user_email"] = "a@b" } } })).Code);
    }

    [Fact]
    public async Task ZeroDenominatorNeverCertifiesSlo()
    {
        var result = await Evaluate(evidence: Evidence() with
        {
            Aggregate = Aggregate() with { Numerator = 0, Denominator = 0 }
        });
        Assert.Equal("NO_MEDIDO", result.Code);
        Assert.False(result.CertifiesSloOrCost);
    }

    [Fact]
    public async Task InvalidWindowAndAggregateBoundaryAreDenied()
    {
        Assert.Equal("INVALID_MEASUREMENT_WINDOW",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { WindowEndUtc = Start.AddDays(2) } })).Code);
        Assert.Equal("INVALID_AGGREGATE_COUNTS",
            (await Evaluate(evidence: Evidence() with
                { Aggregate = Aggregate() with { Numerator = 6 } })).Code);
    }

    [Fact]
    public async Task NullEvidenceDoesNotGrantAccess()
    {
        var reader = new Reader(null);
        var service = new C1104ObservabilityApplicationService(reader);
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED",
            (await service.PrepareAsync(Request())).Code);
    }

    [Fact]
    public async Task CancellationIsPropagatedBeforeExternalRead()
    {
        var reader = new Reader(Evidence());
        var service = new C1104ObservabilityApplicationService(reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.PrepareAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
