using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityBoundedReadWorker;
using Sink = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityStructuredAuditSink;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// C11-04.M05.03: real Microsoft.Extensions.Logging provider/LoggerFactory
/// pipeline in-process. All tenants/permissions are synthetic, not HTTP, JWT,
/// MySQL, a hosted collector, durable audit or live telemetry.
/// </summary>
public sealed class C1104ObservabilityLoggerProviderIntegrationTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Secret = "JWT_PRIVATE_TENANT_B_EMAIL@example.invalid";

    private sealed class TrustedAuthority : IC1104ObservabilityAuditEvidenceSource
    {
        public Sink.TrustedContext? Current = new(Sha, Sha, true, true, true, true, true);
        public bool Throws;
        public int ReadCount;
        public ValueTask<Sink.TrustedContext?> ReadCurrentAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ReadCount++;
            if (Throws) throw new InvalidOperationException(Secret);
            return ValueTask.FromResult(Current);
        }
    }

    private sealed record LoggedEntry(
        LogLevel Level, EventId EventId, string Category,
        IReadOnlyDictionary<string, object?> Fields, string Formatted);

    private sealed class InMemoryProvider : ILoggerProvider
    {
        public readonly List<LoggedEntry> Entries = [];
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);
        public void Dispose() { }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();
            public void Dispose() { }
        }

        private sealed class CapturingLogger(InMemoryProvider provider, string category) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
                NoopScope.Instance;
            public bool IsEnabled(LogLevel level) => true;

            public void Log<TState>(LogLevel level, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var fields = state is IEnumerable<KeyValuePair<string, object?>> items
                    ? items.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal)
                    : new Dictionary<string, object?>(StringComparer.Ordinal);
                provider.Entries.Add(new(level, eventId, category, fields,
                    formatter(state, exception)));
            }
        }
    }

    private static Worker.Review Good(int count = 2) =>
        new("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", count);

    private static (ILoggerFactory Factory, InMemoryProvider Provider) Pipeline(LogLevel minimum)
    {
        var provider = new InMemoryProvider();
        var factory = LoggerFactory.Create(builder =>
            builder.SetMinimumLevel(minimum).AddProvider(provider));
        return (factory, provider);
    }

    [Fact]
    public async Task RealLoggerFactoryProviderEmitsOnlyFixedStructuredFields()
    {
        var (factory, provider) = Pipeline(LogLevel.Trace);
        using (factory)
        {
            var authority = new TrustedAuthority();
            var receipt = await new Sink(authority, factory.CreateLogger<Sink>()).WriteAsync(Good());
            Assert.True(receipt.LoggerAcceptedEvent);
            Assert.False(receipt.GrantsTenantRead);
            Assert.False(receipt.ExposesMetricsOrPayload);
            Assert.False(receipt.CertifiesDurableAudit);
            Assert.False(receipt.CertifiesSloOrCost);
            Assert.Equal(1, authority.ReadCount);

            var entry = Assert.Single(provider.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal(110406, entry.EventId.Id);
            Assert.Contains(nameof(C1104ObservabilityStructuredAuditSink), entry.Category);
            Assert.Equal("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", entry.Fields["AuditCode"]);
            Assert.Equal(2, entry.Fields["InspectedCount"]);
            Assert.Equal(3, entry.Fields.Count); // Includes OriginalFormat, no tenant/actor/JWT.
            Assert.Contains("{OriginalFormat}", entry.Fields.Keys);
            Assert.DoesNotContain(Sha, entry.Formatted);
            Assert.DoesNotContain(Secret, entry.Formatted);
            Assert.DoesNotContain("tenant", entry.Formatted, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("jwt", entry.Formatted, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DenialUsesWarningEventWithoutPrivateLabels()
    {
        var (factory, provider) = Pipeline(LogLevel.Trace);
        using (factory)
        {
            var receipt = await new Sink(new TrustedAuthority(), factory.CreateLogger<Sink>())
                .WriteAsync(new Worker.Review("OBSERVABILITY_DUPLICATE_TICKET", 0));
            Assert.True(receipt.LoggerAcceptedEvent);
            var record = Assert.Single(provider.Entries);
            Assert.Equal(LogLevel.Warning, record.Level);
            Assert.Equal(110406, record.EventId.Id);
            Assert.Equal("OBSERVABILITY_DUPLICATE_TICKET", record.Fields["AuditCode"]);
            Assert.Equal(0, record.Fields["InspectedCount"]);
        }
    }

    [Theory]
    [InlineData("tenant_b")]
    [InlineData("membership_revoked")]
    [InlineData("rbac_revoked")]
    [InlineData("audit_revoked")]
    [InlineData("sha_mismatch")]
    public async Task TenantBoundaryRevocationAndShaMismatchProduceNoLog(string threat)
    {
        var (factory, provider) = Pipeline(LogLevel.Trace);
        using (factory)
        {
            var authority = new TrustedAuthority();
            authority.Current = threat switch
            {
                "tenant_b" => authority.Current! with { TenantPartitionMatches = false },
                "membership_revoked" => authority.Current! with { MembershipCurrent = false },
                "rbac_revoked" => authority.Current! with { ExplicitReadPermissionCurrent = false },
                "audit_revoked" => authority.Current! with { AuditFenceCurrent = false },
                _ => authority.Current! with { ObservedSourceSha = new string('b', 40) }
            };
            var receipt = await new Sink(authority, factory.CreateLogger<Sink>())
                .WriteAsync(Good());
            Assert.Equal("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED", receipt.Code);
            Assert.False(receipt.LoggerAcceptedEvent);
            Assert.Empty(provider.Entries);
        }
    }

    [Theory]
    [InlineData(-1, "OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API")]
    [InlineData(0, "OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API")]
    [InlineData(17, "OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API")]
    [InlineData(1, "FORGED_JWT_PRIVATE_TENANT")]
    [InlineData(1, "OBSERVABILITY_SOURCE_REJECTED")]
    public async Task ForgedCodeOrOutsideBoundsNeverTouchesSourceOrProvider(int count, string code)
    {
        var (factory, provider) = Pipeline(LogLevel.Trace);
        using (factory)
        {
            var authority = new TrustedAuthority();
            var receipt = await new Sink(authority, factory.CreateLogger<Sink>())
                .WriteAsync(new Worker.Review(code, count));
            Assert.Equal("UNTRUSTED_OR_INVALID_AUDIT_RESULT", receipt.Code);
            Assert.Equal(0, authority.ReadCount);
            Assert.Empty(provider.Entries);
        }
    }

    [Fact]
    public async Task RealLogFilterPreventsFalsePositiveSuccess()
    {
        var (factory, provider) = Pipeline(LogLevel.Warning);
        using (factory)
        {
            var authority = new TrustedAuthority();
            var receipt = await new Sink(authority, factory.CreateLogger<Sink>()).WriteAsync(Good());
            Assert.Equal("AUDIT_LOGGER_UNAVAILABLE", receipt.Code);
            Assert.Equal(0, authority.ReadCount);
            Assert.Empty(provider.Entries);
        }
    }

    [Fact]
    public async Task CancelledOrFaultingAuthorityCannotLeakRawExceptionsToProvider()
    {
        var (factory, provider) = Pipeline(LogLevel.Trace);
        using (factory)
        {
            var authority = new TrustedAuthority { Throws = true };
            var sink = new Sink(authority, factory.CreateLogger<Sink>());
            var denied = await sink.WriteAsync(Good());
            Assert.Equal("TRUSTED_AUDIT_EVIDENCE_UNAVAILABLE", denied.Code);
            Assert.Empty(provider.Entries);

            using var cancel = new CancellationTokenSource();
            cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await sink.WriteAsync(Good(), cancel.Token));
            Assert.Equal(1, authority.ReadCount);
            Assert.Empty(provider.Entries);
        }
    }
}
