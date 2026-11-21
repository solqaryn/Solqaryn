using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class C0402M0302ContractApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
    private static C0402ContractApplicationService.ClientIntent Intent() =>
        new(42, 100, 3, Rules.ContractOperation.SubmitForAcceptance, Now, "verified-reason");
    private static C0402ContractApplicationService.VerifiedServerActor Actor() =>
        new(new Rules.VerifiedActor(100, 17, true, true, true, false),
            true, true, true, true);
    private static C0402ContractApplicationService.VerifiedContractContext Context() =>
        new(new Rules.ContractSnapshot(100, 42, Rules.ContractState.Draft, 3,
                Now.AddDays(-10), Now.AddDays(10)),
            new Rules.CommercialEvidence(true, true, true, true, true, true, true),
            true, true, true, true);

    private sealed class ActorStub(
        Func<CancellationToken, ValueTask<C0402ContractApplicationService.VerifiedServerActor?>> read)
        : IC0402VerifiedContractActorSource
    {
        public ValueTask<C0402ContractApplicationService.VerifiedServerActor?> ReadCurrentAsync(
            CancellationToken cancellationToken) => read(cancellationToken);
    }

    private sealed class ContractStub(
        Func<int, long, CancellationToken, ValueTask<C0402ContractApplicationService.VerifiedContractContext?>> read)
        : IC0402VerifiedContractSnapshotSource
    {
        public ValueTask<C0402ContractApplicationService.VerifiedContractContext?> ReadOwnedAsync(
            int tenant, long id, CancellationToken cancellationToken) => read(tenant, id, cancellationToken);
    }

    private static C0402ContractApplicationService Service(
        C0402ContractApplicationService.VerifiedServerActor? actor = null,
        C0402ContractApplicationService.VerifiedContractContext? context = null) =>
        new(new ActorStub(_ => ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(actor ?? Actor())),
            new ContractStub((_, _, _) => ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(context ?? Context())));

    [Fact]
    public async Task PositiveDomainPreflightIsNeverPermissionOrPayment()
    {
        var p = await Service().PrepareAsync(Intent());
        Assert.Equal("REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED", p.Code);
        Assert.True(p.IsDomainCandidateOnly);
        Assert.False(p.AuthorizesContractMutation);
        Assert.False(p.AuthorizesPaymentOrRefund);
        Assert.False(p.AuthorizesPrivateRead);
        Assert.False(p.CertifiesJwtHttpMysql);
        Assert.False(p.PersistsData);
    }

    [Fact]
    public async Task OnlyAuthenticatedTenantIsUsedForPersistedLookup()
    {
        int tenant = 0; long id = 0;
        var service = new C0402ContractApplicationService(
            new ActorStub(_ => ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor())),
            new ContractStub((t, c, _) =>
            {
                tenant = t; id = c;
                return ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context());
            }));
        Assert.True((await service.PrepareAsync(Intent() with { TenantHint = null })).IsDomainCandidateOnly);
        Assert.Equal(100, tenant);
        Assert.Equal(42, id);
    }

    [Fact]
    public async Task RejectedIntentDoesNotReadAnySource()
    {
        var calls = 0;
        var svc = new C0402ContractApplicationService(
            new ActorStub(_ =>
            {
                calls++;
                return ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor());
            }),
            new ContractStub((_, _, _) => ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context())));
        var invalid = new C0402ContractApplicationService.ClientIntent?[]
        {
            null,
            Intent() with { ContractIdHint = -1 },
            Intent() with { ExpectedVersion = 0 },
            Intent() with { EffectiveUtc = DateTime.SpecifyKind(Now, DateTimeKind.Local) },
            Intent() with { Cause = "bad\nreason" },
            Intent() with { Operation = (Rules.ContractOperation)999 }
        };
        foreach (var intent in invalid)
            Assert.False((await svc.PrepareAsync(intent)).IsDomainCandidateOnly);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ClientCanNeverAssertRolePolicyPaymentOrDirectWrite()
    {
        foreach (var input in new[]
        {
            Intent() with { SuppliesRoleOrPolicyAuthority = true },
            Intent() with { RequestsDirectMutation = true },
            Intent() with { RequestsPaymentOrRefund = true }
        })
        {
            var p = await Service().PrepareAsync(input);
            Assert.Equal("CLIENT_AUTHORITY_OR_SIDE_EFFECT_DENIED", p.Code);
            Assert.False(p.AuthorizesContractMutation);
        }
    }

    [Fact]
    public async Task TenantBHintRejectedBeforeLoadingAnyContract()
    {
        var calls = 0;
        var svc = new C0402ContractApplicationService(
            new ActorStub(_ => ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor())),
            new ContractStub((_, _, _) =>
            {
                calls++;
                return ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context());
            }));
        Assert.Equal("CLIENT_TENANT_HINT_MISMATCH",
            (await svc.PrepareAsync(Intent() with { TenantHint = 200 })).Code);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ForgedActorOrRevokedPermissionDenied()
    {
        foreach (var actor in new[]
        {
            Actor() with { TrustedServerSource = false },
            Actor() with { SignedJwtEstablished = false },
            Actor() with { RelationalRbacFresh = false },
            Actor() with { MembershipFresh = false },
            Actor() with { Scope = Actor().Scope with { PermissionRevoked = true } },
            Actor() with { Scope = Actor().Scope with { JwtVerified = false } }
        })
        {
            var p = await Service(actor: actor).PrepareAsync(Intent());
            Assert.Equal("LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED", p.Code);
        }
    }

    [Fact]
    public async Task WrongTenantOwnerVersionAndPolicyProvenanceDenied()
    {
        var inputs = new[]
        {
            Context() with { ServerTenantFiltered = false },
            Context() with { ResourceOwnedByActorTenant = false },
            Context() with { SnapshotVersionFresh = false },
            Context() with { PolicyProvenanceVerified = false },
            Context() with { Contract = Context().Contract with { EmpresaId = 200 } },
            Context() with { Contract = Context().Contract with { ContractId = 43 } },
            Context() with { Contract = Context().Contract with { Version = 2 } }
        };
        foreach (var x in inputs)
        {
            var p = await Service(context: x).PrepareAsync(Intent());
            Assert.Equal("VERIFIED_OWNED_CONTRACT_VERSION_AND_POLICY_REQUIRED", p.Code);
        }
    }

    [Fact]
    public async Task CommercialPolicyNotApprovedAndInvalidTransitionDenied()
    {
        var unapproved = Context() with
        {
            CommercialPolicy = Context().CommercialPolicy with { D02TermsApproved = false }
        };
        Assert.Equal("D01_D02_APPROVAL_REQUIRED",
            (await Service(context: unapproved).PrepareAsync(Intent())).Code);
        Assert.Equal("INVALID_STATE_TRANSITION",
            (await Service().PrepareAsync(Intent() with { Operation = Rules.ContractOperation.Activate })).Code);
    }

    [Fact]
    public async Task ServerFailuresReturnNoCandidateWithoutLeakingPayload()
    {
        var failedActor = new C0402ContractApplicationService(
            new ActorStub(_ => throw new InvalidOperationException("auth offline")),
            new ContractStub((_, _, _) => ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context())));
        var failedContract = new C0402ContractApplicationService(
            new ActorStub(_ => ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor())),
            new ContractStub((_, _, _) => throw new InvalidOperationException("sql offline")));
        Assert.Equal("SERVER_ACTOR_SOURCE_UNAVAILABLE", (await failedActor.PrepareAsync(Intent())).Code);
        Assert.Equal("SERVER_CONTRACT_SOURCE_UNAVAILABLE", (await failedContract.PrepareAsync(Intent())).Code);
    }

    [Fact]
    public async Task CancellationBeforeAndBetweenProvidersIsPropagated()
    {
        var reads = 0;
        using var cts = new CancellationTokenSource();
        var svc = new C0402ContractApplicationService(
            new ActorStub(_ =>
            {
                cts.Cancel();
                return ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor());
            }),
            new ContractStub((_, _, _) =>
            {
                reads++;
                return ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context());
            }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await svc.PrepareAsync(Intent(), cts.Token));
        Assert.Equal(0, reads);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Service().PrepareAsync(Intent(), new CancellationToken(true)));
    }

    [Fact]
    public void NullSourceDependenciesFailImmediately()
    {
        Assert.Throws<ArgumentNullException>(() => new C0402ContractApplicationService(
            null!, new ContractStub((_, _, _) =>
                ValueTask.FromResult<C0402ContractApplicationService.VerifiedContractContext?>(Context()))));
        Assert.Throws<ArgumentNullException>(() => new C0402ContractApplicationService(
            new ActorStub(_ => ValueTask.FromResult<C0402ContractApplicationService.VerifiedServerActor?>(Actor())), null!));
    }
}
