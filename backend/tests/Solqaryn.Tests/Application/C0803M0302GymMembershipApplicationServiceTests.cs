using Xunit;
using Rules = global::Solqaryn.Domain.AutomationV5.C0803GymMembershipDomainRules;
using Service = global::Solqaryn.Application.AutomationV5.C0803GymMembershipApplicationService;
using global::Solqaryn.Application.AutomationV5;

namespace Solqaryn.Tests.Application;

public sealed class C0803M0302GymMembershipApplicationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 11, 23, 1, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }
    private static Service.AuthenticatedContext Actor() =>
        new(new Rules.VerifiedActor(91, 11, 111, true, true, true, false),
            true, true, true, true);
    private static Service.VerifiedMembership Member() =>
        new(new Rules.MembershipSnapshot(11, 801, 401, 111,
            Rules.MembershipState.Active, 7,
            UtcNow.AddHours(-1), UtcNow.AddHours(1),
            true, false, false, true), true, true, true);
    private static Service.ClientIntent Intent() => new(801, 401, 111, 7);

    private sealed class ActorSource : IC0803VerifiedGymActorSource
    {
        public Service.AuthenticatedContext? Value { get; set; } = Actor();
        public Exception? Error { get; set; }
        public Action? AfterRead { get; set; }
        public int Calls { get; private set; }
        public ValueTask<Service.AuthenticatedContext?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Error is not null) throw Error;
            AfterRead?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class MemberSource : IC0803VerifiedGymMembershipSource
    {
        public Service.VerifiedMembership? Value { get; set; } = Member();
        public Exception? Error { get; set; }
        public Action? AfterRead { get; set; }
        public int Calls { get; private set; }
        public int ReadTenant { get; private set; }
        public int ReadMember { get; private set; }
        public ValueTask<Service.VerifiedMembership?> ReadCurrentAsync(
            int tenant, int member, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++; ReadTenant = tenant; ReadMember = member;
            if (Error is not null) throw Error;
            AfterRead?.Invoke();
            return ValueTask.FromResult(Value);
        }
    }

    private static Service Create(ActorSource actor, MemberSource member) =>
        new(actor, member, new Clock());

    [Fact]
    public async Task ValidTrustedServerEligibilityIsOnlyNonGrantCandidate()
    {
        var a = new ActorSource(); var m = new MemberSource();
        var result = await Create(a, m).PrepareAsync(Intent());
        Assert.Equal("GYM_MEMBERSHIP_CANDIDATE_REQUIRES_LIVE_BACKEND_GATE", result.Code);
        Assert.True(result.RequiresAtomicAuthenticatedBackendRecheck);
        Assert.False(result.AuthorizesDoorOrHardware);
        Assert.False(result.AuthorizesJwtRbacOrPayments);
        Assert.False(result.CertifiesHttpMysqlIntegration);
        Assert.Equal(11, m.ReadTenant); Assert.Equal(801, m.ReadMember);
        Assert.Equal(1, a.Calls); Assert.Equal(1, m.Calls);
    }

    [Fact]
    public async Task InvalidInputAndClientPaymentDoorOfflineOverrideDenyBeforeSources()
    {
        var a = new ActorSource(); var m = new MemberSource();
        var service = Create(a, m);
        foreach (var input in new Service.ClientIntent?[]
        {
            null, Intent() with { MemberId = 0 },
            Intent() with { PlanId = 0 }, Intent() with { BranchId = 0 },
            Intent() with { ExpectedVersion = 0 },
            Intent() with { ClientTenantOverride = true },
            Intent() with { ClaimsPaymentConfirmed = true },
            Intent() with { RequestsOfflineAccess = true },
            Intent() with { RequestsDoorOpening = true }
        })
        {
            var r = await service.PrepareAsync(input);
            Assert.False(r.RequiresAtomicAuthenticatedBackendRecheck);
            Assert.False(r.AuthorizesDoorOrHardware);
        }
        Assert.Equal(0, a.Calls); Assert.Equal(0, m.Calls);
    }

    [Fact]
    public async Task ForgedJwtRevokedPermissionOrUntrustedSourceCannotReadMembership()
    {
        foreach (var ctx in new[]
        {
            Actor() with { SignedJwtValidated = false },
            Actor() with { FromTrustedServer = false },
            Actor() with { CurrentMembershipAndRbacFromDatabase = false },
            Actor() with { CurrentTenantResolved = false },
            Actor() with { Actor = Actor().Actor! with { JwtVerified = false } },
            Actor() with { Actor = Actor().Actor! with { PermissionRevoked = true } },
            Actor() with { Actor = Actor().Actor! with { GymPermissionGrantedByServer = false } },
            Actor() with { Actor = Actor().Actor! with { SaaSMembershipActive = false } }
        })
        {
            var members = new MemberSource();
            var result = await Create(new ActorSource { Value = ctx }, members)
                .PrepareAsync(Intent());
            Assert.Equal("SIGNED_JWT_CURRENT_TENANT_AND_RBAC_REQUIRED", result.Code);
            Assert.Equal(0, members.Calls);
        }
    }

    [Fact]
    public async Task ForeignBranchAndTenantBMembershipDoNotLeak()
    {
        var members = new MemberSource();
        var denyBranch = await Create(new ActorSource(), members).PrepareAsync(
            Intent() with { BranchId = 222 });
        Assert.Equal("CROSS_BRANCH_ACCESS_DENIED", denyBranch.Code);
        Assert.Equal(0, members.Calls);

        members.Value = Member() with
        { Snapshot = Member().Snapshot! with { TenantEmpresaId = 22 } };
        var crossTenant = await Create(new ActorSource(), members).PrepareAsync(Intent());
        Assert.Equal("TENANT_BRANCH_MEMBER_PLAN_MISMATCH", crossTenant.Code);
        Assert.Equal(11, members.ReadTenant);
    }

    [Fact]
    public async Task UntrustedOrStaleMemberOrErpEvidenceCannotQualify()
    {
        foreach (Service.VerifiedMembership? snapshot in new Service.VerifiedMembership?[]
        {
            null, Member() with { FromTrustedTenantScopedStore = false },
            Member() with { CurrentVersionRead = false },
            Member() with { CurrentErpReconciliationVerified = false },
            Member() with { Snapshot = null }
        })
        {
            var result = await Create(new ActorSource(), new MemberSource
            { Value = snapshot }).PrepareAsync(Intent());
            Assert.Equal("FRESH_TENANT_SCOPED_MEMBER_AND_ERP_PROOF_REQUIRED", result.Code);
        }
    }

    [Fact]
    public async Task ExpiredSuspendedRevokedStaleVersionAndUnreconciledPaymentFailClosed()
    {
        foreach (var snapshot in new[]
        {
            Member().Snapshot! with { EndsAtUtc = UtcNow },
            Member().Snapshot! with { SuspensionActive = true },
            Member().Snapshot! with { CredentialRevoked = true },
            Member().Snapshot! with { Version = 8 },
            Member().Snapshot! with { ErpPaymentReconciled = false },
            Member().Snapshot! with { State = Rules.MembershipState.Cancelled }
        })
        {
            var r = await Create(new ActorSource(), new MemberSource
            { Value = Member() with { Snapshot = snapshot } }).PrepareAsync(Intent());
            Assert.False(r.RequiresAtomicAuthenticatedBackendRecheck);
            Assert.False(r.AuthorizesDoorOrHardware);
        }
    }

    [Fact]
    public async Task ProviderFailuresUseConstantSafeCodesWithoutSecrets()
    {
        var a = new ActorSource { Error = new InvalidOperationException("JWT secret") };
        var m = new MemberSource();
        Assert.Equal("TRUSTED_GYM_ACTOR_SOURCE_UNAVAILABLE",
            (await Create(a, m).PrepareAsync(Intent())).Code);
        Assert.Equal(0, m.Calls);
        var broken = new MemberSource { Error = new InvalidOperationException("mysql passwd") };
        Assert.Equal("TRUSTED_GYM_MEMBERSHIP_SOURCE_UNAVAILABLE",
            (await Create(new ActorSource(), broken).PrepareAsync(Intent())).Code);
    }

    [Fact]
    public async Task CancellationAfterActorReadPreventsPrivateMemberLookup()
    {
        using var cts = new CancellationTokenSource();
        var a = new ActorSource { AfterRead = () => cts.Cancel() };
        var m = new MemberSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { await Create(a, m).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(0, m.Calls);
    }

    [Fact]
    public async Task CancellationAfterMemberReadCannotIssueCandidate()
    {
        using var cts = new CancellationTokenSource();
        var m = new MemberSource { AfterRead = () => cts.Cancel() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { await Create(new ActorSource(), m).PrepareAsync(Intent(), cts.Token); });
        Assert.Equal(1, m.Calls);
    }
}
