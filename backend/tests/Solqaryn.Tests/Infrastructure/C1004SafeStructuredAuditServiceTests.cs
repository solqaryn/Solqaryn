using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1004SafeStructuredAuditServiceTests
{
    private static C1004SafeStructuredAuditService.VerifiedEvidence Valid() =>
        new(7, 11, 11, true, true, true, true, false, true, true,
            "trusted-12345678", "0123456789abcdef0123456789abcdef01234567");

    private sealed class Source : IC1004CurrentAuditAuthorityReader
    {
        public C1004SafeStructuredAuditService.VerifiedEvidence? Value = Valid();
        public Exception? Error;
        public int Calls;
        public ValueTask<C1004SafeStructuredAuditService.VerifiedEvidence?> ReadCurrentAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class Capture : ILogger<C1004SafeStructuredAuditService>
    {
        private sealed class Noop : IDisposable { public void Dispose() {} }
        public List<string> Messages { get; } = new();
        public bool ThrowOnLog;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Noop();
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnLog) throw new InvalidOperationException("sink confidential");
            Messages.Add(formatter(state, exception));
        }
    }
    private static C1004SafeStructuredAuditService Service(Source s, Capture c) => new(s, c);
    private static C1004SafeStructuredAuditService.Request Intent() =>
        new(C1004SafeStructuredAuditService.AuditOperation.JobReview, 11);

    [Fact]
    public async Task ValidCurrentScopeEmitsOnlySafeStructuredOperation()
    {
        var source = new Source(); var logger = new Capture();
        var d = await Service(source, logger).AuditAsync(Intent());
        Assert.Equal("EXTERNAL_DURABLE_AUDIT_ADAPTER_REQUIRED", d.Code);
        Assert.True(d.EventSubmittedToLogger);
        Assert.False(d.CertifiesDurableAudit);
        Assert.False(d.GrantsTenantAccess);
        Assert.False(d.ExecutesWorkerOrProviderWrite);
        Assert.Single(logger.Messages);
        Assert.Contains("JobReview", logger.Messages[0]);
        Assert.Contains("trusted-12345678", logger.Messages[0]);
        Assert.DoesNotContain("tenant=11", logger.Messages[0]);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public async Task InvalidOrClientElevatedIntentDoesNotReadAuthority()
    {
        var source = new Source(); var logger = new Capture();
        foreach (var request in new C1004SafeStructuredAuditService.Request?[] {
            null, Intent() with { ExpectedTenantId = 0 },
            Intent() with { ClaimsClientAuthority = true },
            Intent() with { RequestsWorkerOrProviderWrite = true },
            Intent() with { ContainsSensitiveClientPayload = true },
            Intent() with { Operation = (C1004SafeStructuredAuditService.AuditOperation)999 }
        })
            Assert.Equal("UNTRUSTED_AUDIT_INTENT_DENIED",
                (await Service(source, logger).AuditAsync(request)).Code);
        Assert.Equal(0, source.Calls);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task CrossTenantAAndBNeverProduceAJobOrEvent()
    {
        var source = new Source { Value = Valid() with { ResourceTenantId = 22 } };
        var logger = new Capture();
        Assert.Equal("CURRENT_VERIFIED_TENANT_AUDIT_SCOPE_REQUIRED",
            (await Service(source, logger).AuditAsync(Intent())).Code);
        source.Value = Valid();
        Assert.Equal("CURRENT_VERIFIED_TENANT_AUDIT_SCOPE_REQUIRED",
            (await Service(source, logger).AuditAsync(Intent() with { ExpectedTenantId = 22 })).Code);
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task RevokedMembershipRoleAndServiceGrantFailClosed()
    {
        var source = new Source(); var logger = new Capture();
        foreach (var v in new[] {
            Valid() with { GrantRevoked = true },
            Valid() with { MembershipCurrent = false },
            Valid() with { RelationalRoleVerified = false },
            Valid() with { ServiceScopeCurrent = false },
            Valid() with { TenantActive = false },
            Valid() with { BackendIdentityVerified = false },
            Valid() with { ProviderSourceAuthenticated = false }
        })
        {
            source.Value = v;
            Assert.Equal("CURRENT_VERIFIED_TENANT_AUDIT_SCOPE_REQUIRED",
                (await Service(source, logger).AuditAsync(Intent())).Code);
        }
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public async Task ForgedCorrelationOrSourceShaIsNeverLogged()
    {
        var s = new Source(); var c = new Capture();
        foreach (var v in new[] {
            Valid() with { InternalCorrelationId = "secret\nbearer token" },
            Valid() with { InternalCorrelationId = "x" },
            Valid() with { SourceSha = "untrusted" },
            Valid() with { ActorId = 0 },
            Valid() with { TenantId = 0 }
        })
        {
            s.Value = v;
            Assert.Equal("CURRENT_VERIFIED_TENANT_AUDIT_SCOPE_REQUIRED",
                (await Service(s,c).AuditAsync(Intent())).Code);
        }
        Assert.Empty(c.Messages);
    }

    [Fact]
    public async Task BackendFailureIsOpaqueAndDoesNotLogExceptionOrPii()
    {
        var s = new Source { Error = new InvalidOperationException("secret=password") };
        var c = new Capture();
        var result = await Service(s,c).AuditAsync(Intent());
        Assert.Equal("CURRENT_AUDIT_AUTHORITY_UNAVAILABLE", result.Code);
        Assert.Single(c.Messages);
        Assert.DoesNotContain("secret", c.Messages[0]);
        Assert.False(result.EventSubmittedToLogger);
    }

    [Fact]
    public async Task LoggingSinkFailureNeverReturnsAudited()
    {
        var c = new Capture { ThrowOnLog = true };
        var result = await Service(new Source(),c).AuditAsync(Intent());
        Assert.Equal("STRUCTURED_AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.EventSubmittedToLogger);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeReadingOrLogging()
    {
        var s = new Source(); var c = new Capture();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Service(s,c).AuditAsync(Intent(),new CancellationToken(true)));
        Assert.Equal(0,s.Calls); Assert.Empty(c.Messages);
    }

    [Fact]
    public async Task FourExplicitOperationTypesAreBoundedNoExecutableSideEffects()
    {
        var s = new Source(); var c = new Capture();
        foreach (C1004SafeStructuredAuditService.AuditOperation type in
            Enum.GetValues<C1004SafeStructuredAuditService.AuditOperation>())
        {
            var d = await Service(s,c).AuditAsync(Intent() with { Operation = type });
            Assert.True(d.EventSubmittedToLogger);
            Assert.False(d.ExecutesWorkerOrProviderWrite);
        }
        Assert.Equal(4,c.Messages.Count);
    }
}
