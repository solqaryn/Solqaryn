using Xunit;
using Rules = global::Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;
using Service = global::Solqaryn.Application.AutomationV5.C0303SecurityAuditApplicationService;
using global::Solqaryn.Application.AutomationV5;

namespace Solqaryn.Tests.Application;

public sealed class C0303M0302SecurityAuditApplicationServiceTests
{
    private static Service.ServerEvent Actor() => new(
        71, 10, 10, true, true, true, true, false, true,
        false, false, false, Rules.Scope.Enterprise,
        Rules.Action.AuthorizationDenied, Rules.Outcome.Denied);
    private static Service.ClientIntent Intent() => new("audit-tenant-A.01");

    private sealed class EventReader : IC0303VerifiedAuditEventReader
    {
        public Service.ServerEvent? Value { get; set; } = Actor();
        public Exception? Error { get; set; }
        public Action? AfterRead { get; set; }
        public int Calls { get; private set; }
        public ValueTask<Service.ServerEvent?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Error is not null) throw Error;
            AfterRead?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }
    private sealed class ResourceReader : IC0303VerifiedAuditResourceReader
    {
        public Service.ServerResource? Value { get; set; } = new(10, true, true);
        public Exception? Error { get; set; }
        public Action? AfterRead { get; set; }
        public int Calls { get; private set; }
        public int TenantRead { get; private set; }
        public ValueTask<Service.ServerResource?> ReadCurrentAsync(
            int actorId, int tenantId, Rules.Action action, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++; TenantRead = tenantId;
            if (Error is not null) throw Error;
            AfterRead?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }
    private static Service Create(EventReader e, ResourceReader r) => new(e, r);

    [Fact]
    public async Task PositiveTenantAuditReturnsMinimalNonGrantCandidate()
    {
        var e = new EventReader(); var r = new ResourceReader();
        var result = await Create(e, r).PrepareAsync(Intent());
        Assert.Equal("AUDIT_CANDIDATE_REQUIRES_ATOMIC_SERVER_PERSISTENCE", result.Code);
        Assert.True(result.RequiresDurableAuthenticatedAdapter);
        Assert.False(result.AuthorizesAnyScope);
        Assert.False(result.WritesAnyAuditOrBusinessData);
        Assert.False(result.CertifiesLiveJwtHttpMysql);
        Assert.Equal(71, result.Candidate!.ActorId);
        Assert.Equal(10, result.Candidate.TenantId);
        Assert.Equal(Rules.Outcome.Denied, result.Candidate.Outcome);
        Assert.Equal(10, r.TenantRead); Assert.Equal(1, r.Calls);
    }

    [Fact]
    public async Task BadCorrelationAndUntrustedClientOverridesNeverReadServer()
    {
        var e = new EventReader(); var r = new ResourceReader();
        var service = Create(e, r);
        foreach (var intent in new Service.ClientIntent?[]
        {
            null, new(""), new("bad\r\nAuthorization: Bearer secret"),
            new(new string('x', 65)), Intent() with { ClientTenantOverride = true },
            Intent() with { RequestsRawPayload = true },
            Intent() with { RequestsImmediateWrite = true }
        })
        {
            var result = await service.PrepareAsync(intent);
            Assert.Null(result.Candidate);
            Assert.False(result.AuthorizesAnyScope);
        }
        Assert.Equal(0, e.Calls); Assert.Equal(0, r.Calls);
    }

    [Fact]
    public async Task UnsignedJwtRevokedRoleAndUntrustedContextFailBeforeResourceLookup()
    {
        foreach (var actor in new[]
        {
            Actor() with { SignedJwtVerified = false },
            Actor() with { TrustedServerContext = false },
            Actor() with { CurrentAuditRbacVerified = false },
            Actor() with { PermissionRevoked = true },
            Actor() with { TrustedAuditWriter = false },
            Actor() with { ActorId = 0 }
        })
        {
            var r = new ResourceReader();
            var verdict = await Create(new EventReader { Value = actor }, r)
                .PrepareAsync(Intent());
            Assert.Equal("LIVE_SIGNED_JWT_AND_RBAC_REQUIRED", verdict.Code);
            Assert.Equal(0, r.Calls);
        }
    }

    [Fact]
    public async Task TenantBResourceAndRevokedMembershipCannotBeAuditedAsTenantA()
    {
        Assert.Equal("CROSS_TENANT_RESOURCE_DENIED",
            (await Create(new EventReader(), new ResourceReader
            { Value = new(20, true, true) }).PrepareAsync(Intent())).Code);
        Assert.Equal("CURRENT_TENANT_MEMBERSHIP_REQUIRED",
            (await Create(new EventReader { Value = Actor() with
            { MembershipTenantId = 20 } }, new ResourceReader()).PrepareAsync(Intent())).Code);
        Assert.Equal("CURRENT_TENANT_MEMBERSHIP_REQUIRED",
            (await Create(new EventReader { Value = Actor() with
            { MembershipActive = false } }, new ResourceReader()).PrepareAsync(Intent())).Code);
    }

    [Fact]
    public async Task MissingOrUntrustedResourceOwnershipNeverCreatesCandidate()
    {
        foreach (Service.ServerResource? resource in new Service.ServerResource?[]
        { null, new(10, false, true), new(10, true, false) })
        {
            var result = await Create(new EventReader(), new ResourceReader
            { Value = resource }).PrepareAsync(Intent());
            Assert.Equal("CURRENT_PERSISTED_RESOURCE_OWNERSHIP_REQUIRED", result.Code);
            Assert.Null(result.Candidate);
        }
    }

    [Fact]
    public async Task PlatformRequiresExplicitIndependentGrantWithoutTenantFallback()
    {
        var platform = Actor() with
        { Scope = Rules.Scope.Platform, TenantId = null, MembershipTenantId = null,
            MembershipActive = false };
        var r = new ResourceReader();
        Assert.Equal("EXPLICIT_PLATFORM_AUDIT_SCOPE_REQUIRED",
            (await Create(new EventReader { Value = platform }, r)
                .PrepareAsync(Intent())).Code);
        var accepted = await Create(new EventReader { Value = platform with
        { PlatformAuthorityVerified = true, ExplicitPlatformAuditGrant = true } }, r)
            .PrepareAsync(Intent());
        Assert.True(accepted.RequiresDurableAuthenticatedAdapter);
        Assert.Null(accepted.Candidate!.TenantId);
        Assert.False(accepted.AuthorizesAnyScope);
        Assert.Equal(0, r.Calls);
    }

    [Fact]
    public async Task CriticalChangeRequiresTransactionAndDeniedCannotBeReportedAllowed()
    {
        Assert.Equal("ACTION_OUTCOME_MISMATCH",
            (await Create(new EventReader { Value = Actor() with
            { Outcome = Rules.Outcome.Allowed } }, new ResourceReader())
                .PrepareAsync(Intent())).Code);
        var critical = Actor() with { Action = Rules.Action.CriticalChange };
        Assert.Equal("STRICT_TRANSACTIONAL_AUDIT_REQUIRED",
            (await Create(new EventReader { Value = critical }, new ResourceReader())
                .PrepareAsync(Intent())).Code);
        var approved = await Create(new EventReader { Value = critical with
        { TransactionalSinkVerified = true } }, new ResourceReader()).PrepareAsync(Intent());
        Assert.True(approved.RequiresDurableAuthenticatedAdapter);
        Assert.False(approved.WritesAnyAuditOrBusinessData);
    }

    [Fact]
    public async Task ProviderFailuresAreSanitizedAndNeverExposedToClients()
    {
        var brokenAuth = new EventReader { Error = new InvalidOperationException("secret JWT") };
        var r = new ResourceReader();
        Assert.Equal("AUTHENTICATED_AUDIT_SOURCE_UNAVAILABLE",
            (await Create(brokenAuth, r).PrepareAsync(Intent())).Code);
        Assert.Equal(0, r.Calls);
        var brokenResource = new ResourceReader
        { Error = new InvalidOperationException("MySQL password") };
        var result = await Create(new EventReader(), brokenResource).PrepareAsync(Intent());
        Assert.Equal("AUDIT_RESOURCE_SOURCE_UNAVAILABLE", result.Code);
        Assert.Null(result.Candidate);
    }

    [Fact]
    public async Task CancellationAfterEventReadCannotReachOwnershipLookup()
    {
        using var cts = new CancellationTokenSource();
        var e = new EventReader { AfterRead = () => cts.Cancel() };
        var r = new ResourceReader();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { await Create(e, r).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(0, r.Calls);
    }

    [Fact]
    public async Task CancellationAfterResourceReadCannotYieldPositiveCandidate()
    {
        using var cts = new CancellationTokenSource();
        var r = new ResourceReader { AfterRead = () => cts.Cancel() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { await Create(new EventReader(), r).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(1, r.Calls);
    }
}
