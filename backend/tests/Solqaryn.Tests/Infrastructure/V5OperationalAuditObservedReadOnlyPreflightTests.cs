using Microsoft.Extensions.Logging;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using AuditRules = Solqaryn.Domain.AutomationV5.V5OperationalAuditDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5OperationalAuditObservedReadOnlyPreflightTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Revision = "source-v5-r1";
    private const string WorkId = "C15-05.M03.06";
    private static readonly HashSet<string> Tasks = new(StringComparer.Ordinal)
    {
        "6adbb9fff1548191a1bd5b30d9e9984a",
        "6af16bb9492c8191b8e8c7d606f11507",
        "6adc2e4bd8a48191ac7d8641ed95a1a3",
        "6aeacb7820ac8191b7d768a6f33dfa09",
        "6adc2e55f47c81919f6374e561687076",
        "6af16bbad26881918c283abe06e1f32a"
    };
    private sealed class Provider : IV5OperationalAuditReadOnlyEvidenceProvider
    {
        public bool Throw { get; set; }
        public int Calls { get; private set; }
        public string? TenantSeen { get; private set; }
        public V5OperationalAuditEvidenceSnapshot Snapshot { get; set; } = new(
            "tenant-A", Revision, Sha,
            new HashSet<string>(StringComparer.Ordinal) { WorkId },
            new HashSet<string>(Tasks, StringComparer.Ordinal),
            new[] { new AuditRules.WorkItem(
                WorkId, "LISTO", "6af16bb9492c8191b8e8c7d606f11507",
                Sha, "6aeacb7820ac8191b7d768a6f33dfa09",
                "check:exact-sha", "a5:independent") });
        public ValueTask<V5OperationalAuditEvidenceSnapshot?> ReadScopedAsync(
            string serverTenantId, CancellationToken cancellationToken)
        {
            Calls++;
            TenantSeen = serverTenantId;
            if (Throw) throw new InvalidOperationException("private-provider-token tenant-A");
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<V5OperationalAuditEvidenceSnapshot?>(Snapshot);
        }
    }
    private sealed class CapturingLogger : ILogger<V5OperationalAuditObservedReadOnlyPreflight>
    {
        public List<(LogLevel Level, EventId EventId, string Message, Exception? Error)>
            Entries { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            NoopScope.Instance;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(
            LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((level, eventId, formatter(state, exception), exception));
        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();
            public void Dispose() { }
        }
    }
    private static V5OperationalAuditSecurityGate.Context Context() => new(
        PrincipalId: "user-private", ServerResolvedTenantId: "tenant-A",
        MembershipTenantId: "tenant-A", TargetResourceTenantId: "tenant-A",
        ClientRequestedTenantId: null, EvidenceSha: Sha,
        IsAuthenticated: true, TenantResolvedOnServer: true,
        MembershipActive: true, HasExplicitAuditPermission: true,
        PermissionRevoked: false, IsServiceActor: false,
        ServiceActorGrantVerified: false, RequestedQueryFilterBypass: false,
        RequestedInstitutionalWrite: false);
    private static V5OperationalAuditObservedReadOnlyPreflight Service(
        Provider provider, CapturingLogger logger) => new(provider, logger);

    [Fact]
    public async Task CompleteSnapshotEmitsSafeStructuredReviewWithoutCertifying()
    {
        var provider = new Provider();
        var logger = new CapturingLogger();
        var result = await Service(provider, logger).PrepareAsync(
            Context(), Sha, Revision, Tasks);
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", result.Code);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesA5);
        Assert.False(result.AllowsEventDispatch);
        Assert.Equal("tenant-A", provider.TenantSeen);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(150506, entry.EventId.Id);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("NEEDS_EXTERNAL_VERIFICATION", entry.Message);
        Assert.Null(entry.Error);
        Assert.DoesNotContain("tenant-A", entry.Message);
        Assert.DoesNotContain("user-private", entry.Message);
        Assert.DoesNotContain(Sha, entry.Message);
    }

    [Fact]
    public async Task CrossTenantDeniesBeforeProviderAndRedactsIds()
    {
        var provider = new Provider();
        var logger = new CapturingLogger();
        var result = await Service(provider, logger).PrepareAsync(
            Context() with { MembershipTenantId = "tenant-B" }, Sha, Revision, Tasks);
        Assert.Equal("TENANT_MEMBERSHIP_MISMATCH", result.Code);
        Assert.Equal(0, provider.Calls);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal("V5 operational audit preflight DENIED", entry.Message);
        Assert.DoesNotContain("tenant-B", entry.Message);
        Assert.DoesNotContain("user-private", entry.Message);
        Assert.Null(entry.Error);
    }

    [Fact]
    public async Task ProviderExceptionRedactedAndNeverGrantsA5()
    {
        var provider = new Provider { Throw = true };
        var logger = new CapturingLogger();
        var result = await Service(provider, logger).PrepareAsync(
            Context(), Sha, Revision, Tasks);
        Assert.Equal("EVIDENCE_PROVIDER_UNAVAILABLE", result.Code);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesA5);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(150507, entry.EventId.Id);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("PROVIDER_UNAVAILABLE", entry.Message);
        Assert.DoesNotContain("private-provider-token", entry.Message);
        Assert.DoesNotContain("tenant-A", entry.Message);
        Assert.Null(entry.Error);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutLoggingSensitiveData()
    {
        var provider = new Provider();
        var logger = new CapturingLogger();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Service(provider, logger).PrepareAsync(
                Context(), Sha, Revision, Tasks, cancel.Token));
        Assert.Equal(0, provider.Calls);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(150508, entry.EventId.Id);
        Assert.Contains("CANCELLED", entry.Message);
        Assert.Null(entry.Error);
    }

    [Fact]
    public void MissingDependenciesFailBeforeAudit()
    {
        var provider = new Provider();
        var logger = new CapturingLogger();
        Assert.Throws<ArgumentNullException>(() =>
            new V5OperationalAuditObservedReadOnlyPreflight(null!, logger));
        Assert.Throws<ArgumentNullException>(() =>
            new V5OperationalAuditObservedReadOnlyPreflight(provider, null!));
        Assert.Equal(0, provider.Calls);
        Assert.Empty(logger.Entries);
    }
}