using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0102SafeCapabilityAuditReaderTests
{
    private sealed class Source : IC0102ReadThroughCapabilityEvidenceSource
    {
        public Fence.VerifiedResourcePolicy? Value;
        public Exception? Failure;
        public int Reads;
        public ValueTask<Fence.VerifiedResourcePolicy?> ReadUntrustedAsync(
            int tenantId, int resourceId, CancellationToken cancellationToken)
        {
            Reads++;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class Capture : ILogger<C0102SafeCapabilityAuditReader>
    {
        public readonly List<(int Id, string Text, Exception? Error)> Entries = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => EmptyScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((id.Id, formatter(state, error), error));
        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new();
            public void Dispose() { }
        }
    }

    private static Fence.VerifiedResourcePolicy Evidence()
    {
        var capability = new HashSet<string>(StringComparer.Ordinal) { "orders.read" };
        var p = new Rules.VerifiedPolicy(
            11, 11, 7, 19, 3, 3, "pro", "retail",
            true, true, true, false, true, true, true,
            capability, capability, capability, capability,
            new HashSet<string>(StringComparer.Ordinal) { "security.override" });
        return new Fence.VerifiedResourcePolicy(101, 11, 7, true, true, p);
    }

    private static C0102SafeCapabilityAuditReader Create(Source src, Capture log) =>
        new(new C0102ReadThroughCapabilityProvider(src), log);

    [Fact]
    public async Task InvalidIdsAreDeniedWithoutReachingSourceOrLeakingIds()
    {
        var source = new Source { Value = Evidence() };
        var logs = new Capture();
        var reader = Create(source, logs);
        Assert.Null(await reader.ReadCurrentAsync(-9999, 101, default));
        Assert.Null(await reader.ReadCurrentAsync(11, -9999, default));
        Assert.Equal(0, source.Reads);
        Assert.Equal(2, logs.Entries.Count);
        Assert.All(logs.Entries, e => {
            Assert.Equal(C0102SafeCapabilityAuditReader.InvalidInputEventId, e.Id);
            Assert.Equal("C0102_AUDIT_INVALID_INPUT", e.Text);
            Assert.Null(e.Error);
            Assert.DoesNotContain("9999", e.Text);
        });
    }

    [Fact]
    public async Task PositiveEvidenceLogsOnlyNonAuthoritativeSafeCode()
    {
        var source = new Source { Value = Evidence() };
        var logs = new Capture();
        var reader = Create(source, logs);
        var result = await reader.ReadCurrentAsync(11, 101, default);
        Assert.NotNull(result);
        Assert.False(result.OwnershipReadFromTrustedPersistence);
        Assert.False(result.CurrentCapabilityCatalogReadFromBackend);
        Assert.False(result.Policy!.ExplicitRelationalRbacGrant);
        Assert.Equal(1, source.Reads);
        var single = Assert.Single(logs.Entries);
        Assert.Equal(C0102SafeCapabilityAuditReader.UntrustedEvidenceEventId, single.Id);
        Assert.Equal("C0102_AUDIT_UNTRUSTED_EVIDENCE_ONLY", single.Text);
        Assert.Null(single.Error);
    }

    [Fact]
    public async Task SecretBearingSourceErrorIsOpaqueAndNeverLogged()
    {
        var source = new Source { Failure = new InvalidOperationException("PRIVATE_JWT_TENANT_B_SECRET") };
        var logs = new Capture();
        var reader = Create(source, logs);
        Assert.Null(await reader.ReadCurrentAsync(11, 101, default));
        var entry = Assert.Single(logs.Entries);
        Assert.Equal(C0102SafeCapabilityAuditReader.EvidenceUnavailableEventId, entry.Id);
        Assert.Equal("C0102_AUDIT_NO_EVIDENCE", entry.Text);
        Assert.DoesNotContain("PRIVATE_JWT", entry.Text);
        Assert.Null(entry.Error);
    }

    [Fact]
    public async Task RevocationRechecksEachCallAndAuditsDenialWithoutCachedAllow()
    {
        var source = new Source { Value = Evidence() };
        var logs = new Capture();
        var reader = Create(source, logs);
        Assert.NotNull(await reader.ReadCurrentAsync(11, 101, default));
        source.Value = null;
        Assert.Null(await reader.ReadCurrentAsync(11, 101, default));
        Assert.Equal(2, source.Reads);
        Assert.Equal(new[] {
            C0102SafeCapabilityAuditReader.UntrustedEvidenceEventId,
            C0102SafeCapabilityAuditReader.EvidenceUnavailableEventId
        }, logs.Entries.Select(e => e.Id).ToArray());
    }

    [Fact]
    public async Task CancellationPropagatesWithoutFalseDenialAudit()
    {
        var source = new Source { Value = Evidence() };
        var logs = new Capture();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var reader = Create(source, logs);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await reader.ReadCurrentAsync(11, 101, cancel.Token));
        Assert.Empty(logs.Entries);
        Assert.Equal(0, source.Reads);
    }

    [Fact]
    public void MissingDependenciesFailBeforeInvocation()
    {
        var s = new Source();
        var l = new Capture();
        Assert.Throws<ArgumentNullException>(() =>
            new C0102SafeCapabilityAuditReader(null!, l));
        Assert.Throws<ArgumentNullException>(() =>
            new C0102SafeCapabilityAuditReader(new C0102ReadThroughCapabilityProvider(s), null!));
    }
}
